using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Configurador;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		string arquivo = ((args != null && args.Length != 0) ? args[0] : null);
		try
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
		}
		catch
		{
		}
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
		{
			MostrarErro(e.Exception);
		};
		try
		{
			Application.Run(new JanelaPrincipal(arquivo));
		}
		catch (Exception e2)
		{
			MostrarErro(e2);
		}
	}

	private static void MostrarErro(Exception e)
	{
		string text = null;
		try
		{
			string text2 = Path.Combine(Caminhos.PastaDoPrograma(), "erros");
			Directory.CreateDirectory(text2);
			text = Path.Combine(text2, "KeyToJoyEmulator-Configurador-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".txt");
			File.WriteAllText(text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + e.ToString());
		}
		catch
		{
			try
			{
				text = Path.Combine(Path.GetTempPath(), "KeyToJoyEmulator-Configurador-erro.txt");
				File.WriteAllText(text, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + e.ToString());
			}
			catch
			{
				text = null;
			}
		}
		MessageBox.Show("erro inesperado na interface: " + e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace + ((text == null) ? "" : (Environment.NewLine + Environment.NewLine + "detalhe gravado em: " + text)), "KeyToJoyEmulator", MessageBoxButtons.OK, MessageBoxIcon.Hand);
	}
}
