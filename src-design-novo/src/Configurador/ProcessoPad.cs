// ProcessoPad.cs - liga e desliga o programa do pad (KeyToJoyEmulator.exe) como processo separado.
//
// A interface NAO embute o pad: ela o comanda. Assim o pad continua sendo um programa pequeno,
// independente e testavel por linha de comando, e a interface cuida so de configurar, ligar e olhar.
//
// Detalhe medido na pratica (2026-10-18): com o jogo aberto o pad leva de 0,4 a 2,4 s para ficar
// pronto; se ele sair antes disso, foi erro de verdade (codigo 3 = ja existe instancia, 4 = sem
// driver, 5 = falhou o pad ou o gancho de teclado).

using System;
using System.Diagnostics;
using System.IO;

namespace Configurador
{
    public sealed class ProcessoPad : IDisposable
    {
        // Nome do programa do pad. Era "my-version" ate 2026-10-18; o usuario renomeou o executavel
        // principal para KeyToJoyEmulator (o mesmo nome que aparece no titulo da janela e nos pacotes).
        public const string NOME_DO_PROCESSO = "KeyToJoyEmulator";

        private Process _processo;

        /// <summary>Quantos KeyToJoyEmulator estao rodando agora (inclusive os que nao foram ligados por aqui).</summary>
        public static int QuantosRodando()
        {
            try { return Process.GetProcessesByName(NOME_DO_PROCESSO).Length; }
            catch { return 0; }
        }

        public bool LigadoPorMim
        {
            get { return _processo != null && !_processo.HasExited; }
        }

        public string CaminhoDoLog { get; set; }

        /// <summary>
        /// Procura o KeyToJoyEmulator.exe: primeiro ao lado deste executavel (e como o pacote ./dist fica),
        /// depois no layout de desenvolvimento (my-version\src\MyVersion\bin\Release\net8.0\).
        /// </summary>
        public static string ProcurarExecutavel()
        {
            string lado = Path.Combine(Caminhos.PastaDoPrograma(), NOME_DO_PROCESSO + ".exe");
            if (File.Exists(lado)) return lado;

            string pasta = Caminhos.PastaDoPrograma();
            for (int nivel = 0; nivel < 6 && !string.IsNullOrEmpty(pasta); nivel++)
            {
                string[] candidatos =
                {
                    Path.Combine(pasta, "my-version", "src", "MyVersion", "bin", "Release", "net8.0", NOME_DO_PROCESSO + ".exe"),
                    Path.Combine(pasta, "src", "MyVersion", "bin", "Release", "net8.0", NOME_DO_PROCESSO + ".exe"),
                };
                foreach (string c in candidatos) if (File.Exists(c)) return c;

                DirectoryInfo pai = Directory.GetParent(pasta);
                pasta = pai == null ? null : pai.FullName;
            }
            return lado;   // devolve o caminho esperado no pacote, mesmo que ainda nao exista
        }

        /// <summary>Inicia o pad com o arquivo de perfis indicado. Devolve false e explica em `erro`.</summary>
        public bool Iniciar(string exe, string arquivoDePerfis, string caminhoDoLog, out string erro)
        {
            erro = null;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe)) { erro = "nao encontrei o programa do pad: " + exe; return false; }
            if (!File.Exists(arquivoDePerfis)) { erro = "nao encontrei o arquivo de perfis: " + arquivoDePerfis; return false; }
            if (QuantosRodando() > 0) { erro = "ja existe um KeyToJoyEmulator rodando (o programa aceita uma instancia so); pare antes de iniciar"; return false; }

            ProcessStartInfo info = new ProcessStartInfo(exe);
            info.Arguments = "--perfil \"" + arquivoDePerfis + "\" --log \"" + caminhoDoLog + "\"";
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;   // reforco: o pad nao deve abrir janela nenhuma
            info.WorkingDirectory = Path.GetDirectoryName(exe);

            try { _processo = Process.Start(info); }
            catch (Exception e) { erro = "nao consegui iniciar o pad: " + e.Message; return false; }

            CaminhoDoLog = caminhoDoLog;

            // saida imediata = erro de verdade (codigo 3/4/5/2); ver o cabecalho deste arquivo
            if (_processo.WaitForExit(1500))
            {
                int codigo;
                try { codigo = _processo.ExitCode; } catch { codigo = -1; }
                erro = "o pad saiu logo depois de iniciar (codigo " + codigo + "): " + Significado(codigo);
                _processo.Dispose();
                _processo = null;
                return false;
            }
            return true;
        }

        /// <summary>Para o pad: primeiro o que eu liguei; se nao houver, todos os KeyToJoyEmulator do sistema.</summary>
        public bool Parar(out string erro)
        {
            erro = null;
            try
            {
                if (LigadoPorMim)
                {
                    _processo.Kill(true);
                    _processo.WaitForExit(3000);
                    return true;
                }

                Process[] todos = Process.GetProcessesByName(NOME_DO_PROCESSO);
                if (todos.Length == 0) { erro = "nao havia nenhum KeyToJoyEmulator rodando"; return false; }
                foreach (Process p in todos)
                {
                    try { p.Kill(true); p.WaitForExit(3000); }
                    catch (Exception e) { erro = "nao consegui parar o processo " + p.Id + ": " + e.Message; return false; }
                }
                return true;
            }
            catch (Exception e) { erro = "erro ao parar o pad: " + e.Message; return false; }
        }

        public static string Significado(int codigo)
        {
            switch (codigo)
            {
                case 0: return "terminou normalmente";
                case 1: return "erro inesperado ou argumento errado";
                case 2: return "arquivo de perfis invalido";
                case 3: return "ja existe uma instancia do pad";
                case 4: return "driver ViGEmBus nao encontrado";
                case 5: return "falhou ao criar o controle virtual ou ao instalar o gancho de teclado";
                default: return "codigo nao documentado";
            }
        }

        public void Dispose()
        {
            if (_processo != null) { _processo.Dispose(); _processo = null; }
        }
    }
}
