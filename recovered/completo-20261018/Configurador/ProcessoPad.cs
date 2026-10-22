using System;
using System.Diagnostics;
using System.IO;

namespace Configurador;

public sealed class ProcessoPad : IDisposable
{
	public const string NOME_DO_PROCESSO = "KeyToJoyEmulator";

	private Process _processo;

	public bool LigadoPorMim
	{
		get
		{
			if (_processo != null)
			{
				return !_processo.HasExited;
			}
			return false;
		}
	}

	public string CaminhoDoLog { get; set; }

	public static int QuantosRodando()
	{
		try
		{
			return Process.GetProcessesByName("KeyToJoyEmulator").Length;
		}
		catch
		{
			return 0;
		}
	}

	public static string ProcurarExecutavel()
	{
		string text = Path.Combine(Caminhos.PastaDoPrograma(), "KeyToJoyEmulator.exe");
		if (File.Exists(text))
		{
			return text;
		}
		string text2 = Caminhos.PastaDoPrograma();
		for (int i = 0; i < 6; i++)
		{
			if (string.IsNullOrEmpty(text2))
			{
				break;
			}
			string[] array = new string[2]
			{
				Path.Combine(text2, "my-version", "src", "MyVersion", "bin", "Release", "net8.0", "KeyToJoyEmulator.exe"),
				Path.Combine(text2, "src", "MyVersion", "bin", "Release", "net8.0", "KeyToJoyEmulator.exe")
			};
			foreach (string text3 in array)
			{
				if (File.Exists(text3))
				{
					return text3;
				}
			}
			text2 = Directory.GetParent(text2)?.FullName;
		}
		return text;
	}

	public bool Iniciar(string exe, string arquivoDePerfis, string caminhoDoLog, out string erro)
	{
		erro = null;
		if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
		{
			erro = "nao encontrei o programa do pad: " + exe;
			return false;
		}
		if (!File.Exists(arquivoDePerfis))
		{
			erro = "nao encontrei o arquivo de perfis: " + arquivoDePerfis;
			return false;
		}
		if (QuantosRodando() > 0)
		{
			erro = "ja existe um KeyToJoyEmulator rodando (o programa aceita uma instancia so); pare antes de iniciar";
			return false;
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(exe);
		processStartInfo.Arguments = "--perfil \"" + arquivoDePerfis + "\" --log \"" + caminhoDoLog + "\"";
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		processStartInfo.WorkingDirectory = Path.GetDirectoryName(exe);
		try
		{
			_processo = Process.Start(processStartInfo);
		}
		catch (Exception ex)
		{
			erro = "nao consegui iniciar o pad: " + ex.Message;
			return false;
		}
		CaminhoDoLog = caminhoDoLog;
		if (_processo.WaitForExit(1500))
		{
			int codigo;
			try
			{
				codigo = _processo.ExitCode;
			}
			catch
			{
				codigo = -1;
			}
			erro = "o pad saiu logo depois de iniciar (codigo " + codigo + "): " + Significado(codigo);
			_processo.Dispose();
			_processo = null;
			return false;
		}
		return true;
	}

	public bool Parar(out string erro)
	{
		erro = null;
		try
		{
			if (LigadoPorMim)
			{
				_processo.Kill(entireProcessTree: true);
				_processo.WaitForExit(3000);
				return true;
			}
			Process[] processesByName = Process.GetProcessesByName("KeyToJoyEmulator");
			if (processesByName.Length == 0)
			{
				erro = "nao havia nenhum KeyToJoyEmulator rodando";
				return false;
			}
			Process[] array = processesByName;
			foreach (Process process in array)
			{
				try
				{
					process.Kill(entireProcessTree: true);
					process.WaitForExit(3000);
				}
				catch (Exception ex)
				{
					erro = "nao consegui parar o processo " + process.Id + ": " + ex.Message;
					return false;
				}
			}
			return true;
		}
		catch (Exception ex2)
		{
			erro = "erro ao parar o pad: " + ex2.Message;
			return false;
		}
	}

	public static string Significado(int codigo)
	{
		return codigo switch
		{
			0 => "terminou normalmente", 
			1 => "erro inesperado ou argumento errado", 
			2 => "arquivo de perfis invalido", 
			3 => "ja existe uma instancia do pad", 
			4 => "driver ViGEmBus nao encontrado", 
			5 => "falhou ao criar o controle virtual ou ao instalar o gancho de teclado", 
			_ => "codigo nao documentado", 
		};
	}

	public void Dispose()
	{
		if (_processo != null)
		{
			_processo.Dispose();
			_processo = null;
		}
	}
}
