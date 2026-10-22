// Program.cs - interface de linha de comando (secao 9 do SPEC.md), instancia unica,
// escolha do backend, gancho de teclado e laco de atualizacao (secao 5).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MyVersion
{
    internal static class Program
    {
        private const string NomeMutex = "Local\\KeyToJoyEmulator-teclado-joystick";

        private static Log _log;
        private static Motor _motor;
        private static IControleVirtual _backend;
        private static GanchoTeclado _gancho;
        private static volatile bool _rodando;
        private static int _encerrado;
        private static readonly ManualResetEventSlim _lacoEncerrado = new ManualResetEventSlim(false);

        // resolucao do relogio do sistema em 1 ms (sem isso Sleep(1) dorme ~15 ms e o tique de 8 ms nao acontece)
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")] private static extern uint TimeBeginPeriod(uint ms);
        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")] private static extern uint TimeEndPeriod(uint ms);

        private delegate bool FnCtrl(uint tipo);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleCtrlHandler(FnCtrl fn, bool adicionar);
        private static FnCtrl _aoCtrl;   // manter viva a referencia

        private const int SwHide = 0;
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint GetConsoleProcessList(uint[] processos, uint tamanho);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr janela, int comando);

        // O pad e publicado como programa de JANELA (sem console). Assim o Windows NUNCA abre aquela janela
        // preta ao dar dois cliques no KeyToJoyEmulator.exe (antes ela abria cheia de texto de log e nao fechava).
        // Quando ele e chamado de dentro de um console (cmd/PowerShell/atalho), a gente se pendura no console
        // do pai: as mensagens continuam aparecendo na tela, como antes.
        private const uint AttachParentProcess = 0xFFFFFFFF;
        private const int StdOutputHandle = -11;
        private const int StdErrorHandle = -12;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;

        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AttachConsole(uint idDoProcesso);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GetStdHandle(int qual);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(string nome, uint acesso, uint compartilhar, IntPtr seguranca, uint criacao, uint atributos, IntPtr modelo);

        private static void PendurarNoConsoleDoPai()
        {
            try
            {
                bool temSaida = Valido(GetStdHandle(StdOutputHandle));
                bool temErro = Valido(GetStdHandle(StdErrorHandle));
                if (temSaida && temErro) return;   // ja tem destino: arquivo, cano (pipe) ou console herdado

                if (!AttachConsole(AttachParentProcess)) return;   // ninguem nos chamou de um console: nada a mostrar

                if (!temSaida) Console.SetOut(EscritorNoConsole());
                if (!temErro) Console.SetError(EscritorNoConsole());
            }
            catch { }
        }

        private static bool Valido(IntPtr h)
        {
            return h != IntPtr.Zero && h != new IntPtr(-1);
        }

        private static TextWriter EscritorNoConsole()
        {
            IntPtr h = CreateFile("CONOUT$", GenericRead | GenericWrite, FileShareRead | FileShareWrite,
                                  IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
            if (!Valido(h)) return Console.Out;   // sem console mesmo: fica como estava
            FileStream fs = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(h, true), FileAccess.Write);
            return new StreamWriter(fs) { AutoFlush = true };
        }

        private static int Main(string[] args)
        {
            // primeiro de tudo: se fomos chamados de um console (cmd/PowerShell), aparecer na tela dele.
            // Chamado de dois cliques, nao existe console nenhum e nenhuma janela aparece.
            PendurarNoConsoleDoPai();

            Opcoes op;
            try
            {
                op = Opcoes.Analisar(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("erro: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Opcoes.Ajuda());
                return 1;
            }

            if (op.PedirAjuda)
            {
                Console.WriteLine(Opcoes.Ajuda());
                return 0;
            }

            if (op.Validar != null)
            {
                try
                {
                    CarregadorPerfil.Carregar(op.Validar, out List<string> avisos);
                    foreach (string a in avisos) Console.WriteLine("aviso: " + a);
                    Console.WriteLine("arquivo de perfil OK: " + op.Validar);
                    return 0;
                }
                catch (PerfilInvalidoException e)
                {
                    Console.Error.WriteLine("erro no arquivo de perfil: " + e.Message);
                    return 2;
                }
            }

            // 1. arquivo de perfil
            string caminhoPerfil = op.Perfil ?? CaminhoPerfilPadrao();
            ArquivoPerfis arquivo;
            List<string> avisosPerfil;
            try
            {
                arquivo = CarregadorPerfil.Carregar(caminhoPerfil, out avisosPerfil);
            }
            catch (PerfilInvalidoException e)
            {
                Console.Error.WriteLine("erro no arquivo de perfil: " + e.Message);
                return 2;
            }

            _log = new Log(op.SemLog ? null : (op.Log ?? CaminhoLogPadrao()), escreverNoConsole: true);
            foreach (string a in avisosPerfil) _log.Comum("aviso: " + a);
            _log.Comum($"arquivo de perfil: {arquivo.Caminho} (tique_ms={arquivo.TiqueMs}, perfil_inicial='{arquivo.PerfilInicial}')");
            if (_log.Caminho != null) _log.Comum("log em arquivo: " + _log.Caminho);

            // 2. instancia unica (RF6)
            Mutex mutex;
            bool criadoAgora;
            try
            {
                mutex = new Mutex(true, NomeMutex, out criadoAgora);
            }
            catch (Exception e)
            {
                _log.Comum("erro ao criar o mutex de instancia unica: " + e.Message);
                return 1;
            }
            if (!criadoAgora)
            {
                _log.Comum("ja existe uma instancia deste programa rodando; saindo (codigo 3)");
                return 3;
            }

            // 3. motor de mapeamento
            try
            {
                _motor = new Motor(arquivo, op.PerfilInicial);
            }
            catch (PerfilInvalidoException e)
            {
                _log.Comum("erro: " + e.Message);
                return 2;
            }
            _motor.Mensagem += texto => _log.Comum(texto);
            _log.Comum("perfil ativo: " + _motor.PerfilAtivo);

            // 4. backend (controle virtual)
            int codigo;
            _backend = CriarBackend(op, out codigo);
            if (_backend == null)
            {
                _motor = null;
                return codigo;
            }

            // 5. gancho global de teclado (precisa ser instalado nesta thread, que roda a bomba de mensagens)
            // MYVERSION_DEBUG_TECLAS=1 grava no log o codigo CRU de cada tecla que o hook ve (diagnostico)
            bool depurarTeclas = Environment.GetEnvironmentVariable("MYVERSION_DEBUG_TECLAS") == "1";
            if (depurarTeclas) _log.Comum("depuracao de teclas LIGADA (MYVERSION_DEBUG_TECLAS=1)");
            _gancho = new GanchoTeclado((tecla, pressionada) =>
            {
                string nome = TabelaTeclas.NomeDe(tecla);
                if (pressionada) _motor.Apertar(tecla.Scan, tecla.Estendida);
                else _motor.Soltar(tecla.Scan, tecla.Estendida);
                if (nome == null && !pressionada) { /* tecla desconhecida: ignorada */ }
            },
            depurarTeclas ? (Action<string>)(texto => _log.Comum(texto)) : null);

            if (!_gancho.Instalar(out string erroHook))
            {
                _log.Comum("erro: " + erroHook);
                _backend.Desconectar();
                return 5;
            }

            _aoCtrl = AoCtrlCtrlC;
            try { SetConsoleCtrlHandler(_aoCtrl, true); } catch { }

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _log.Comum("Ctrl+C recebido; encerrando");
                _rodando = false;
                _gancho.PedirParada();
            };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Encerrar();

            _rodando = true;
            Thread laco = new Thread(Laco) { IsBackground = true, Name = "laco-atualizacao" };
            laco.Start();

            if (op.EncerrarAposMs > 0)
            {
                // usado pelos testes de aceitacao (T7) para exercitar o encerramento normal
                int ms = op.EncerrarAposMs;
                _log.Comum("--encerrar-apos-ms " + ms + ": vou encerrar sozinho");
                Thread encerrador = new Thread(() =>
                {
                    Thread.Sleep(ms);
                    _log.Comum("encerrando por --encerrar-apos-ms");
                    _rodando = false;
                    _gancho.PedirParada();
                });
                encerrador.IsBackground = true;
                encerrador.Start();
            }

            _log.Comum("pronto: capturando teclado. Use Ctrl+C para sair.");

            _gancho.BombearMensagens();   // bloqueia ate WM_QUIT

            _rodando = false;
            _lacoEncerrado.Wait(1500);
            Encerrar();

            try { mutex.ReleaseMutex(); } catch { }
            return 0;
        }

        private static bool AoCtrlCtrlC(uint tipo)
        {
            // 0 CTRL_C, 1 CTRL_BREAK, 2 CTRL_CLOSE, 5 CTRL_LOGOFF, 6 CTRL_SHUTDOWN
            _log?.Comum("evento de console " + tipo + "; encerrando");
            _rodando = false;
            try { Encerrar(); } catch { }
            return tipo != 2 && tipo != 5 && tipo != 6;
        }

        private static IControleVirtual CriarBackend(Opcoes op, out int codigo)
        {
            codigo = 0;
            string escolha = op.Backend ?? Environment.GetEnvironmentVariable("MYVERSION_BACKEND") ?? "vigem";

            if (escolha.Equals("falso", StringComparison.OrdinalIgnoreCase))
            {
                _log.Comum("backend FALSO escolhido: nenhum controle virtual real sera criado");
                ControleFalso falso = new ControleFalso();
                falso.Conectar();
                _log.Comum("backend: " + falso.Nome);
                return falso;
            }

            BackendVigem vigem = new BackendVigem(t => _log.Comum(t));
            try
            {
                vigem.Conectar();
            }
            catch (DllNotFoundException e)
            {
                _log.Comum("erro: nao encontrei a biblioteca nativa ViGEmClient.dll: " + e.Message);
                _log.Comum("coloque ViGEmClient.dll (x64) ao lado do executavel, ou use --backend falso.");
                codigo = 4;
                return null;
            }
            catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException e)
            {
                _log.Comum("erro: driver ViGEmBus nao esta instalado neste Windows: " + e.Message);
                _log.Comum("instale o ViGEmBus (ViGEmBus_1.22.0_x64_x86_arm64.exe) e tente de novo. Veja o README.");
                codigo = 4;
                return null;
            }
            catch (Nefarius.ViGEm.Client.Exceptions.VigemBusVersionMismatchException e)
            {
                _log.Comum("erro: versao do driver ViGEmBus incompativel: " + e.Message);
                codigo = 4;
                return null;
            }
            catch (Nefarius.ViGEm.Client.Exceptions.VigemBusAccessFailedException e)
            {
                _log.Comum("erro: sem acesso ao driver ViGEmBus: " + e.Message);
                codigo = 4;
                return null;
            }
            catch (Exception e)
            {
                _log.Comum("erro ao criar o controle virtual (" + e.GetType().Name + "): " + e.Message);
                codigo = 5;
                return null;
            }

            _log.Comum("backend: " + vigem.Nome);
            return vigem;
        }

        private static void Laco()
        {
            double tique = Math.Min(_motor.Arquivo.TiqueMs, 8);   // secao 5: nunca dormir mais que 8 ms
            if (tique < 1) tique = 1;
            TimeBeginPeriod(1);
            try
            {
                Stopwatch relogio = Stopwatch.StartNew();
                double ultimo = 0;
                double ultimoLog = -1000;
                EstadoControle anterior = new EstadoControle();

                while (_rodando)
                {
                    double agora = relogio.Elapsed.TotalMilliseconds;
                    double dt = agora - ultimo;
                    if (dt < tique)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    EstadoControle estado = _motor.Tique(dt);
                    _backend.Aplicar(estado);          // RF6: envia sempre no tique

                    if (!estado.Igual(anterior))
                    {
                        anterior = estado;
                        if (agora - ultimoLog >= 100)
                        {
                            _log.Comum("estado: " + estado.Descrever());
                            ultimoLog = agora;
                        }
                    }
                    ultimo = agora;
                }
            }
            catch (Exception e)
            {
                _log.Comum("erro no laco de atualizacao: " + e);
            }
            finally
            {
                TimeEndPeriod(1);
                _lacoEncerrado.Set();
            }
        }

        private static void Encerrar()
        {
            if (Interlocked.Exchange(ref _encerrado, 1) != 0) return;
            try { _backend?.Desconectar(); } catch (Exception e) { _log?.Comum("aviso: " + e.Message); }
            try { _gancho?.Dispose(); } catch { }
            _log?.Comum("encerrado; controle virtual removido");
            _log?.Dispose();
        }

        private static string CaminhoPerfilPadrao()
        {
            string aoLado = Path.Combine(PastaDoPrograma(), "profiles", "exemplo.json");
            if (File.Exists(aoLado)) return aoLado;
            return Path.Combine("profiles", "exemplo.json");
        }

        private static string CaminhoLogPadrao()
        {
            return Path.Combine(PastaDoPrograma(), "KeyToJoyEmulator.log");
        }

        /// <summary>
        /// A pasta do proprio programa. Num exe de ARQUIVO UNICO (e como o pacote ./dist e publicado) o
        /// `AppContext.BaseDirectory` aponta para a pasta TEMPORARIA da descompactacao; o caminho de
        /// verdade esta em `Environment.ProcessPath`.
        /// </summary>
        private static string PastaDoPrograma()
        {
            string exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                string pasta = Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(pasta)) return pasta;
            }
            return AppContext.BaseDirectory;
        }

        // ------------------------------------------------------------------ opcoes

        internal sealed class Opcoes
        {
            public string Perfil;
            public string PerfilInicial;
            public string Log;
            public bool SemLog;
            public string Validar;
            public bool PedirAjuda;
            public string Backend;
            public int EncerrarAposMs;

            public static string Ajuda()
            {
                return string.Join(Environment.NewLine, new[]
                {
                    "KeyToJoyEmulator - teclado vira controle de jogo virtual (XInput, via ViGEmBus)",
                    "",
                    "Uso:",
                    "  KeyToJoyEmulator.exe [--perfil <arquivo.json>] [--perfil-inicial <nome>] [--log <arquivo>] [--sem-log]",
                    "  KeyToJoyEmulator.exe --validar <arquivo.json>",
                    "  KeyToJoyEmulator.exe --ajuda",
                    "",
                    "Opcoes:",
                    "  --perfil <arquivo.json>    arquivo de perfis (padrao: profiles/exemplo.json ao lado do executavel)",
                    "  --perfil-inicial <nome>    perfil com que o programa comeca (padrao: o 'perfil_inicial' do arquivo)",
                    "  --log <arquivo>            grava o log tambem neste arquivo (padrao: KeyToJoyEmulator.log ao lado do executavel)",
                    "  --sem-log                  nao grava log em arquivo",
                    "  --backend <vigem|falso>    'falso' nao cria controle virtual (para testes sem driver)",
                    "  --encerrar-apos-ms <ms>    encerra sozinho depois de N ms (usado pelos testes)",
                    "  --validar <arquivo.json>   so valida o arquivo de perfis e sai",
                    "  --ajuda                    mostra esta ajuda",
                    "",
                    "Codigos de saida: 0 sucesso; 1 erro inesperado; 2 arquivo de perfil invalido;",
                    "                  3 ja existe uma instancia; 4 ViGEmBus ausente/incompativel;",
                    "                  5 falha ao criar o controle virtual (ou o gancho de teclado)",
                });
            }

            public static Opcoes Analisar(string[] args)
            {
                Opcoes o = new Opcoes();
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i];
                    switch (a)
                    {
                        case "--ajuda":
                        case "--help":
                        case "-h":
                            o.PedirAjuda = true;
                            break;
                        case "--perfil":
                            o.Perfil = Valor(args, ref i, a);
                            break;
                        case "--perfil-inicial":
                            o.PerfilInicial = Valor(args, ref i, a);
                            break;
                        case "--log":
                            o.Log = Valor(args, ref i, a);
                            break;
                        case "--sem-log":
                            o.SemLog = true;
                            break;
                        case "--backend":
                            o.Backend = Valor(args, ref i, a);
                            if (o.Backend != "vigem" && o.Backend != "falso")
                                throw new ArgumentException("--backend aceita apenas 'vigem' ou 'falso'");
                            break;
                        case "--validar":
                            o.Validar = Valor(args, ref i, a);
                            break;
                        case "--encerrar-apos-ms":
                            o.EncerrarAposMs = int.Parse(Valor(args, ref i, a), System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        default:
                            throw new ArgumentException("opcao desconhecida: " + a);
                    }
                }
                return o;
            }

            private static string Valor(string[] args, ref int i, string opcao)
            {
                if (i + 1 >= args.Length) throw new ArgumentException("falta o valor de " + opcao);
                i++;
                return args[i];
            }
        }
    }
}
