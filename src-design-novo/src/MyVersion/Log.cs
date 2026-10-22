// Log.cs - log simples para o console e, opcionalmente, para um arquivo.
//
// Nota de nomes (2026-10-18): o parametro se chamava `noConsole`, mas o efeito era o CONTRARIO (escrevia no
// console quando ele era true). Agora se chama `escreverNoConsole`, que e o que ele faz de verdade.

using System;
using System.IO;

namespace MyVersion
{
    internal sealed class Log : IDisposable
    {
        private readonly StreamWriter _arquivo;
        private readonly bool _escreverNoConsole;
        private readonly object _trava = new object();

        public string Caminho { get; }

        public Log(string caminho, bool escreverNoConsole)
        {
            _escreverNoConsole = escreverNoConsole;
            if (!string.IsNullOrWhiteSpace(caminho))
            {
                try
                {
                    string pasta = Path.GetDirectoryName(Path.GetFullPath(caminho));
                    if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
                    _arquivo = new StreamWriter(new FileStream(caminho, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                    Caminho = Path.GetFullPath(caminho);
                }
                catch (Exception e)
                {
                    Comum($"aviso: nao consegui abrir o arquivo de log '{caminho}': {e.Message}");
                }
            }
        }

        public void Comum(string texto)
        {
            string linha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + texto;
            lock (_trava)
            {
                if (_escreverNoConsole) Console.WriteLine(linha);
                _arquivo?.WriteLine(linha);
            }
        }

        public void Dispose()
        {
            lock (_trava)
            {
                try { _arquivo?.Flush(); _arquivo?.Dispose(); } catch { }
            }
        }
    }
}
