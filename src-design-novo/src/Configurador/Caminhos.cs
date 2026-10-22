// Caminhos.cs - de onde vem "a pasta do programa".
//
// Num exe de ARQUIVO UNICO (self-contained, que e como o pacote ./dist e publicado), o
// `AppContext.BaseDirectory` aponta para a pasta TEMPORARIA onde o pacote foi descompactado
// (ex.: `%TEMP%\.net\KeyToJoyEmulator-Configurador\<codigo>\`). Usar aquilo fazia a interface procurar o pad e os perfis
// DENTRO da pasta temporaria: na foto da janela publicada (2026-10-18), o campo "Executavel" aparecia
// com `...\AppData\Local\Temp\.net\KeyToJoyEmulator-Configurador\XJBNWTEAYHxppSrbkmRON7Cp85LDPY=\KeyToJoyEmulator.exe`.
// O caminho de verdade do programa em execucao esta em `Environment.ProcessPath`.

using System;
using System.IO;

namespace Configurador
{
    internal static class Caminhos
    {
        /// <summary>A pasta onde o executavel esta de verdade (nao a pasta de descompactacao).</summary>
        public static string PastaDoPrograma()
        {
            string exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                string pasta = Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(pasta)) return pasta;
            }
            return AppContext.BaseDirectory;
        }
    }
}
