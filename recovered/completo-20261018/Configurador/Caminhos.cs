using System;
using System.IO;

namespace Configurador;

internal static class Caminhos
{
	public static string PastaDoPrograma()
	{
		string processPath = Environment.ProcessPath;
		if (!string.IsNullOrEmpty(processPath))
		{
			string directoryName = Path.GetDirectoryName(processPath);
			if (!string.IsNullOrEmpty(directoryName))
			{
				return directoryName;
			}
		}
		return AppContext.BaseDirectory;
	}
}
