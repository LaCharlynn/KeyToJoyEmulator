# Entrega 1.0.0

## Pacotes

| Arquivo | Bytes | SHA-256 |
| --- | ---: | --- |
| `KeyToJoyEmulator-portable-win-x64.zip` | 95.427.111 | `9D57DB44FBAB3D99D4EB2917A0D4B6ACC156F8202BA3E43984EB8B15907A875E` |
| `KeyToJoyEmulator-Setup-1.0.0.exe` | 69.497.381 | `F160478691C3DB84033A7504BB3ADF2A9C6FCE1701245FC1C0AE4CBCC67CEB8E` |

## Executáveis do pacote

| Arquivo | Bytes | SHA-256 |
| --- | ---: | --- |
| `KeyToJoyEmulator-Configurador.exe` | 71.160.307 | `A0609F8E34CFF1CDC7C09BF49C6B679EC27DE725B0864E6133C60472436F0F59` |
| `KeyToJoyEmulator.exe` | 35.754.794 | `0B7EF330BA497A382A836581E2B8BD3440E4B267CD2D5FB57DCD14188E168395` |

O Configurador e o pad são exatamente os binários do design aprovado
(`completo`). O pacote foi montado a partir dessa referência, sem recompilar a
interface.

## Instalador

- Instala por usuário, em `%LocalAppData%\Programs\KeyToJoyEmulator`.
- Não exige administrador.
- Cria atalho do Configurador e desinstalador.
- Não instala o driver **ViGEmBus**; ele precisa ser instalado separadamente.
- Não inclui `devices.json`, tokens, logs ou backups.
- Não tem assinatura digital; o SmartScreen pode avisar.

## Fonte

O pacote preserva os binários aprovados. A fonte do Configurador foi recuperada
por decompilação em `recovered/completo-20261018`; o processo e as limitações
estão em `docs/RECUPERACAO-DESIGN-COMPLETO.md`.

A conferência visual de 22/10/2026 contra a captura de validação do instalador
confirmou as cores, as medidas e os textos do layout. A comparação não foi pixel
a pixel e a fonte recuperada não foi recompilada.

A pasta `src-design-novo` é a fonte do layout alternativo e não gera o `completo`.
