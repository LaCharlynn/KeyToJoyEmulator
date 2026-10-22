// Teclado.cs - captura global de teclado (hook de baixo nivel) e tabela de nomes de tecla.
//
// Espelha o que o alvo analisado carrega na injecao (REA-FINDINGS.md, Entrada 37):
// a identidade de uma tecla e o par (SCAN CODE, flag de tecla ESTENDIDA), nao apenas a letra.
// No lado do hook, o Windows entrega exatamente isso em KBDLLHOOKSTRUCT.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MyVersion
{
    internal static class Nativo
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int HC_ACTION = 0;

        public const uint LLKHF_EXTENDED = 0x01;
        public const uint LLKHF_UP = 0x80;

        public const uint WM_QUIT = 0x0012;

        public const uint MAPVK_VK_TO_VSC = 0;
        public const uint MAPVK_VK_TO_VSC_EX = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandleW(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyW(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostThreadMessageW(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();
    }

    /// <summary>Uma tecla capturada pelo hook: scan code + flag estendida.</summary>
    public readonly struct CodigoTecla : IEquatable<CodigoTecla>
    {
        public readonly int Scan;
        public readonly bool Estendida;

        public CodigoTecla(int scan, bool estendida)
        {
            Scan = scan & 0xFF;
            Estendida = estendida;
        }

        public int Chave => Scan | (Estendida ? 0x100 : 0);

        public bool Equals(CodigoTecla outro) => Scan == outro.Scan && Estendida == outro.Estendida;

        public override bool Equals(object obj) => obj is CodigoTecla c && Equals(c);

        public override int GetHashCode() => Chave;

        public override string ToString() => Estendida ? $"scan 0x{Scan:X2} (estendida)" : $"scan 0x{Scan:X2}";
    }

    /// <summary>
    /// Tabela nome canonico -> (scan code, estendida).
    /// Os nomes seguem a secao 8.3 do SPEC.md. O scan code e obtido do Windows
    /// (MapVirtualKeyW), nunca escrito a mao, para nao errar teclas de layout.
    /// </summary>
    public static class TabelaTeclas
    {    // virtual-key codes do Windows (o nome canonico -> VK)
        private static readonly Dictionary<string, uint> VirtualKeys = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "Backspace", 0x08 }, { "Tab", 0x09 }, { "Enter", 0x0D }, { "Esc", 0x1B }, { "Space", 0x20 },
            { "PageUp", 0x21 }, { "PageDown", 0x22 }, { "End", 0x23 }, { "Home", 0x24 },
            { "Left", 0x25 }, { "Up", 0x26 }, { "Right", 0x27 }, { "Down", 0x28 },
            { "Insert", 0x2D }, { "Delete", 0x2E },
            { "Shift", 0xA0 }, { "LeftShift", 0xA0 }, { "RightShift", 0xA1 },
            { "Control", 0xA2 }, { "LeftControl", 0xA2 }, { "RightControl", 0xA3 },
            { "Alt", 0xA4 }, { "LeftAlt", 0xA4 }, { "RightAlt", 0xA5 },
        };

        // apelidos aceitos no arquivo de perfil (ex.: "Ctrl+Alt+1")
        private static readonly Dictionary<string, string> Apelidos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Ctrl", "Control" }, { "Ctl", "Control" },
            { "Shift", "Shift" }, { "AltGr", "RightAlt" }, { "Return", "Enter" },
            { "Escape", "Esc" }, { "Spacebar", "Space" }, { "Del", "Delete" }, { "Ins", "Insert" },
            { "PgUp", "PageUp" }, { "PgDn", "PageDown" },
        };

        static TabelaTeclas()
        {
            for (char c = 'A'; c <= 'Z'; c++) VirtualKeys[c.ToString()] = (uint)c;
            for (char c = '0'; c <= '9'; c++) VirtualKeys[c.ToString()] = (uint)c;
            for (int i = 1; i <= 12; i++) VirtualKeys["F" + i] = (uint)(0x6F + i);
        }

        public static IEnumerable<string> NomesCanonicos => VirtualKeys.Keys;

        /// <summary>Traduz um nome (com apelidos) para o nome canonico; null se nao existe.</summary>
        public static string Canonico(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return null;
            string n = nome.Trim();
            if (Apelidos.TryGetValue(n, out string alias)) n = alias;
            return VirtualKeys.ContainsKey(n) ? n : null;
        }

        /// <summary>Resolve um nome para (scan code, estendida).</summary>
        public static bool TentarResolver(string nome, out CodigoTecla tecla)
        {
            tecla = default;
            string canonico = Canonico(nome);
            if (canonico == null) return false;
            uint vk = VirtualKeys[canonico];
            uint r = Nativo.MapVirtualKeyW(vk, Nativo.MAPVK_VK_TO_VSC_EX);
            if (r == 0) r = Nativo.MapVirtualKeyW(vk, Nativo.MAPVK_VK_TO_VSC);
            if (r == 0) return false;
            // O Windows marca a tecla estendida com 0xE0/0xE1 no byte alto do retorno - MAS nesta maquina
            // (medido em 2026-10-18) MapVirtualKeyW(VK_UP, MAPVK_VK_TO_VSC_EX) devolve 0x0048, SEM a
            // marca. Sem a lista `EhEstendida` as setas saiam com a identidade do teclado NUMERICO
            // (mesmo scan code, sem a flag LLKHF_EXTENDED) e portanto nunca casavam com a tecla fisica:
            // era o defeito do D-PAD e da camera nas setas. A lista e a rede de seguranca.
            bool estendida = ((r >> 8) & 0xFF) == 0xE0 || ((r >> 8) & 0xFF) == 0xE1 || EhEstendida(vk);
            tecla = new CodigoTecla((int)(r & 0xFF), estendida);
            return true;
        }

        /// <summary>
        /// Teclas que o Windows entrega com a flag LLKHF_EXTENDED ligada (o prefixo E0 do scan code).
        /// Necessaria porque MapVirtualKeyW(MAPVK_VK_TO_VSC_EX) NAO marca as setas nesta maquina
        /// (medido: VK_UP -> 0x0048). Os valores sao os VK do Windows; os seus scan codes de teclado
        /// NUMERICO sao os mesmos, e e a flag estendida que separa os dois blocos.
        /// </summary>
        private static bool EhEstendida(uint vk)
        {
            switch (vk)
            {
                case 0x21:  // PageUp
                case 0x22:  // PageDown
                case 0x23:  // End
                case 0x24:  // Home
                case 0x25:  // Left
                case 0x26:  // Up
                case 0x27:  // Right
                case 0x28:  // Down
                case 0x2D:  // Insert
                case 0x2E:  // Delete
                case 0xA3:  // RightControl
                case 0xA5:  // RightAlt
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Nome para relatorio/log; devolve null se o codigo nao tem nome conhecido.</summary>
        public static string NomeDe(CodigoTecla t)
        {
            foreach (KeyValuePair<string, uint> par in VirtualKeys)
            {
                if (TentarResolver(par.Key, out CodigoTecla cand) && cand.Equals(t)) return par.Key;
            }
            return null;
        }
    }

    /// <summary>
    /// Hook global de teclado de baixo nivel (WH_KEYBOARD_LL).
    /// Publico de proposito: a interface de configuracao (projeto Configurador) usa ESTE MESMO gancho
    /// para capturar a tecla que o usuario aperta. Assim a identidade mostrada na interface (scan code
    /// + flag estendida) e, por construcao, a mesma que o programa usa em tempo de jogo - nada de
    /// deduzir por MapVirtualKey (foi o defeito no. 5).
    /// Instalar() instala na thread que chama; BombearMensagens() tem de rodar NESSA mesma thread
    /// (o hook de baixo nivel entrega os eventos na fila da thread que o instalou).
    /// </summary>
    public sealed class GanchoTeclado : IDisposable
    {
        // manter viva a referencia do delegate e do handle do modulo: sem isso o GC coleta e o hook morre
        private Nativo.LowLevelKeyboardProc _declaracao;
        private IntPtr _hook = IntPtr.Zero;
        private readonly Action<CodigoTecla, bool> _aoEvento;
        private readonly Action<string> _aoDepuracao;

        public uint ThreadId { get; private set; }

        /// <summary>aoEvento(tecla, pressionada?). aoDepuracao (opcional) recebe o que o hook viu, cru.</summary>
        public GanchoTeclado(Action<CodigoTecla, bool> aoEvento, Action<string> aoDepuracao = null)
        {
            _aoEvento = aoEvento ?? throw new ArgumentNullException(nameof(aoEvento));
            _aoDepuracao = aoDepuracao;
        }

        public bool Instalar(out string erro)
        {
            erro = null;
            _declaracao = Retorno;
            IntPtr modulo = Nativo.GetModuleHandleW(null);
            _hook = Nativo.SetWindowsHookExW(Nativo.WH_KEYBOARD_LL, _declaracao, modulo, 0);
            if (_hook == IntPtr.Zero)
            {
                erro = "SetWindowsHookExW falhou (codigo " + Marshal.GetLastWin32Error() + ")";
                return false;
            }
            ThreadId = Nativo.GetCurrentThreadId();
            return true;
        }

        private IntPtr Retorno(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == Nativo.HC_ACTION)
            {
                Nativo.KBDLLHOOKSTRUCT dados = Marshal.PtrToStructure<Nativo.KBDLLHOOKSTRUCT>(lParam);
                bool soltou = (dados.flags & Nativo.LLKHF_UP) != 0;
                bool estendida = (dados.flags & Nativo.LLKHF_EXTENDED) != 0;
                try
                {
                    if (_aoDepuracao != null)
                    {
                        _aoDepuracao($"hook viu: vk=0x{dados.vkCode:X2} scan=0x{dados.scanCode:X2} " +
                                     $"flags=0x{dados.flags:X2} estendida={estendida} " +
                                     $"acao={(soltou ? "soltou" : "apertou")}");
                    }
                    _aoEvento(new CodigoTecla((int)dados.scanCode, estendida), !soltou);
                }
                catch
                {
                    // um erro no motor nunca pode travar a fila de entrada do Windows
                }
            }
            return Nativo.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        /// <summary>Bomba de mensagens: obrigatoria para o hook de baixo nivel funcionar.</summary>
        public void BombearMensagens()
        {
            while (Nativo.GetMessageW(out Nativo.MSG msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == Nativo.WM_QUIT) return;
            }
        }

        public void PedirParada()
        {
            if (ThreadId != 0) Nativo.PostThreadMessageW(ThreadId, Nativo.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                Nativo.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
