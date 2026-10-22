// LeitorControle.cs - le o controle virtual pelo lado do Windows (XInputGetState).
//
// E a MESMA pergunta que o jogo faz ao sistema, e a mesma que o verificador independente
// (verificar-controle.exe) usa. A interface nao pergunta nada ao KeyToJoyEmulator: ela pergunta ao Windows.
// O arquivo e de proposito independente do projeto Verifier (o verificador tem de continuar
// independente de tudo); sao ~30 linhas de P/Invoke.

using System;
using System.Runtime.InteropServices;

namespace Configurador
{
    public static class LeitorControle
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState14(uint dwUserIndex, ref XINPUT_STATE pState);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern uint XInputGetState91(uint dwUserIndex, ref XINPUT_STATE pState);

        private const uint ERRO_SUCESSO = 0;
        private const uint ERRO_NAO_CONECTADO = 1167;

        private static bool _usarBibliotecaAntiga;

        /// <summary>O que o Windows ve no controle naquele instante.</summary>
        public struct Estado
        {
            public int Indice;        // qual dos 4 XInput respondeu
            public uint Pacote;       // dwPacketNumber
            public ushort Botoes;     // mascara dos botoes
            public byte LT;
            public byte RT;
            public short LX;
            public short LY;
            public short RX;
            public short RY;
        }

        // bits do XInput (secao 6 do SPEC.md)
        public const ushort BT_UP = 0x0001, BT_DOWN = 0x0002, BT_LEFT = 0x0004, BT_RIGHT = 0x0008;
        public const ushort BT_START = 0x0010, BT_BACK = 0x0020, BT_LS = 0x0040, BT_RS = 0x0080;
        public const ushort BT_LB = 0x0100, BT_RB = 0x0200, BT_GUIDE = 0x0400;
        public const ushort BT_A = 0x1000, BT_B = 0x2000, BT_X = 0x4000, BT_Y = 0x8000;

        /// <summary>Primeiro controle conectado (indice 0 a 3). false se nenhum responde.</summary>
        public static bool Ler(out Estado estado)
        {
            estado = default;
            for (uint i = 0; i < 4; i++)
            {
                XINPUT_STATE bruto = default;
                uint r = Chamar(i, ref bruto);
                if (r == ERRO_SUCESSO)
                {
                    estado.Indice = (int)i;
                    estado.Pacote = bruto.dwPacketNumber;
                    estado.Botoes = bruto.Gamepad.wButtons;
                    estado.LT = bruto.Gamepad.bLeftTrigger;
                    estado.RT = bruto.Gamepad.bRightTrigger;
                    estado.LX = bruto.Gamepad.sThumbLX;
                    estado.LY = bruto.Gamepad.sThumbLY;
                    estado.RX = bruto.Gamepad.sThumbRX;
                    estado.RY = bruto.Gamepad.sThumbRY;
                    return true;
                }
                if (r != ERRO_NAO_CONECTADO) return false;
            }
            return false;
        }

        private static uint Chamar(uint indice, ref XINPUT_STATE estado)
        {
            if (!_usarBibliotecaAntiga)
            {
                try { return XInputGetState14(indice, ref estado); }
                catch (DllNotFoundException) { _usarBibliotecaAntiga = true; }
                catch (EntryPointNotFoundException) { _usarBibliotecaAntiga = true; }
            }
            return XInputGetState91(indice, ref estado);
        }

        /// <summary>Para a barra na tela: -32768..32767 -> 0..200 (100 = centro).</summary>
        public static int BarraDeEixo(short valor)
        {
            int v = 100 + (int)((long)valor * 100 / 32767);
            if (v < 0) v = 0;
            if (v > 200) v = 200;
            return v;
        }
    }
}
