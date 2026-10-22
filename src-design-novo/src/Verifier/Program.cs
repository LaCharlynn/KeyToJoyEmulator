// Program.cs do verificador independente.
//
// Ele NAO sabe nada sobre o KeyToJoyEmulator: so pergunta ao Windows (XInputGetState) o que o
// controle de indice N esta reportando. E o "olho" dos testes T1 a T7 do SPEC.md.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace Verificador
{
    internal static class Program
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

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

        private const uint ERRO_SUCESSO = 0;
        private const uint ERRO_NAO_CONECTADO = 1167;

        private static int Main(string[] args)
        {
            string modo = "varrer";
            int indice = -1;
            int duracaoMs = 1000;
            int intervaloMs = 2;
            int bit = -1;
            int tempoMaxMs = 3000;
            int amostras = 0;
            bool tick = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--varrer": modo = "varrer"; break;
                    case "--amostrar": modo = "amostrar"; break;
                    case "--esperar-bit": modo = "esperar"; bit = Ler(args, ref i, "0x"); break;
                    case "--tick": tick = true; break;
                    case "--indice": indice = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--duracao-ms": duracaoMs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--intervalo-ms": intervaloMs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--tempo-max-ms": tempoMaxMs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--amostras": amostras = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                    case "--ajuda":
                        Console.WriteLine("verificar-controle [--varrer] [--amostrar --indice N --duracao-ms D --intervalo-ms I]");
                        Console.WriteLine("                   [--esperar-bit <0xXXXX> --indice N --tempo-max-ms M]");
                        return 0;
                    default:
                        Console.Error.WriteLine("opcao desconhecida: " + args[i]);
                        return 2;
                }
            }

            if (modo == "varrer") return Varrer();
            if (indice < 0) { Console.Error.WriteLine("--indice e obrigatorio neste modo"); return 2; }
            return modo == "amostrar" ? Amostrar(indice, duracaoMs, intervaloMs, amostras) : EsperarBit(indice, bit, tempoMaxMs, tick);
        }

        private static int Ler(string[] args, ref int i, string prefixo)
        {
            i++;
            if (i >= args.Length) throw new ArgumentException("falta o valor de " + prefixo);
            string s = args[i].Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt32(s.Substring(2), 16);
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        private static bool Estado(uint indice, out XINPUT_STATE estado)
        {
            estado = new XINPUT_STATE();
            uint r = XInputGetState(indice, ref estado);
            return r == ERRO_SUCESSO;
        }

        private static int Varrer()
        {
            int conectados = 0;
            for (uint i = 0; i < 4; i++)
            {
                if (Estado(i, out XINPUT_STATE e))
                {
                    conectados++;
                    Console.WriteLine($"indice={i} conectado=sim pkt={e.dwPacketNumber} botoes=0x{e.Gamepad.wButtons:X4} " +
                                      $"LX={e.Gamepad.sThumbLX} LY={e.Gamepad.sThumbLY} RX={e.Gamepad.sThumbRX} RY={e.Gamepad.sThumbRY} " +
                                      $"LT={e.Gamepad.bLeftTrigger} RT={e.Gamepad.bRightTrigger}");
                }
                else
                {
                    Console.WriteLine($"indice={i} conectado=nao");
                }
            }
            Console.WriteLine("conectados=" + conectados);
            return conectados > 0 ? 0 : 1;
        }

        private static int Amostrar(int indice, int duracaoMs, int intervaloMs, int amostras)
        {
            if (!Estado((uint)indice, out XINPUT_STATE _))
            {
                Console.Error.WriteLine("controle no indice " + indice + " nao esta conectado");
                return 3;
            }
            Console.WriteLine("# t_ms;pkt;botoes;LX;LY;RX;RY;LT;RT");
            long t0 = Environment.TickCount64;
            int n = 0;
            while (true)
            {
                long t = Environment.TickCount64 - t0;
                if (amostras > 0 ? n >= amostras : t > duracaoMs) break;
                if (Estado((uint)indice, out XINPUT_STATE e))
                {
                    Console.WriteLine($"{t};{e.dwPacketNumber};0x{e.Gamepad.wButtons:X4};{e.Gamepad.sThumbLX};{e.Gamepad.sThumbLY};" +
                                      $"{e.Gamepad.sThumbRX};{e.Gamepad.sThumbRY};{e.Gamepad.bLeftTrigger};{e.Gamepad.bRightTrigger}");
                }
                else
                {
                    Console.WriteLine(t + ";DESCONECTADO;;;;;;;");
                }
                n++;
                Thread.Sleep(intervaloMs);
            }
            return 0;
        }

        private static int EsperarBit(int indice, int bit, int tempoMaxMs, bool tick)
        {
            if (bit < 0) { Console.Error.WriteLine("--esperar-bit precisa de um valor"); return 2; }
            if (tick) Console.WriteLine("pronto tick=" + Environment.TickCount64);
            long t0 = Environment.TickCount64;
            while (Environment.TickCount64 - t0 <= tempoMaxMs)
            {
                if (Estado((uint)indice, out XINPUT_STATE e))
                {
                    if ((e.Gamepad.wButtons & (ushort)bit) != 0)
                    {
                        if (tick) Console.WriteLine("visto tick=" + Environment.TickCount64);
                        else Console.WriteLine("latencia_ms=" + (Environment.TickCount64 - t0));
                        return 0;
                    }
                }
                Thread.Sleep(1);
            }
            Console.Error.WriteLine("timeout esperando o bit 0x" + bit.ToString("X4") + " no indice " + indice);
            return 3;
        }
    }
}
