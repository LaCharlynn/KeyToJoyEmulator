# Fonte recuperada do Configurador `completo`

Esta pasta contém a saída bruta da decompilação do
`KeyToJoyEmulator-Configurador.dll` do pacote `completo`, feita em 22/10/2026.

- DLL de origem: SHA-256 `B8B8C2DEE5C140CD7CD096217A14E6C3FF2128A883C4F7D7D34D14BC462BF011`
- Executável de origem: SHA-256 `A0609F8E34CFF1CDC7C09BF49C6B679EC27DE725B0864E6133C60472436F0F59`
- Decompilador: `ilspycmd` 8.2.0.7535
- 18 arquivos, 15 fontes C#, 5.442 linhas

Isto é uma **reconstrução por decompilação**, não a fonte oficial. O projeto
`.csproj` aponta para uma DLL em `%TEMP%`; a dependência `KeyToJoyEmulator.dll`
não está incluída. Veja `..\..\RECUPERACAO-DESIGN-COMPLETO.md` para as
evidências, limitações e o próximo passo seguro.

## Conferência visual

Em 22/10/2026, a fonte foi conferida contra a captura
`dist\validacao-instalador-completo.png`: fundo `#17191A`, painel `#1F2223`,
painel 2 `#292D2E`, borda `#3A3F40` e realce escuro `#145C4F` batem com
`Configurador\Tema.cs`; a janela de 1270×850 e os textos do layout batem com
`JanelaPrincipal.cs`. A comparação não foi pixel a pixel e a fonte não foi
recompilada.

Nenhum binário, layout ou arquivo do `completo` foi alterado para produzir esta
pasta.
