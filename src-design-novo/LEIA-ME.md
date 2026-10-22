# Fonte do layout alternativo (histórico)

Esta pasta guarda a fonte do layout **novo/alternativo** do KeyToJoyEmulator,
mantida como histórico de desenvolvimento. Ela **não** é a fonte do pacote
`completo` e não foi usada para gerar os binários da release 1.0.0.

O design aprovado do pacote é o `completo`. A fonte recuperada dele está em
`..\recovered\completo-20261018`.

## Conteúdo

- `src/` — projetos C# (Configurador, MyVersion, testes e verificador);
- `native/` — `ViGEmClient.dll` usado pelo pad;
- `profiles/` — perfis de exemplo;
- `tests/` — teste de aceitação.

## Build

Requer Windows x64 e .NET SDK 8:

```powershell
dotnet build src\MyVersion\MyVersion.csproj -c Release
dotnet build src\Configurador\Configurador.csproj -c Release
dotnet build src\Tests\Tests.csproj -c Release
```

O layout produzido por esta fonte é diferente do `completo`. Não substitua os
binários da release sem comparar o visual e validar o comportamento.
