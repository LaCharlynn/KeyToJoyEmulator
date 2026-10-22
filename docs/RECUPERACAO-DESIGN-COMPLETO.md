# Recuperação da fonte do design atual (`completo`)

Data da recuperação: 22/10/2026

## Objetivo

Recuperar a fonte do layout aprovado do KeyToJoyEmulator, o mesmo usado no pacote
`completo`, **sem alterar o visual e sem substituir os binários aprovados**.

O `completo` contém apenas os executáveis e os arquivos de execução. Não havia uma
pasta de código-fonte que gerasse exatamente esse layout. A fonte foi recuperada por
decompilação do Configurador e gravada em uma pasta separada.

## Identidade do material de origem

| Item | Valor |
| --- | --- |
| Executável de origem | `completo\KeyToJoyEmulator-Configurador.exe` |
| Tamanho do executável | `71.160.307` bytes |
| SHA-256 do executável | `A0609F8E34CFF1CDC7C09BF49C6B679EC27DE725B0864E6133C60472436F0F59` |
| Data do executável | 18/10/2026 23:22:39 |
| DLL interna usada | `KeyToJoyEmulator-Configurador.dll` |
| Tamanho da DLL | `498.688` bytes |
| SHA-256 da DLL | `B8B8C2DEE5C140CD7CD096217A14E6C3FF2128A883C4F7D7D34D14BC462BF011` |
| Extração | `%TEMP%\.net\KeyToJoyEmulator-Configurador\FtnP7DnXnZpYEPdUCWnvrMl7rKdaK8Y=\` |
| Data da extração | 18/10/2026 23:24:26 |

A DLL de `498.688` bytes também apareceu na extração `Ts3FpFsPljPEvN0RvlNRh7g5KamnhdI=`,
com o mesmo SHA-256. As DLLs maiores em `%TEMP%` pertencem a builds posteriores do
design novo e **não** foram usadas.

## Como a fonte foi recuperada

1. O executável é um aplicativo .NET de arquivo único. Ele já havia sido executado e
   deixou a DLL gerenciada extraída em `%TEMP%\.net\KeyToJoyEmulator-Configurador\Ftn...\`.
2. A DLL foi decompilada com `ilspycmd` 8.2.0.7535, usando o .NET 8 local com
   `DOTNET_ROLL_FORWARD=Major`.
3. O resultado foi gravado em:

```text
recovered\completo-20261018\
```

Comando usado, a partir da raiz do projeto:

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet <caminho-do-ilspycmd.dll> -p --nested-directories `
  -o .\recovered\completo-20261018 `
  -r <pasta-da-DLL-extraida> `
  <pasta-da-DLL-extraida>\KeyToJoyEmulator-Configurador.dll
```

## Evidências de que a fonte corresponde ao `completo`

- `Configurador\JanelaPrincipal.cs` define o título
  `KeyToJoyEmulator - configurador do controle virtual`.
- A janela é criada com `ClientSize = new Size(1270, 850)` e
  `MinimumSize = new Size(1120, 760)`, as medidas do layout aprovado.
- `Configurador\Tema.cs` contém as cores do tema escuro do `completo`:
  fundo `#17191A`, painel `#1F2223`, realce `#1ABC9C` e texto `#E7EAEC`.
- `Configurador\DesenhoControle.cs` contém o desenho do controle, os alvos, os rótulos
  dos botões e dos analógicos usados na tela.
- O recurso `Configurador.recursos.controle-xbox.png` foi recuperado e está embutido no
  projeto com o mesmo nome lógico.
- O ícone `app.ico` também foi recuperado.
- Foram gerados 18 arquivos, sendo 15 arquivos C# com 5.442 linhas no total.

## O que foi preservado

- O `completo` não foi alterado.
- O `my-version-design-novo` não foi alterado.
- Os executáveis, o portátil e o instalador não foram substituídos.
- Nenhum ajuste de cor, tamanho, texto ou desenho foi feito na fonte recuperada.
- A pasta recuperada é uma cópia separada, somente para consulta e evolução futura.

## Limitações

- É código **decompilado**, não a fonte oficial. Comentários e nomes originais de
  variáveis locais podem ter sido perdidos.
- O arquivo `.csproj` gerado pelo decompilador aponta para uma DLL em `%TEMP%`. Essa
  referência é local e não funciona em outra máquina sem ajuste.
- A dependência `KeyToJoyEmulator.dll` não foi incluída na pasta recuperada. Ela contém
  o motor do pad e precisa ser copiada ou recompilada para um build completo.
- A recuperação cobre o **Configurador** (a interface). O motor do pad não foi
  decompilado nesta etapa.
- A fonte recuperada ainda não foi compilada nem comparada pixel a pixel com o
  executável. A correspondência foi verificada por classes, recursos, medidas e cores.
- O projeto recuperado não deve ser usado para substituir o pacote aprovado antes de
  uma validação visual e funcional.

## Situação dos outros fontes

- `my-version-design-novo` é a fonte do **outro** layout, com barra superior e controle
  grande. Ela não gera o `completo` e não foi usada nesta recuperação.
- A fonte clássica está arquivada em `backups\keytojoy-classico-...` apenas como
  histórico.
- O pacote `completo` continua sendo a referência visual e de execução.

## Próximo passo seguro

Se for necessário evoluir o layout:

1. copiar a pasta recuperada para uma pasta de trabalho;
2. copiar `KeyToJoyEmulator.dll` para uma pasta `libs` e corrigir o `HintPath`;
3. compilar em uma cópia, sem tocar no `completo`;
4. comparar a tela com o executável aprovado;
5. somente depois decidir se a fonte substitui o fluxo de build.
