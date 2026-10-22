# KeyToJoyEmulator

Emulador de controle Xbox para Windows que transforma teclas do teclado em comandos
de gamepad. O teclado passa a funcionar como um controle virtual para jogos e
programas que esperam XInput.

> Desenvolvido com **REA (Reverse Engineering Anything)** e **IA**.

## O que faz

- Instala um gancho global de teclado.
- Converte teclas em botões e eixos analógicos de um controle Xbox 360 virtual.
- Usa perfis por jogo, com troca de perfil por atalho.
- Tem rampa de aceleração, zona morta e sensibilidade configuráveis.
- Apresenta o controle ao Windows pelo driver **ViGEmBus**.
- Inclui um Configurador visual para editar perfis e testar o resultado ao vivo.

## Download

Os pacotes prontos ficam em **Releases**:

- `KeyToJoyEmulator-portable-win-x64.zip` — versão portátil;
- `KeyToJoyEmulator-Setup-1.0.0.exe` — instalador por usuário, sem administrador.

O pacote **não inclui o driver ViGEmBus**. Instale-o separadamente antes de usar o
pad. Se o programa sair com código 4, o driver está faltando.

O instalador não tem assinatura digital, então o Windows pode exibir um aviso do
SmartScreen.

## Como usar

1. Instale o driver **ViGEmBus**.
2. Extraia o portátil ou instale pelo `Setup`.
3. Abra `KeyToJoyEmulator-Configurador.exe`.
4. Escolha ou crie um perfil, mapeie as teclas e clique em **Ligar o pad**.
5. Abra o jogo; o Windows verá um controle Xbox 360 virtual.

O pad também pode ser iniciado sem a interface:

```powershell
KeyToJoyEmulator.exe --perfil profiles\corrida.json --log meu-log.txt
```

## Design e fonte

O pacote 1.0.0 usa exatamente os binários do design aprovado (`completo`). Os
hashes e detalhes da entrega estão em [`docs/ENTREGA.md`](docs/ENTREGA.md).

A fonte do Configurador desse design foi recuperada por decompilação e está em
[`recovered/completo-20261018`](recovered/completo-20261018). Ela não é a fonte
oficial: é uma reconstrução, com limitações e sem os comentários originais. O
processo, as evidências e os próximos passos seguros estão em
[`docs/RECUPERACAO-DESIGN-COMPLETO.md`](docs/RECUPERACAO-DESIGN-COMPLETO.md).
A conferência visual contra a captura de validação confirmou as cores, as medidas
e os textos do layout; a comparação não foi pixel a pixel e a fonte não foi
recompilada.

A pasta [`src-design-novo`](src-design-novo) contém a fonte de um layout
alternativo, mantida como histórico de desenvolvimento. Ela **não** gera o layout
do pacote `completo` e não foi usada para produzir os binários da release.

Nenhum binário, perfil, log ou elemento visual do pacote aprovado foi alterado
durante a recuperação.

## Estrutura do repositório

```text
docs/                         documentação da entrega e da recuperação
installer/KeyToJoy.iss        script do instalador Inno Setup
recovered/completo-20261018/  fonte decompilada do Configurador aprovado
src-design-novo/              fonte do layout alternativo (histórico)
```

## Requisitos de desenvolvimento

- Windows 10/11 x64.
- .NET SDK 8 para compilar a fonte alternativa.
- Driver ViGEmBus para executar o pad.
- Para o projeto recuperado, leia as limitações antes de tentar compilar.

## Privacidade

O repositório não inclui perfis pessoais, logs, backups, tokens, credenciais ou
chaves de dispositivo. O Configurador e o pacote não enviam dados para servidores
do projeto.

## Créditos

- **ViGEmBus / ViGEm Client** — driver e biblioteca de controle virtual.
- **JetBrains Mono** — fonte usada no layout alternativo, sob SIL Open Font
  License 1.1.

KeyToJoyEmulator não é afiliado à Microsoft, Sony ou ao JoyToKey.
