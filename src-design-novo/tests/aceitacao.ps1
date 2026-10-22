<#
  aceitacao.ps1 - testes T1 a T9 da secao 10 do SPEC.md.

  PRECISA DE:
    - o driver ViGEmBus instalado (senao o KeyToJoyEmulator sai com codigo 4);
    - os tres projetos compilados em Release:
        dotnet build src\MyVersion\MyVersion.csproj -c Release
        dotnet build src\Verifier\Verifier.csproj -c Release

  COMO RODAR:
    powershell -ExecutionPolicy Bypass -File tests\aceitacao.ps1
    powershell -ExecutionPolicy Bypass -File tests\aceitacao.ps1 -Teste T1,T3,T7

  QUEM VERIFICA:
    O verificador independente (`verificar-controle.exe`) le o controle pelo XInputGetState do
    proprio Windows e nao sabe nada sobre o KeyToJoyEmulator. Ele e usado como PROGRAMA em T1 e T7.
    Nas medidas de tempo (T2, T3, T6) o mesmo XInputGetState e chamado direto do script, porque
    abrir um processo novo a cada amostra custaria ~50 ms e falsearia a medida.
#>
param(
    [string[]]$Teste = @('T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9'),
    [int]$Indice = 0
)

$ErrorActionPreference = 'Stop'

$Raiz = Split-Path -Parent $PSScriptRoot
$ExePrograma = Join-Path $Raiz 'src\MyVersion\bin\Release\net8.0\KeyToJoyEmulator.exe'
$ExeVerificador = Join-Path $Raiz 'src\Verifier\bin\Release\net8.0\verificar-controle.exe'
$Perfil = Join-Path $Raiz 'profiles\exemplo.json'
$Tmp = Join-Path $env:TEMP ('KeyToJoyEmulator-aceitacao-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $Tmp | Out-Null

foreach ($caminho in @($ExePrograma, $ExeVerificador, $Perfil)) {
    if (-not (Test-Path $caminho)) { throw "nao encontrei $caminho - compile em Release antes de rodar os testes" }
}

# ------------------------------------------------------------------ ferramentas nativas

Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class TeclaTeste
{
    [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }
    [StructLayout(LayoutKind.Explicit)] public struct EntradaUniao { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public HARDWAREINPUT hi; }
    [StructLayout(LayoutKind.Sequential)] public struct ENTRADA { public uint type; public EntradaUniao u; }

    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, ENTRADA[] pInputs, int cbSize);
    [DllImport("user32.dll")] private static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)] private struct XINPUT_GAMEPAD { public ushort wButtons; public byte bLeftTrigger; public byte bRightTrigger; public short sThumbLX; public short sThumbLY; public short sThumbRX; public short sThumbRY; }
    [StructLayout(LayoutKind.Sequential)] private struct XINPUT_STATE { public uint dwPacketNumber; public XINPUT_GAMEPAD Gamepad; }
    [DllImport("xinput1_4.dll")] private static extern uint XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")] private static extern uint timeBeginPeriod(uint ms);

    /// <summary>Baixa a resolucao do relogio do sistema para 1 ms (senao Sleep(1) dorme ~15 ms).</summary>
    public static void ResolucaoDeTempo() { timeBeginPeriod(1); }

    /// <summary>
    /// Aperta (descer=true) ou solta (descer=false) uma tecla pelo SendInput.
    /// O SCAN CODE e obrigatorio: a identidade da tecla para o programa e o par
    /// (scan code, flag estendida) - se o scan code for 0, o hook ve 0x00 e nada casa.
    /// </summary>
    public static void Enviar(int vk, int scan, bool descer, bool estendida)
    {
        ENTRADA[] e = new ENTRADA[1];
        e[0].type = 1; // INPUT_KEYBOARD
        e[0].u.ki.wVk = (ushort)vk;
        e[0].u.ki.wScan = (ushort)scan;
        e[0].u.ki.dwFlags = (descer ? 0u : 2u) | (estendida ? 1u : 0u);
        SendInput(1, e, Marshal.SizeOf(typeof(ENTRADA)));
    }

    /// <summary>Como Enviar, mas deduz o scan code e a flag estendida do proprio Windows (igual ao teclado real).</summary>
    public static void EnviarAuto(int vk, bool descer)
    {
        uint r = MapVirtualKeyW((uint)vk, 4);          // MAPVK_VK_TO_VSC_EX
        if (r == 0) r = MapVirtualKeyW((uint)vk, 0);   // MAPVK_VK_TO_VSC
        // ATENCAO (medido em 2026-10-18): nesta maquina MapVirtualKeyW(VK_UP, MAPVK_VK_TO_VSC_EX) devolve
        // 0x0048, SEM o marcador 0xE0 no byte alto. Deduzir so pelo retorno mandaria as setas como se
        // fossem teclado NUMERICO - foi assim que este defeito escapou dos testes antes. A lista abaixo
        // e a mesma regra do Teclado.cs.
        bool estendida = ((r >> 8) & 0xFF) == 0xE0 || ((r >> 8) & 0xFF) == 0xE1 || EhEstendida(vk);
        Enviar(vk, (int)(r & 0xFF), descer, estendida);
    }

    /// <summary>VK que o Windows entrega com a flag estendida ligada (o prefixo E0 do scan code).</summary>
    private static bool EhEstendida(int vk)
    {
        switch (vk)
        {
            case 0x21: case 0x22: case 0x23: case 0x24:
            case 0x25: case 0x26: case 0x27: case 0x28:
            case 0x2D: case 0x2E: case 0xA3: case 0xA5:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Le o estado do controle: [pkt, botoes, LX, LY, RX, RY, LT, RT] ou null se nao conectado.</summary>
    public static int[] Ler(int indice)
    {
        XINPUT_STATE s = new XINPUT_STATE();
        if (XInputGetState((uint)indice, ref s) != 0) return null;
        return new int[] { (int)s.dwPacketNumber, s.Gamepad.wButtons, s.Gamepad.sThumbLX, s.Gamepad.sThumbLY, s.Gamepad.sThumbRX, s.Gamepad.sThumbRY, s.Gamepad.bLeftTrigger, s.Gamepad.bRightTrigger };
    }

    /// <summary>-1 quando o controle nao esta conectado.</summary>
    public static int Botoes(int indice) { int[] e = Ler(indice); return e == null ? -1 : e[1]; }

    /// <summary>
    /// Latencia de UMA tecla, em MICROssegundos: aperta, espera o bit aparecer num laco apertado
    /// e solta. -1 quando o bit nao aparece em 1 s. Fica em C# de proposito: um laco escrito em
    /// PowerShell custa ~35-70 us por volta (medido em 2026-10-18), o que injeta erro na medida.
    /// </summary>
    public static int LatenciaDeUmaTecla(int indice, int vk, int bit)
    {
        Stopwatch cronometro = Stopwatch.StartNew();
        EnviarAuto(vk, true);
        int achou = -1;
        while (cronometro.ElapsedTicks < Stopwatch.Frequency)
        {
            int[] e = Ler(indice);
            if (e != null && (e[1] & bit) != 0)
            {
                achou = (int)(cronometro.ElapsedTicks * 1000000L / Stopwatch.Frequency);
                break;
            }
        }
        EnviarAuto(vk, false);
        return achou;
    }
}
"@

[TeclaTeste]::ResolucaoDeTempo()

# ------------------------------------------------------------------ utilidades do script

$VK = @{
    Enter = 0x0D; Space = 0x20; Ctrl = 0xA2; Alt = 0xA4
    A = 0x41; D = 0x44; S = 0x53; W = 0x57; I = 0x49; J = 0x4A; K = 0x4B
    Um = 0x31; Dois = 0x32
}
$BIT = @{
    UP = 0x0001; DOWN = 0x0002; LEFT = 0x0004; RIGHT = 0x0008
    START = 0x0010; BACK = 0x0020; LS = 0x0040; RS = 0x0080
    LB = 0x0100; RB = 0x0200; A = 0x1000; B = 0x2000; X = 0x4000; Y = 0x8000
}

$script:resultados = New-Object System.Collections.ArrayList
function Registrar([string]$nome, [bool]$ok, [string]$detalhe = '') {
    [void]$script:resultados.Add([pscustomobject]@{ Nome = $nome; Ok = $ok; Detalhe = $detalhe })
    $marca = if ($ok) { 'PASS' } else { 'FALHA' }
    $extra = if ($detalhe) { "  ($detalhe)" } else { '' }
    Write-Host ("{0} {1}{2}" -f $marca, $nome, $extra)
}

function Tecla([int]$vk, [bool]$descer, [bool]$estendida = $false) { [TeclaTeste]::EnviarAuto($vk, $descer) }
function Aspas([string]$s) { return ('"' + $s + '"') }   # caminhos podem ter espaco (ex.: "Projeto Padrao")
function Apertar([int]$vk) { Tecla $vk $true }
function Soltar([int]$vk) { Tecla $vk $false }
function Bater([int]$vk, [int]$ms = 120) { Apertar $vk; Start-Sleep -Milliseconds $ms; Soltar $vk }
function Segurar([int[]]$vks, [int]$ms) {
    foreach ($k in $vks) { Apertar $k }
    Start-Sleep -Milliseconds $ms
    foreach ($k in $vks) { Soltar $k }
    Start-Sleep -Milliseconds 200
}
function Estado { return [TeclaTeste]::Ler($Indice) }
function BotoesAgora { $e = Estado; if ($e -eq $null) { return -1 } else { return $e[1] } }
function TemBit([int]$bit) { $b = BotoesAgora; if ($b -lt 0) { return $false } else { return (($b -band $bit) -ne 0) } }
function Hex4([int]$v) { return ("0x{0:X4}" -f $v) }

function Iniciar-Programa {
    param([string]$perfilInicial = $null, [string]$backend = 'vigem', [string[]]$Extras = @())
    $argumentos = @('--perfil', (Aspas $Perfil), '--log', (Aspas (Join-Path $Tmp 'KeyToJoyEmulator.log')))
    if ($perfilInicial) { $argumentos += @('--perfil-inicial', $perfilInicial) }
    if ($backend) { $argumentos += @('--backend', $backend) }
    $argumentos += $Extras
    $p = Start-Process -FilePath $ExePrograma -ArgumentList $argumentos -PassThru -WindowStyle Hidden
    for ($i = 0; $i -lt 120; $i++) {
        if ($p.HasExited) { throw "o KeyToJoyEmulator terminou sozinho (codigo $($p.ExitCode)); veja o log em $Tmp" }
        if ([TeclaTeste]::Botoes($Indice) -ge 0) { break }
        Start-Sleep -Milliseconds 50
    }
    Start-Sleep -Milliseconds 400
    return $p
}

function Parar-Programa {
    param($Processo, [switch]$SemEsperar)
    if ($Processo -and -not $Processo.HasExited) {
        Stop-Process -Id $Processo.Id -Force -ErrorAction SilentlyContinue
        $Processo.WaitForExit(3000) | Out-Null
    }
    if (-not $SemEsperar) {
        for ($i = 0; $i -lt 40; $i++) { if ([TeclaTeste]::Botoes($Indice) -lt 0) { break }; Start-Sleep -Milliseconds 50 }
    }
}

function IrParaPerfilJogoA {
    Segurar @($VK.Ctrl, $VK.Alt, $VK.Um) 200
    Start-Sleep -Milliseconds 150
}

# ------------------------------------------------------------------ T1: tecla -> botao

function Teste-T1 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        $inicial = Estado
        Registrar 'T1c ao subir, o controle ja aparece com os eixos zerados e nenhum botao' `
                  ($null -ne $inicial -and $inicial[1] -eq 0 -and $inicial[2] -eq 0 -and $inicial[3] -eq 0 -and $inicial[4] -eq 0 -and $inicial[5] -eq 0) `
                  ("botoes=" + (Hex4 $inicial[1]) + " LX=$($inicial[2]) LY=$($inicial[3]) RX=$($inicial[4]) RY=$($inicial[5]) LT=$($inicial[6]) RT=$($inicial[7])")

        Apertar $VK.Enter
        Start-Sleep -Milliseconds 150
        $saida = & $ExeVerificador --amostrar --indice $Indice --duracao-ms 60 --intervalo-ms 4 2>&1
        Soltar $VK.Enter
        Start-Sleep -Milliseconds 250
        $saidaDepois = & $ExeVerificador --amostrar --indice $Indice --duracao-ms 60 --intervalo-ms 4 2>&1

        $linhas = $saida | Where-Object { $_ -match '^\d+;' }
        $linhasDepois = $saidaDepois | Where-Object { $_ -match '^\d+;' }
        $comA = @($linhas | Where-Object { ([Convert]::ToInt32(($_ -split ';')[2].Replace('0x', ''), 16) -band $BIT.A) -ne 0 }).Count
        $semA = @($linhasDepois | Where-Object { ([Convert]::ToInt32(($_ -split ';')[2].Replace('0x', ''), 16) -band $BIT.A) -eq 0 }).Count

        Registrar 'T1a apertando Enter o verificador ve o botao A ligado' ($comA -gt 0) "amostras com A=$comA de $($linhas.Count)"
        Registrar 'T1b soltando Enter o verificador ve o botao A desligado' ($semA -eq $linhasDepois.Count -and $linhasDepois.Count -gt 0) "amostras sem A=$semA de $($linhasDepois.Count)"
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T2: latencia

function Teste-T2 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        # Latencia = do instante em que a tecla desce ate o instante em que o XInputGetState do
        # proprio Windows ve o bit. A medida roda em C# (LatenciaDeUmaTecla), com Stopwatch.
        #
        # Duas correcoes de instrumento, medidas em 2026-10-18 (achado 108):
        #  (a) laco em PowerShell custa ~35-70 us por volta - falso precisao perto de 16 ms;
        #  (b) a PRIMEIRA tecla depois de subir o programa paga custos de uma vez so (JIT, primeiro
        #      tique, criacao do arquivo de log). Numa unica amostra ela decide o teste inteiro e
        #      transforma um programa de ~5 ms numa reprovacao de 32-68 ms. O jogador nunca sente
        #      esse custo duas vezes, entao a primeira tecla e AQUECIMENTO e fica fora da conta.
        # O criterio continua sendo 16 ms; o que muda e nao julgar por uma amostra so.
        $null = [TeclaTeste]::LatenciaDeUmaTecla($Indice, $VK.Enter, $BIT.A)   # aquecimento
        Start-Sleep -Milliseconds 250

        $amostras = @()
        for ($i = 0; $i -lt 20; $i++) {
            $us = [TeclaTeste]::LatenciaDeUmaTecla($Indice, $VK.Enter, $BIT.A)
            if ($us -lt 0) { $amostras = @(); break }    # o bit nao apareceu em 1 s: reprova
            $amostras += $us
            Start-Sleep -Milliseconds 100
        }

        $nome = 'T2 latencia da tecla ate o bit aparecer (mediana de 20) <= 16 ms'
        if ($amostras.Count -eq 0) {
            Registrar $nome $false 'o bit nao apareceu em 1 s em alguma amostra'
        } else {
            $ord = $amostras | Sort-Object
            $mediana = $ord[[int][Math]::Floor(($ord.Count - 1) / 2)]
            $detalhe = 'mediana {0:N2} ms (min {1:N2} / max {2:N2} ms, {3} amostras)' -f `
                       ($mediana / 1000.0), ($ord[0] / 1000.0), ($ord[$ord.Count - 1] / 1000.0), $ord.Count
            Registrar $nome ($mediana -le 16000) $detalhe
        }
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T3: rampa do eixo

function Teste-T3 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        $noventa = [int](32767 * 0.9)
        $dez = [int](32767 * 0.1)

        Apertar $VK.D
        $t0 = [Environment]::TickCount
        $t90 = -1; $maximo = -99999; $passouDoLimite = $false
        while ([Environment]::TickCount - $t0 -lt 700) {
            $e = Estado
            if ($null -ne $e) {
                if ($e[2] -gt $maximo) { $maximo = $e[2] }
                if ($e[2] -gt 32767) { $passouDoLimite = $true }
                if ($t90 -lt 0 -and $e[2] -ge $noventa) { $t90 = [int]([Environment]::TickCount - $t0) }
            }
            Start-Sleep -Milliseconds 2
        }
        Soltar $VK.D

        $t1 = [Environment]::TickCount
        $voltou = -1
        while ([Environment]::TickCount - $t1 -lt 700) {
            $e = Estado
            if ($null -ne $e -and $e[2] -le $dez) { $voltou = [int]([Environment]::TickCount - $t1); break }
            Start-Sleep -Milliseconds 2
        }

        Registrar 'T3a a rampa chega a 90% do maximo em ate 180 ms' ($t90 -ge 0 -and $t90 -le 180) "t90=${t90} ms (rampa configurada 150 ms)"
        Registrar 'T3b a rampa nao passa do maximo da faixa' (-not $passouDoLimite) "maximo visto=$maximo"
        Registrar 'T3c soltando, o eixo volta ao repouso em ate 180 ms' ($voltou -ge 0 -and $voltou -le 180) "t=${voltou} ms"
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T4: soma e zona morta

function Teste-T4 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        Apertar $VK.D
        Start-Sleep -Milliseconds 400
        Apertar $VK.A
        Start-Sleep -Milliseconds 400
        $e = Estado
        Registrar 'T4a duas teclas opostas no mesmo eixo somam ~0' ($null -ne $e -and [Math]::Abs($e[2]) -le 1000) "LX=$($e[2])"
        Soltar $VK.A; Soltar $VK.D
        Start-Sleep -Milliseconds 400

        IrParaPerfilJogoA
        $e = Estado
        Registrar 'T4b atalho troca para o perfil de zona morta alta' ($null -ne $e) ''

        # no perfil jogo-a: D -> LX com rampa de 1000 ms e zona morta de 8000
        Apertar $VK.D
        $t0 = [Environment]::TickCount
        $abaixo = New-Object System.Collections.ArrayList
        while ([Environment]::TickCount - $t0 -lt 150) {
            $x = Estado
            if ($null -ne $x) { [void]$abaixo.Add($x[2]) }
            Start-Sleep -Milliseconds 3
        }
        $naoZero = @($abaixo | Where-Object { $_ -ne 0 }).Count
        Registrar 'T4c valor abaixo da zona morta sai exatamente 0' ($abaixo.Count -gt 0 -and $naoZero -eq 0) "amostras=$($abaixo.Count) nao-zero=$naoZero"

        Start-Sleep -Milliseconds 1200
        $x = Estado
        Soltar $VK.D
        Registrar 'T4d acima da zona morta o valor passa' ($null -ne $x -and $x[2] -gt 8000) "LX=$($x[2])"
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T5: troca de perfil

function Teste-T5 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        Apertar $VK.Enter
        Start-Sleep -Milliseconds 200
        Registrar 'T5a no perfil navegacao, Enter acende A' (TemBit $BIT.A) (Hex4 (BotoesAgora))

        Segurar @($VK.Ctrl, $VK.Alt, $VK.Um) 250      # Enter continua apertado
        Start-Sleep -Milliseconds 200

        $botoes = BotoesAgora
        Registrar 'T5b o botao do perfil antigo nao fica preso (A apagou)' (($botoes -band $BIT.A) -eq 0) (Hex4 $botoes)
        Registrar 'T5c o perfil novo responde (Enter agora e START)' (($botoes -band $BIT.START) -ne 0) (Hex4 $botoes)

        Soltar $VK.Enter
        Start-Sleep -Milliseconds 250
        Registrar 'T5d soltando Enter o START apaga' (-not (TemBit $BIT.START)) (Hex4 (BotoesAgora))
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T6: auto-repeat

function Teste-T6 {
    $p = Iniciar-Programa -perfilInicial 'navegacao'
    try {
        Apertar $VK.Space
        # O que o teste quer dizer: A PARTIR do momento em que o botao esta aceso, segurar a tecla
        # (com auto-repeat) nao pode apaga-lo nem por UMA amostra. Sem esta espera, a primeira
        # amostra podia cair ANTES da primeira publicacao do estado e ser contada como "piscada" -
        # foi o que aconteceu em 2026-10-18 (amostras=63 sem B=1, na primeira posicao).
        $acendeu = $false
        $limite = [Environment]::TickCount
        while ([Environment]::TickCount - $limite -lt 1000) {
            $e = Estado
            if ($null -ne $e -and ($e[1] -band $BIT.B) -ne 0) { $acendeu = $true; break }
        }

        $faltas = New-Object System.Collections.Generic.List[int]
        $t0 = [Environment]::TickCount
        $amostras = 0
        while ([Environment]::TickCount - $t0 -lt 2000) {
            Apertar $VK.Space                     # simula o auto-repeat do teclado fisico
            $e = Estado
            if ($null -ne $e) {
                $amostras++
                if (($e[1] -band $BIT.B) -eq 0) { $faltas.Add($amostras) }   # guarda ONDE faltou
            }
            Start-Sleep -Milliseconds 20
        }
        Soltar $VK.Space
        Start-Sleep -Milliseconds 250
        $depois = BotoesAgora

        $detalhe = "amostras=$amostras sem B=$($faltas.Count)"
        if ($faltas.Count -gt 0) { $detalhe += " (posicoes: " + ($faltas -join ',') + ")" }
        Registrar 'T6a segurando Space (com auto-repeat) o botao B nunca pisca' ($acendeu -and $amostras -gt 30 -and $faltas.Count -eq 0) $detalhe
        Registrar 'T6b soltando Space o botao B apaga' (($depois -band $BIT.B) -eq 0) (Hex4 $depois)
    }
    finally { Parar-Programa $p }
}

# ------------------------------------------------------------------ T7: encerramento

function Teste-T7 {
    # T7a: encerramento normal (o proprio programa se encerra)
    $p = Iniciar-Programa -perfilInicial 'navegacao' -Extras @('--encerrar-apos-ms', '1500')
    Registrar 'T7a antes de encerrar, o controle virtual existe' ([TeclaTeste]::Botoes($Indice) -ge 0) (Hex4 (BotoesAgora))
    $p.WaitForExit(6000) | Out-Null
    $sumiuNormal = $false
    for ($i = 0; $i -lt 60; $i++) { if ([TeclaTeste]::Botoes($Indice) -lt 0) { $sumiuNormal = $true; break }; Start-Sleep -Milliseconds 100 }
    Registrar 'T7b encerrando normalmente, o controle virtual desaparece' $sumiuNormal ''

    # T7c: encerramento a forca (equivale a taskkill /F)
    $p2 = Iniciar-Programa -perfilInicial 'navegacao'
    Start-Sleep -Milliseconds 500
    Stop-Process -Id $p2.Id -Force -ErrorAction SilentlyContinue
    $p2.WaitForExit(3000) | Out-Null
    $sumiuForca = $false
    for ($i = 0; $i -lt 80; $i++) { if ([TeclaTeste]::Botoes($Indice) -lt 0) { $sumiuForca = $true; break }; Start-Sleep -Milliseconds 100 }
    Registrar 'T7c encerrando a forca, o Windows remove o controle virtual' $sumiuForca 'item [UNKNOWN] do SPEC.md'
}

# ------------------------------------------------------------------ T8: arquivo invalido

function Teste-T8 {
    $quebrado = Join-Path $Tmp 'perfil-quebrado.json'
    '{ "versao": 1, "perfis": ' | Set-Content -Path $quebrado -Encoding ascii

    $saida = Join-Path $Tmp 't8.out'
    $erro = Join-Path $Tmp 't8.err'
    $a = Start-Process -FilePath $ExePrograma -ArgumentList @('--perfil', (Aspas $quebrado), '--sem-log') -PassThru -Wait -RedirectStandardOutput $saida -RedirectStandardError $erro
    $textoErro = Get-Content $erro -Raw -ErrorAction SilentlyContinue
    Registrar 'T8a JSON quebrado: sai com codigo 2' ($a.ExitCode -eq 2) "codigo=$($a.ExitCode)"
    Registrar 'T8b JSON quebrado: mensagem clara na saida de erro' ($textoErro -match 'JSON invalido') ($textoErro -replace "`r?`n", ' ')

    $alvoRuim = Join-Path $Tmp 'perfil-alvo-ruim.json'
    '{ "versao": 1, "perfil_inicial": "p", "perfis": { "p": { "mapeamentos": [ { "tecla": "Enter", "tipo": "botao", "alvo": "AA" } ] } } }' | Set-Content -Path $alvoRuim -Encoding ascii
    $b = Start-Process -FilePath $ExePrograma -ArgumentList @('--perfil', (Aspas $alvoRuim), '--sem-log') -PassThru -Wait -RedirectStandardOutput $saida -RedirectStandardError $erro
    $textoErro = Get-Content $erro -Raw -ErrorAction SilentlyContinue
    Registrar 'T8c alvo inexistente: sai com codigo 2' ($b.ExitCode -eq 2) "codigo=$($b.ExitCode)"
    Registrar 'T8d alvo inexistente: mensagem clara na saida de erro' ($textoErro -match 'alvo de botao invalido') ($textoErro -replace "`r?`n", ' ')

    $c = Start-Process -FilePath $ExePrograma -ArgumentList @('--validar', (Aspas $alvoRuim)) -PassThru -Wait -RedirectStandardOutput $saida -RedirectStandardError $erro
    Registrar 'T8e --validar tambem recusa com codigo 2' ($c.ExitCode -eq 2) "codigo=$($c.ExitCode)"
    Registrar 'T8f nada de controle virtual foi criado nos casos invalidos' ([TeclaTeste]::Botoes($Indice) -lt 0) ''
}

# ------------------------------------------------------------------ T9: instancia unica

function Teste-T9 {
    $primeiro = Start-Process -FilePath $ExePrograma -ArgumentList @('--perfil', (Aspas $Perfil), '--backend', 'falso', '--log', (Aspas (Join-Path $Tmp 't9.log')), '--encerrar-apos-ms', '5000') -PassThru -WindowStyle Hidden
    Start-Sleep -Milliseconds 900
    if ($primeiro.HasExited) {
        Registrar 'T9 a segunda instancia sai com codigo 3' $false ('a primeira instancia saiu sozinha (codigo ' + $primeiro.ExitCode + '); teste invalido')
        return
    }
    $saida = Join-Path $Tmp 't9.out'
    $erro = Join-Path $Tmp 't9.err'
    $segundo = Start-Process -FilePath $ExePrograma -ArgumentList @('--perfil', (Aspas $Perfil), '--backend', 'falso', '--sem-log') -PassThru -Wait -RedirectStandardOutput $saida -RedirectStandardError $erro
    Registrar 'T9 a segunda instancia sai com codigo 3' ($segundo.ExitCode -eq 3) "codigo=$($segundo.ExitCode)"
    if (-not $primeiro.HasExited) { Stop-Process -Id $primeiro.Id -Force -ErrorAction SilentlyContinue }
}

# ------------------------------------------------------------------ execucao

Write-Host "testes de aceitacao do KeyToJoyEmulator (indice XInput $Indice)"
Write-Host "pasta temporaria: $Tmp"
Write-Host ("PRIMEIRA LINHA DA AJUDA: " + (& $ExePrograma --ajuda | Select-Object -First 1))
Write-Host ''

$mapa = [ordered]@{
    'T1' = 'Teste-T1'; 'T2' = 'Teste-T2'; 'T3' = 'Teste-T3'; 'T4' = 'Teste-T4'; 'T5' = 'Teste-T5'
    'T6' = 'Teste-T6'; 'T7' = 'Teste-T7'; 'T8' = 'Teste-T8'; 'T9' = 'Teste-T9'
}

$escolhidos = @()
foreach ($t in $Teste) {
    foreach ($pedaco in ($t -split ',')) {
        $nome = $pedaco.Trim().ToUpperInvariant()
        if (-not $mapa.Contains($nome)) { throw "teste desconhecido: $nome" }
        if ($escolhidos -notcontains $nome) { $escolhidos += $nome }
    }
}

foreach ($nome in $escolhidos) {
    Write-Host ("--- $nome " + ('-' * 60))
    & $mapa[$nome]
    Write-Host ''
}

$falhas = @($script:resultados | Where-Object { -not $_.Ok })
Write-Host ('=' * 72)
Write-Host ("resultado: " + $script:resultados.Count + " verificacoes, " + $falhas.Count + " falhas")
if ($falhas.Count -gt 0) {
    foreach ($f in $falhas) { Write-Host ("  FALHOU: " + $f.Nome + $(if ($f.Detalhe) { "  ($($f.Detalhe))" } else { '' })) }
}
Write-Host ('=' * 72)

exit $(if ($falhas.Count -eq 0) { 0 } else { 1 })
