// Program.cs - ponto de entrada da interface de configuracao de perfis e teclas.
//
// Uso: KeyToJoyEmulator-Configurador.exe [arquivo-de-perfis.json]
//      Sem argumento, ele procura profiles/corrida.json ao lado do executavel.

using System;
using System.IO;
using System.Windows.Forms;

namespace Configurador
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string arquivo = (args != null && args.Length > 0) ? args[0] : null;

            try { Application.SetHighDpiMode(HighDpiMode.SystemAware); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);

            // defeito dentro de um evento (clique, mudanca de valor) nao passa pelo try/catch de baixo:
            // sem isto o Windows mostra um aviso generico e a janela segue pela metade, sem deixar rastro
            Application.ThreadException += (s, e) => MostrarErro(e.Exception);

            try
            {
                Application.Run(new JanelaPrincipal(arquivo));
            }
            catch (Exception e)
            {
                MostrarErro(e);
            }
        }

        /// <summary>
        /// Mostra o defeito numa janela e grava o detalhe (com a pilha de chamadas) num arquivo de texto,
        /// para poder ser conferido depois. Grava ao lado do executavel; se nao puder, na pasta temporaria.
        /// </summary>
        private static void MostrarErro(Exception e)
        {
            string caminho = null;
            try
            {
                string pasta = Path.Combine(Caminhos.PastaDoPrograma(), "erros");
                Directory.CreateDirectory(pasta);
                caminho = Path.Combine(pasta, "KeyToJoyEmulator-Configurador-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".txt");
                File.WriteAllText(caminho, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + e.ToString());
            }
            catch
            {
                try
                {
                    caminho = Path.Combine(Path.GetTempPath(), "KeyToJoyEmulator-Configurador-erro.txt");
                    File.WriteAllText(caminho, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + e.ToString());
                }
                catch { caminho = null; }
            }

            MessageBox.Show("erro inesperado na interface: " + e.Message + Environment.NewLine + Environment.NewLine +
                            e.StackTrace +
                            (caminho == null ? "" : Environment.NewLine + Environment.NewLine + "detalhe gravado em: " + caminho),
                            "KeyToJoyEmulator", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
