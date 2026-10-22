using System;
using System.Runtime.InteropServices;

namespace Configurador;

public static class LeitorControle
{
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

	private struct XINPUT_STATE
	{
		public uint dwPacketNumber;

		public XINPUT_GAMEPAD Gamepad;
	}

	public struct Estado
	{
		public int Indice;

		public uint Pacote;

		public ushort Botoes;

		public byte LT;

		public byte RT;

		public short LX;

		public short LY;

		public short RX;

		public short RY;
	}

	private const uint ERRO_SUCESSO = 0u;

	private const uint ERRO_NAO_CONECTADO = 1167u;

	private static bool _usarBibliotecaAntiga;

	public const ushort BT_UP = 1;

	public const ushort BT_DOWN = 2;

	public const ushort BT_LEFT = 4;

	public const ushort BT_RIGHT = 8;

	public const ushort BT_START = 16;

	public const ushort BT_BACK = 32;

	public const ushort BT_LS = 64;

	public const ushort BT_RS = 128;

	public const ushort BT_LB = 256;

	public const ushort BT_RB = 512;

	public const ushort BT_GUIDE = 1024;

	public const ushort BT_A = 4096;

	public const ushort BT_B = 8192;

	public const ushort BT_X = 16384;

	public const ushort BT_Y = 32768;

	[DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
	private static extern uint XInputGetState14(uint dwUserIndex, ref XINPUT_STATE pState);

	[DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
	private static extern uint XInputGetState91(uint dwUserIndex, ref XINPUT_STATE pState);

	public static bool Ler(out Estado estado)
	{
		estado = default(Estado);
		for (uint num = 0u; num < 4; num++)
		{
			XINPUT_STATE estado2 = default(XINPUT_STATE);
			switch (Chamar(num, ref estado2))
			{
			case 0u:
				estado.Indice = (int)num;
				estado.Pacote = estado2.dwPacketNumber;
				estado.Botoes = estado2.Gamepad.wButtons;
				estado.LT = estado2.Gamepad.bLeftTrigger;
				estado.RT = estado2.Gamepad.bRightTrigger;
				estado.LX = estado2.Gamepad.sThumbLX;
				estado.LY = estado2.Gamepad.sThumbLY;
				estado.RX = estado2.Gamepad.sThumbRX;
				estado.RY = estado2.Gamepad.sThumbRY;
				return true;
			default:
				return false;
			case 1167u:
				break;
			}
		}
		return false;
	}

	private static uint Chamar(uint indice, ref XINPUT_STATE estado)
	{
		if (!_usarBibliotecaAntiga)
		{
			try
			{
				return XInputGetState14(indice, ref estado);
			}
			catch (DllNotFoundException)
			{
				_usarBibliotecaAntiga = true;
			}
			catch (EntryPointNotFoundException)
			{
				_usarBibliotecaAntiga = true;
			}
		}
		return XInputGetState91(indice, ref estado);
	}

	public static int BarraDeEixo(short valor)
	{
		int num = 100 + (int)((long)valor * 100L / 32767);
		if (num < 0)
		{
			num = 0;
		}
		if (num > 200)
		{
			num = 200;
		}
		return num;
	}
}
