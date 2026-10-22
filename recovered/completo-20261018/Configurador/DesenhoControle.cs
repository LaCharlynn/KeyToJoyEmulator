using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Configurador;

public sealed class DesenhoControle : Control
{
	public sealed class Alvo
	{
		public string Nome;

		public int Sentido;

		public int Lado;

		public string Tecla;

		public bool Aceso;

		public float Valor;

		public float DeslocX;

		public float DeslocY;

		public Rectangle Caixa;

		public string Chave
		{
			get
			{
				if (Sentido != 0)
				{
					return Nome + ((Sentido > 0) ? "+" : "-");
				}
				return Nome;
			}
		}

		public string Rotulo
		{
			get
			{
				switch (Nome)
				{
				case "LX":
					if (Sentido <= 0)
					{
						return "L. analogico esq.";
					}
					return "L. analogico dir.";
				case "LY":
					if (Sentido <= 0)
					{
						return "L. analogico baixo";
					}
					return "L. analogico cima";
				case "RX":
					if (Sentido <= 0)
					{
						return "R. analogico esq.";
					}
					return "R. analogico dir.";
				case "RY":
					if (Sentido <= 0)
					{
						return "R. analogico baixo";
					}
					return "R. analogico cima";
				case "LT":
					return "LT";
				case "RT":
					return "RT";
				case "LB":
					return "LB";
				case "RB":
					return "RB";
				case "LS":
					return "LS (clique)";
				case "RS":
					return "RS (clique)";
				case "BACK":
					return "Voltar";
				case "START":
					return "Iniciar";
				case "UP":
					return "D-pad cima";
				case "DOWN":
					return "D-pad baixo";
				case "LEFT":
					return "D-pad esq.";
				case "RIGHT":
					return "D-pad dir.";
				default:
					return Nome;
				}
			}
		}

		public string NomePorExtenso
		{
			get
			{
				switch (Nome)
				{
				case "A":
					return "Botao A";
				case "B":
					return "Botao B";
				case "X":
					return "Botao X";
				case "Y":
					return "Botao Y";
				case "LB":
					return "Botao LB (ombro esquerdo)";
				case "RB":
					return "Botao RB (ombro direito)";
				case "BACK":
					return "Botao VOLTAR";
				case "START":
					return "Botao INICIAR";
				case "LS":
					return "Clique do analogico esquerdo (LS)";
				case "RS":
					return "Clique do analogico direito (RS)";
				case "UP":
					return "Direcional - cima";
				case "DOWN":
					return "Direcional - baixo";
				case "LEFT":
					return "Direcional - esquerda";
				case "RIGHT":
					return "Direcional - direita";
				case "LT":
					return "Gatilho esquerdo (LT)";
				case "RT":
					return "Gatilho direito (RT)";
				case "LX":
					if (Sentido <= 0)
					{
						return "Analogico esquerdo - esquerda";
					}
					return "Analogico esquerdo - direita";
				case "LY":
					if (Sentido <= 0)
					{
						return "Analogico esquerdo - baixo";
					}
					return "Analogico esquerdo - cima";
				case "RX":
					if (Sentido <= 0)
					{
						return "Analogico direito - esquerda";
					}
					return "Analogico direito - direita";
				case "RY":
					if (Sentido <= 0)
					{
						return "Analogico direito - baixo";
					}
					return "Analogico direito - cima";
				default:
					return Rotulo;
				}
			}
		}
	}

	private readonly List<Alvo> _alvos = AlvosPadrao();

	private string _selecionado = "A";

	private string _passando;

	private bool _temLeitura;

	private ushort _botoes;

	private short _lx;

	private short _ly;

	private short _rx;

	private short _ry;

	private byte _lt;

	private byte _rt;

	private static Image _imagem;

	private float _escala = 1f;

	private PointF _canto;

	private RectangleF _area;

	private Font _fonteLetra;

	private float _tamFonteLetra;

	private const int IMG_LARGURA = 680;

	private const int IMG_ALTURA = 678;

	private const double DES_X0 = 3.0;

	private const double DES_Y0 = 99.0;

	private const double DES_LARGURA = 674.0;

	private const double DES_ALTURA = 481.0;

	private const double LOGO_X = 340.0;

	private const double LOGO_Y = 166.5;

	private const double LOGO_R = 31.0;

	private const double BACK_X = 291.0;

	private const double BACK_Y = 233.5;

	private const double BACK_R = 22.5;

	private const double INI_X = 388.0;

	private const double INI_Y = 233.5;

	private const double INI_R = 22.5;

	private const double AE_X = 175.0;

	private const double AE_Y = 222.0;

	private const double ARO_R = 42.5;

	private const double TAMPA_R = 26.5;

	private const double AD_X = 433.0;

	private const double AD_Y = 337.5;

	private const double DP_X = 241.0;

	private const double DP_Y = 352.0;

	private const double DP_R = 58.5;

	private const double ABXY_X = 513.0;

	private const double ABXY_Y = 237.7;

	private const double ABXY_PASSO = 46.7;

	private const double ABXY_R = 26.5;

	private const double BORDA = 4.0;

	private static readonly double[,] OMBRO_ESQ = new double[10, 2]
	{
		{ 110.0, 152.0 },
		{ 118.0, 145.0 },
		{ 130.0, 131.0 },
		{ 142.0, 122.0 },
		{ 154.0, 116.0 },
		{ 166.0, 111.0 },
		{ 178.0, 107.0 },
		{ 190.0, 104.0 },
		{ 202.0, 101.0 },
		{ 214.0, 99.0 }
	};

	public List<Alvo> Alvos => _alvos;

	public string Selecionado
	{
		get
		{
			return _selecionado;
		}
		set
		{
			if (!string.Equals(_selecionado, value, StringComparison.Ordinal))
			{
				_selecionado = value;
				Invalidate();
			}
		}
	}

	public string Passando => _passando;

	public event EventHandler Selecionou;

	public DesenhoControle()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		BackColor = Tema.Painel;
		DoubleBuffered = true;
	}

	public static List<Alvo> AlvosPadrao()
	{
		return new List<Alvo>
		{
			new Alvo
			{
				Nome = "LT",
				Sentido = 1,
				Lado = 0
			},
			new Alvo
			{
				Nome = "LB",
				Sentido = 0,
				Lado = 0
			},
			new Alvo
			{
				Nome = "BACK",
				Sentido = 0,
				Lado = 0
			},
			new Alvo
			{
				Nome = "START",
				Sentido = 0,
				Lado = 0
			},
			new Alvo
			{
				Nome = "RB",
				Sentido = 0,
				Lado = 0
			},
			new Alvo
			{
				Nome = "RT",
				Sentido = 1,
				Lado = 0
			},
			new Alvo
			{
				Nome = "LX",
				Sentido = 1,
				Lado = 1
			},
			new Alvo
			{
				Nome = "LX",
				Sentido = -1,
				Lado = 1
			},
			new Alvo
			{
				Nome = "LY",
				Sentido = 1,
				Lado = 1
			},
			new Alvo
			{
				Nome = "LY",
				Sentido = -1,
				Lado = 1
			},
			new Alvo
			{
				Nome = "UP",
				Sentido = 0,
				Lado = 1
			},
			new Alvo
			{
				Nome = "DOWN",
				Sentido = 0,
				Lado = 1
			},
			new Alvo
			{
				Nome = "LEFT",
				Sentido = 0,
				Lado = 1
			},
			new Alvo
			{
				Nome = "RIGHT",
				Sentido = 0,
				Lado = 1
			},
			new Alvo
			{
				Nome = "A",
				Sentido = 0,
				Lado = 2
			},
			new Alvo
			{
				Nome = "B",
				Sentido = 0,
				Lado = 2
			},
			new Alvo
			{
				Nome = "X",
				Sentido = 0,
				Lado = 2
			},
			new Alvo
			{
				Nome = "Y",
				Sentido = 0,
				Lado = 2
			},
			new Alvo
			{
				Nome = "RX",
				Sentido = 1,
				Lado = 2
			},
			new Alvo
			{
				Nome = "RX",
				Sentido = -1,
				Lado = 2
			},
			new Alvo
			{
				Nome = "RY",
				Sentido = 1,
				Lado = 2
			},
			new Alvo
			{
				Nome = "RY",
				Sentido = -1,
				Lado = 2
			},
			new Alvo
			{
				Nome = "LS",
				Sentido = 0,
				Lado = 3
			},
			new Alvo
			{
				Nome = "RS",
				Sentido = 0,
				Lado = 3
			}
		};
	}

	public void DefinirTeclas(Dictionary<string, string> teclas)
	{
		foreach (Alvo alvo in _alvos)
		{
			alvo.Tecla = ((teclas != null && teclas.TryGetValue(alvo.Chave, out var value)) ? value : null);
		}
		Invalidate();
	}

	public void AoVivo(ushort botoes, short lx, short ly, short rx, short ry, byte lt, byte rt)
	{
		_temLeitura = true;
		_botoes = botoes;
		_lx = lx;
		_ly = ly;
		_rx = rx;
		_ry = ry;
		_lt = lt;
		_rt = rt;
		foreach (Alvo alvo in _alvos)
		{
			bool aceso = false;
			float valor = 0f;
			float deslocX = 0f;
			float deslocY = 0f;
			switch (alvo.Nome)
			{
			case "A":
				aceso = (botoes & 0x1000) != 0;
				break;
			case "B":
				aceso = (botoes & 0x2000) != 0;
				break;
			case "X":
				aceso = (botoes & 0x4000) != 0;
				break;
			case "Y":
				aceso = (botoes & 0x8000) != 0;
				break;
			case "LB":
				aceso = (botoes & 0x100) != 0;
				break;
			case "RB":
				aceso = (botoes & 0x200) != 0;
				break;
			case "BACK":
				aceso = (botoes & 0x20) != 0;
				break;
			case "START":
				aceso = (botoes & 0x10) != 0;
				break;
			case "LS":
				aceso = (botoes & 0x40) != 0;
				break;
			case "RS":
				aceso = (botoes & 0x80) != 0;
				break;
			case "UP":
				aceso = (botoes & 1) != 0;
				break;
			case "DOWN":
				aceso = (botoes & 2) != 0;
				break;
			case "LEFT":
				aceso = (botoes & 4) != 0;
				break;
			case "RIGHT":
				aceso = (botoes & 8) != 0;
				break;
			case "LT":
				valor = (float)(int)lt / 255f;
				aceso = lt > 0;
				break;
			case "RT":
				valor = (float)(int)rt / 255f;
				aceso = rt > 0;
				break;
			case "LX":
				deslocX = (float)lx / 32767f;
				aceso = ((alvo.Sentido <= 0) ? (lx < -4000) : (lx > 4000));
				break;
			case "LY":
				deslocY = (float)ly / 32767f;
				aceso = ((alvo.Sentido <= 0) ? (ly < -4000) : (ly > 4000));
				break;
			case "RX":
				deslocX = (float)rx / 32767f;
				aceso = ((alvo.Sentido <= 0) ? (rx < -4000) : (rx > 4000));
				break;
			case "RY":
				deslocY = (float)ry / 32767f;
				aceso = ((alvo.Sentido <= 0) ? (ry < -4000) : (ry > 4000));
				break;
			}
			if (alvo.Nome == "LX")
			{
				alvo.DeslocX = deslocX;
				alvo.DeslocY = 0f;
			}
			else if (alvo.Nome == "LY")
			{
				alvo.DeslocX = 0f;
				alvo.DeslocY = deslocY;
			}
			else if (alvo.Nome == "RX")
			{
				alvo.DeslocX = deslocX;
				alvo.DeslocY = 0f;
			}
			else if (alvo.Nome == "RY")
			{
				alvo.DeslocX = 0f;
				alvo.DeslocY = deslocY;
			}
			alvo.Aceso = aceso;
			if (alvo.Nome == "LT" || alvo.Nome == "RT")
			{
				alvo.Valor = valor;
			}
			if (alvo.Nome == "LS")
			{
				alvo.DeslocX = (float)lx / 32767f;
				alvo.DeslocY = (float)(-ly) / 32767f;
			}
			if (alvo.Nome == "RS")
			{
				alvo.DeslocX = (float)rx / 32767f;
				alvo.DeslocY = (float)(-ry) / 32767f;
			}
		}
		Invalidate();
	}

	public void SemLeitura()
	{
		_temLeitura = false;
		_lx = (_ly = (_rx = (_ry = 0)));
		_lt = (_rt = 0);
		_botoes = 0;
		foreach (Alvo alvo in _alvos)
		{
			alvo.Aceso = false;
			alvo.Valor = 0f;
			alvo.DeslocX = 0f;
			alvo.DeslocY = 0f;
		}
		Invalidate();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && _fonteLetra != null)
		{
			_fonteLetra.Dispose();
			_fonteLetra = null;
		}
		base.Dispose(disposing);
	}

	private static Image Imagem()
	{
		if (_imagem != null)
		{
			return _imagem;
		}
		try
		{
			Assembly assembly = typeof(DesenhoControle).Assembly;
			string text = null;
			string[] manifestResourceNames = assembly.GetManifestResourceNames();
			foreach (string text2 in manifestResourceNames)
			{
				if (text2.EndsWith("controle-xbox.png", StringComparison.OrdinalIgnoreCase))
				{
					text = text2;
					break;
				}
			}
			if (text == null)
			{
				return null;
			}
			using Stream stream = assembly.GetManifestResourceStream(text);
			if (stream == null)
			{
				return null;
			}
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			memoryStream.Position = 0L;
			_imagem = Image.FromStream(memoryStream);
		}
		catch (Exception)
		{
			return null;
		}
		return _imagem;
	}

	private PointF P(double x, double y)
	{
		return new PointF((float)((double)_canto.X + x * (double)_escala), (float)((double)_canto.Y + y * (double)_escala));
	}

	private float PX(double x)
	{
		return (float)((double)_canto.X + x * (double)_escala);
	}

	private float PY(double y)
	{
		return (float)((double)_canto.Y + y * (double)_escala);
	}

	private float RD(double v)
	{
		return (float)(v * (double)_escala);
	}

	private float LN(float basePixels)
	{
		float num = basePixels * _escala;
		if (num < 1.2f)
		{
			num = 1.2f;
		}
		if (num > 6f)
		{
			num = 6f;
		}
		return num;
	}

	private PointF Ancora(Alvo a)
	{
		switch (a.Nome)
		{
		case "LT":
			return P(180.0, 88.0);
		case "RT":
			return P(500.0, 88.0);
		case "LB":
			return P(176.0, 132.0);
		case "RB":
			return P(504.0, 132.0);
		case "BACK":
			return P(291.0, 207.0);
		case "START":
			return P(388.0, 207.0);
		case "Y":
			return P(513.0, 160.5);
		case "B":
			return P(590.2, 237.7);
		case "X":
			return P(466.3, 268.2);
		case "A":
			return P(513.0, 314.9);
		case "UP":
			return P(241.0, 289.5);
		case "DOWN":
			return P(241.0, 414.5);
		case "LEFT":
			return P(178.5, 352.0);
		case "RIGHT":
			return P(303.5, 352.0);
		case "LX":
			if (a.Sentido <= 0)
			{
				return P(128.5, 222.0);
			}
			return P(221.5, 222.0);
		case "LY":
			if (a.Sentido <= 0)
			{
				return P(175.0, 268.5);
			}
			return P(175.0, 175.5);
		case "RX":
			if (a.Sentido <= 0)
			{
				return P(386.5, 337.5);
			}
			return P(479.5, 337.5);
		case "RY":
			if (a.Sentido <= 0)
			{
				return P(433.0, 384.0);
			}
			return P(433.0, 291.0);
		case "LS":
			return P(197.0, 244.0);
		case "RS":
			return P(455.0, 359.5);
		default:
			return P(513.0, 237.7);
		}
	}

	private void Recalcular()
	{
		int width = base.ClientSize.Width;
		int height = base.ClientSize.Height;
		if (width < 60 || height < 60)
		{
			return;
		}
		int num = 6;
		int num2 = 30;
		int num3 = Math.Max(112, Math.Min(136, (width - 240) / 2));
		float num4 = num + num3 + 8;
		float num5 = (float)width - 2f * num4;
		float num6 = num + num2 + 12;
		float num7 = (float)height - num6 - (float)num2 - (float)num - 10f;
		if (num5 < 80f)
		{
			num5 = 80f;
		}
		if (num7 < 80f)
		{
			num7 = 80f;
		}
		float num8 = Math.Min((float)((double)num5 / 674.0), (float)((double)num7 / 481.0));
		if (num8 < 0.05f)
		{
			num8 = 0.05f;
		}
		_escala = num8;
		float num9 = num4 + (num5 - 674f * num8) / 2f;
		float num10 = num6 + (num7 - 481f * num8) / 2f;
		_canto = new PointF((float)((double)num9 - 3.0 * (double)num8), (float)((double)num10 - 99.0 * (double)num8));
		_area = new RectangleF(PX(0.0), PY(0.0), RD(680.0), RD(678.0));
		double[] array = new double[6] { 0.145, 0.135, 0.22, 0.22, 0.135, 0.145 };
		int num11 = width - 2 * num - 5 * num;
		int num12 = num;
		for (int i = 0; i < 6; i++)
		{
			int num13 = (int)Math.Round((double)num11 * array[i]);
			if (num13 < 56)
			{
				num13 = 56;
			}
			_alvos[i].Caixa = new Rectangle(num12, num, num13, num2);
			num12 += num13 + num;
		}
		int num14 = Math.Max(110, num3);
		int num15 = width / 2 - (num14 * 2 + num) / 2;
		for (int i = 22; i < 24; i++)
		{
			_alvos[i].Caixa = new Rectangle(num15, height - num - num2, num14, num2);
			num15 += num14 + num;
		}
		List<Alvo> list = new List<Alvo>();
		List<Alvo> list2 = new List<Alvo>();
		foreach (Alvo alvo in _alvos)
		{
			if (alvo.Lado == 1)
			{
				list.Add(alvo);
			}
			else if (alvo.Lado == 2)
			{
				list2.Add(alvo);
			}
		}
		list.Sort(PorAlturaNoDesenho);
		list2.Sort(PorAlturaNoDesenho);
		int num16 = num2 + 10;
		int num17 = 8 * num16 - 10;
		int num18 = num + num2 + 14;
		int num19 = height - num18 - (num + num2 + 10) - num17;
		if (num19 > 0)
		{
			num18 += num19 / 2;
		}
		int num20 = num18;
		foreach (Alvo item in list)
		{
			item.Caixa = new Rectangle(num, num20, num3, num2);
			num20 += num16;
		}
		num20 = num18;
		foreach (Alvo item2 in list2)
		{
			item2.Caixa = new Rectangle(width - num - num3, num20, num3, num2);
			num20 += num16;
		}
	}

	private int PorAlturaNoDesenho(Alvo a, Alvo b)
	{
		int num = AlturaParaOrdenar(a).CompareTo(AlturaParaOrdenar(b));
		if (num != 0)
		{
			return num;
		}
		return _alvos.IndexOf(a).CompareTo(_alvos.IndexOf(b));
	}

	private float AlturaParaOrdenar(Alvo a)
	{
		if (string.Equals(a.Nome, "A", StringComparison.Ordinal))
		{
			Alvo alvo = Achar("RY", 1);
			if (alvo != null)
			{
				return Ancora(alvo).Y;
			}
		}
		else if (string.Equals(a.Nome, "RY", StringComparison.Ordinal) && a.Sentido == 1)
		{
			Alvo alvo2 = Achar("A");
			if (alvo2 != null)
			{
				return Ancora(alvo2).Y;
			}
		}
		return Ancora(a).Y;
	}

	protected override void OnResize(EventArgs e)
	{
		Recalcular();
		base.OnResize(e);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		Recalcular();
		base.OnHandleCreated(e);
	}

	private Alvo AlvoNoPonto(Point p)
	{
		foreach (Alvo alvo in _alvos)
		{
			if (alvo.Caixa.Contains(p))
			{
				return alvo;
			}
		}
		if (_area.IsEmpty || _escala <= 0f)
		{
			return null;
		}
		if (Cerca(p, 513.0, 284.4, 32.5))
		{
			return Achar("A");
		}
		if (Cerca(p, 559.7, 237.7, 32.5))
		{
			return Achar("B");
		}
		if (Cerca(p, 466.3, 237.7, 32.5))
		{
			return Achar("X");
		}
		if (Cerca(p, 513.0, 191.0, 32.5))
		{
			return Achar("Y");
		}
		using (GraphicsPath graphicsPath = AbaGatilho(esquerdo: true))
		{
			if (graphicsPath.IsVisible(p))
			{
				return Achar("LT");
			}
		}
		using (GraphicsPath graphicsPath2 = AbaGatilho(esquerdo: false))
		{
			if (graphicsPath2.IsVisible(p))
			{
				return Achar("RT");
			}
		}
		if (FaixaOmbro(esquerdo: true).Contains(p))
		{
			return Achar("LB");
		}
		if (FaixaOmbro(esquerdo: false).Contains(p))
		{
			return Achar("RB");
		}
		if (Cerca(p, 291.0, 233.5, 25.5))
		{
			return Achar("BACK");
		}
		if (Cerca(p, 388.0, 233.5, 25.5))
		{
			return Achar("START");
		}
		if (Cerca(p, 241.0, 352.0, 58.5))
		{
			float num = (float)p.X - PX(241.0);
			float num2 = (float)p.Y - PY(352.0);
			if (Math.Abs(num) > Math.Abs(num2))
			{
				return Achar((num > 0f) ? "RIGHT" : "LEFT");
			}
			return Achar((num2 > 0f) ? "DOWN" : "UP");
		}
		if (Cerca(p, 175.0, 222.0, 42.5))
		{
			if (Cerca(p, 175.0, 222.0, 22.525))
			{
				return Achar("LS");
			}
			float num3 = (float)p.X - PX(175.0);
			float num4 = (float)p.Y - PY(222.0);
			if (Math.Abs(num3) > Math.Abs(num4))
			{
				return Achar("LX", (num3 > 0f) ? 1 : (-1));
			}
			return Achar("LY", (!(num4 > 0f)) ? 1 : (-1));
		}
		if (Cerca(p, 433.0, 337.5, 42.5))
		{
			if (Cerca(p, 433.0, 337.5, 22.525))
			{
				return Achar("RS");
			}
			float num5 = (float)p.X - PX(433.0);
			float num6 = (float)p.Y - PY(337.5);
			if (Math.Abs(num5) > Math.Abs(num6))
			{
				return Achar("RX", (num5 > 0f) ? 1 : (-1));
			}
			return Achar("RY", (num6 > 0f) ? 1 : (-1));
		}
		return null;
	}

	private bool Cerca(Point p, double nx, double ny, double raio)
	{
		float num = (float)p.X - PX(nx);
		float num2 = (float)p.Y - PY(ny);
		float num3 = RD(raio);
		return num * num + num2 * num2 <= num3 * num3;
	}

	private Alvo Achar(string nome)
	{
		return Achar(nome, 0);
	}

	private Alvo Achar(string nome, int sentido)
	{
		foreach (Alvo alvo in _alvos)
		{
			if (string.Equals(alvo.Nome, nome, StringComparison.Ordinal) && alvo.Sentido == sentido)
			{
				return alvo;
			}
		}
		return null;
	}

	private bool Aceso(string nome)
	{
		return Achar(nome)?.Aceso ?? false;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		string text = AlvoNoPonto(e.Location)?.Chave;
		if (text != _passando)
		{
			_passando = text;
			Cursor = ((text == null) ? Cursors.Default : Cursors.Hand);
			Invalidate();
		}
		base.OnMouseMove(e);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		if (_passando != null)
		{
			_passando = null;
			Cursor = Cursors.Default;
			Invalidate();
		}
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		Alvo alvo = AlvoNoPonto(e.Location);
		if (alvo != null && e.Button == MouseButtons.Left)
		{
			Selecionado = alvo.Chave;
			if (this.Selecionou != null)
			{
				this.Selecionou(this, EventArgs.Empty);
			}
		}
		base.OnMouseDown(e);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		if (_area.IsEmpty)
		{
			Recalcular();
		}
		using (SolidBrush brush = new SolidBrush(Tema.Painel))
		{
			graphics.FillRectangle(brush, base.ClientRectangle);
		}
		if (!_area.IsEmpty)
		{
			DesenharControle(graphics);
		}
		if (!_area.IsEmpty)
		{
			foreach (Alvo alvo in _alvos)
			{
				DesenharLinha(graphics, alvo);
			}
		}
		if (!_temLeitura)
		{
			float num = _area.Bottom + 6f;
			if (num + 16f <= (float)(base.ClientSize.Height - 42))
			{
				using SolidBrush brush2 = new SolidBrush(Tema.Texto3);
				using StringFormat format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					Trimming = StringTrimming.EllipsisCharacter,
					FormatFlags = StringFormatFlags.NoWrap
				};
				graphics.DrawString("o pad esta desligado: ligue no botao 'Ligar o pad'", Tema.Fonte8, brush2, new RectangleF(_area.Left, num, _area.Width, 16f), format);
			}
		}
		foreach (Alvo alvo2 in _alvos)
		{
			DesenharCaixa(graphics, alvo2);
		}
	}

	private void DesenharLinha(Graphics g, Alvo a)
	{
		if (a.Caixa.Width == 0)
		{
			return;
		}
		PointF pt = Ancora(a);
		PointF pt2 = a.Lado switch
		{
			0 => new PointF((float)a.Caixa.Left + (float)a.Caixa.Width / 2f, a.Caixa.Bottom), 
			1 => new PointF(a.Caixa.Right, (float)a.Caixa.Top + (float)a.Caixa.Height / 2f), 
			2 => new PointF(a.Caixa.Left, (float)a.Caixa.Top + (float)a.Caixa.Height / 2f), 
			_ => new PointF((float)a.Caixa.Left + (float)a.Caixa.Width / 2f, a.Caixa.Top), 
		};
		bool flag = string.Equals(a.Chave, _selecionado, StringComparison.Ordinal);
		Color color = (flag ? Tema.Realce : ((a.Tecla != null) ? Color.FromArgb(200, Tema.Realce) : Color.FromArgb(130, Tema.Texto2)));
		float x = (pt2.X + pt.X) / 2f;
		using (Pen pen = new Pen(color, flag ? 1.8f : 1.2f))
		{
			if (a.Lado == 0 || a.Lado == 3)
			{
				g.DrawBezier(pen, pt2, new PointF(pt2.X, pt2.Y + (pt.Y - pt2.Y) / 2f), new PointF(pt.X, pt2.Y + (pt.Y - pt2.Y) / 2f), pt);
			}
			else
			{
				g.DrawBezier(pen, pt2, new PointF(x, pt2.Y), new PointF(x, pt.Y), pt);
			}
		}
		using SolidBrush brush = new SolidBrush(color);
		g.FillEllipse(brush, pt.X - 2.5f, pt.Y - 2.5f, 5f, 5f);
	}

	private void DesenharCaixa(Graphics g, Alvo a)
	{
		if (a.Caixa.Width == 0)
		{
			return;
		}
		bool flag = string.Equals(a.Chave, _selecionado, StringComparison.Ordinal);
		bool flag2 = string.Equals(a.Chave, _passando, StringComparison.Ordinal);
		using (SolidBrush brush = new SolidBrush(flag ? Color.FromArgb(52, 62, 60) : (flag2 ? Tema.Painel2 : Tema.Painel2)))
		{
			using GraphicsPath path = Arredondado(a.Caixa, 6f);
			g.FillPath(brush, path);
		}
		using (Pen pen = new Pen(flag ? Tema.Realce : (a.Aceso ? Tema.Aceso : Tema.Borda), flag ? 1.8f : 1f))
		{
			using GraphicsPath path2 = Arredondado(a.Caixa, 6f);
			g.DrawPath(pen, path2);
		}
		Rectangle rectangle = new Rectangle(a.Caixa.Left + 8, a.Caixa.Top, a.Caixa.Width - 16, a.Caixa.Height);
		string text = a.Tecla ?? "vazio";
		Font font = ((a.Tecla != null) ? Tema.Fonte8N : Tema.Fonte75);
		float num = Math.Min((float)rectangle.Width * 0.55f, g.MeasureString(text, font).Width + 2f);
		float num2 = (float)rectangle.Width - num - 6f;
		float width = g.MeasureString(a.Rotulo, Tema.Fonte75).Width;
		if (num2 < width && width <= (float)rectangle.Width * 0.7f)
		{
			num2 = width;
			num = (float)rectangle.Width - num2 - 6f;
		}
		if (num2 < (float)rectangle.Width * 0.3f)
		{
			num2 = (float)rectangle.Width * 0.3f;
			num = (float)rectangle.Width - num2 - 6f;
		}
		using (SolidBrush brush2 = new SolidBrush((a.Tecla != null) ? Tema.Texto2 : Tema.Texto3))
		{
			using StringFormat format = new StringFormat
			{
				LineAlignment = StringAlignment.Center,
				Trimming = StringTrimming.EllipsisCharacter,
				FormatFlags = StringFormatFlags.NoWrap
			};
			g.DrawString(a.Rotulo, Tema.Fonte75, brush2, new RectangleF(rectangle.Left, rectangle.Top, num2, rectangle.Height), format);
		}
		using SolidBrush brush3 = new SolidBrush((a.Tecla == null) ? Tema.Texto3 : (a.Aceso ? Tema.Aceso : Tema.Texto));
		using StringFormat format2 = new StringFormat
		{
			Alignment = StringAlignment.Far,
			LineAlignment = StringAlignment.Center,
			Trimming = StringTrimming.EllipsisCharacter,
			FormatFlags = StringFormatFlags.NoWrap
		};
		g.DrawString(text, font, brush3, new RectangleF((float)rectangle.Left + num2 + 6f, rectangle.Top, num, rectangle.Height), format2);
	}

	private void DesenharControle(Graphics g)
	{
		DesenharAbaGatilho(g, esquerdo: true, Achar("LT"));
		DesenharAbaGatilho(g, esquerdo: false, Achar("RT"));
		DesenharImagem(g);
		DesenharBotao(g, Achar("A"), 513.0, 284.4, "A", Color.FromArgb(46, 158, 79));
		DesenharBotao(g, Achar("B"), 559.7, 237.7, "B", Color.FromArgb(192, 57, 43));
		DesenharBotao(g, Achar("X"), 466.3, 237.7, "X", Color.FromArgb(44, 111, 181));
		DesenharBotao(g, Achar("Y"), 513.0, 191.0, "Y", Color.FromArgb(201, 162, 39));
		DesenharBolinha(g, Achar("BACK"), 291.0, 233.5, 22.5);
		DesenharBolinha(g, Achar("START"), 388.0, 233.5, 22.5);
		DesenharDirecional(g);
		DesenharAnalogico(g, esquerdo: true, Achar("LX", 1), Achar("LX", -1), Achar("LY", 1), Achar("LY", -1), Achar("LS"));
		DesenharAnalogico(g, esquerdo: false, Achar("RX", 1), Achar("RX", -1), Achar("RY", 1), Achar("RY", -1), Achar("RS"));
		Alvo alvo = null;
		foreach (Alvo alvo2 in _alvos)
		{
			if (string.Equals(alvo2.Chave, _selecionado, StringComparison.Ordinal))
			{
				alvo = alvo2;
			}
		}
		if (alvo != null)
		{
			PointF pointF = Ancora(alvo);
			float num = RD(11.0);
			using Pen pen = new Pen(Tema.Realce, 2f);
			g.DrawEllipse(pen, pointF.X - num, pointF.Y - num, 2f * num, 2f * num);
		}
	}

	private void DesenharImagem(Graphics g)
	{
		Image image = Imagem();
		if (image != null)
		{
			InterpolationMode interpolationMode = g.InterpolationMode;
			PixelOffsetMode pixelOffsetMode = g.PixelOffsetMode;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			Rectangle destRect = new Rectangle((int)Math.Round(PX(0.0)), (int)Math.Round(PY(0.0)), (int)Math.Round(RD(680.0)), (int)Math.Round(RD(678.0)));
			using (ImageAttributes imageAttributes = new ImageAttributes())
			{
				imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
				g.DrawImage(image, destRect, 0, 0, 680, 678, GraphicsUnit.Pixel, imageAttributes);
			}
			g.InterpolationMode = interpolationMode;
			g.PixelOffsetMode = pixelOffsetMode;
		}
	}

	private GraphicsPath AbaGatilho(bool esquerdo)
	{
		int length = OMBRO_ESQ.GetLength(0);
		PointF[] array = new PointF[2 * length];
		for (int i = 0; i < length; i++)
		{
			double num = OMBRO_ESQ[i, 0];
			double num2 = OMBRO_ESQ[i, 1];
			if (!esquerdo)
			{
				num = 680.0 - num;
			}
			array[i] = P(num, num2 - 30.0);
			array[2 * length - 1 - i] = P(num, num2 - -16.0);
		}
		GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddPolygon(array);
		return graphicsPath;
	}

	private RectangleF FaixaOmbro(bool esquerdo)
	{
		if (!esquerdo)
		{
			return RectangleF.FromLTRB(PX(452.0), PY(100.0), PX(568.0), PY(148.0));
		}
		return RectangleF.FromLTRB(PX(112.0), PY(100.0), PX(228.0), PY(148.0));
	}

	private void DesenharAbaGatilho(Graphics g, bool esquerdo, Alvo a)
	{
		using GraphicsPath graphicsPath = AbaGatilho(esquerdo);
		using (SolidBrush brush = new SolidBrush(Color.White))
		{
			g.FillPath(brush, graphicsPath);
		}
		if (a != null && a.Valor > 0.01f)
		{
			RectangleF bounds = graphicsPath.GetBounds();
			Region clip = g.Clip;
			g.SetClip(graphicsPath);
			float num = bounds.Height * a.Valor;
			using (SolidBrush brush2 = new SolidBrush(Tema.Aceso))
			{
				g.FillRectangle(brush2, bounds.Left, bounds.Bottom - num, bounds.Width, num);
			}
			g.Clip = clip;
		}
		using Pen pen = new Pen(Color.Black, LN(2.4f));
		g.DrawPath(pen, graphicsPath);
	}

	private void DesenharBolinha(Graphics g, Alvo a, double x, double y, double r)
	{
		if (a == null || !a.Aceso)
		{
			return;
		}
		float num = PX(x);
		float num2 = PY(y);
		float num3 = RD(r) + 3f;
		using Pen pen = new Pen(Tema.Aceso, LN(2.5f));
		g.DrawEllipse(pen, num - num3, num2 - num3, 2f * num3, 2f * num3);
	}

	private void DesenharBotao(Graphics g, Alvo a, double x, double y, string letra, Color cor)
	{
		float num = PX(x);
		float num2 = PY(y);
		float num3 = RD(25.0);
		using (SolidBrush brush = new SolidBrush(cor))
		{
			g.FillEllipse(brush, num - num3, num2 - num3, 2f * num3, 2f * num3);
		}
		if (a != null && a.Aceso)
		{
			using Pen pen = new Pen(Tema.Aceso, LN(2.4f));
			g.DrawEllipse(pen, num - num3 - 3.5f, num2 - num3 - 3.5f, 2f * (num3 + 3.5f), 2f * (num3 + 3.5f));
		}
		using SolidBrush brush2 = new SolidBrush(Color.White);
		using StringFormat format = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		g.DrawString(letra, FonteLetra(2f * num3 * 0.78f), brush2, new RectangleF(num - num3, num2 - num3, 2f * num3, 2f * num3), format);
	}

	private void DesenharDirecional(Graphics g)
	{
		float cx = PX(241.0);
		float cy = PY(352.0);
		float r = RD(54.5);
		if (Aceso("UP"))
		{
			Fatia(g, cx, cy, r, -135f, -45f);
		}
		if (Aceso("RIGHT"))
		{
			Fatia(g, cx, cy, r, -45f, 45f);
		}
		if (Aceso("DOWN"))
		{
			Fatia(g, cx, cy, r, 45f, 135f);
		}
		if (Aceso("LEFT"))
		{
			Fatia(g, cx, cy, r, 135f, 225f);
		}
	}

	private void Fatia(Graphics g, float cx, float cy, float r, float de, float ate)
	{
		using SolidBrush brush = new SolidBrush(Color.FromArgb(200, Tema.Aceso));
		g.FillPie(brush, cx - r, cy - r, 2f * r, 2f * r, de, ate - de);
	}

	private void DesenharAnalogico(Graphics g, bool esquerdo, Alvo eixoX, Alvo eixoXmenos, Alvo eixoY, Alvo eixoYmenos, Alvo clique)
	{
		double x = (esquerdo ? 175.0 : 433.0);
		double y = (esquerdo ? 222.0 : 337.5);
		float num = PX(x);
		float num2 = PY(y);
		float num3 = RD(42.5);
		float num4 = RD(26.5);
		float num5 = 0f;
		float num6 = 0f;
		if (_temLeitura)
		{
			num5 = (float)(esquerdo ? _lx : _rx) / 32767f;
			num6 = (float)(-(esquerdo ? _ly : _ry)) / 32767f;
		}
		if (num5 > 1f)
		{
			num5 = 1f;
		}
		if (num5 < -1f)
		{
			num5 = -1f;
		}
		if (num6 > 1f)
		{
			num6 = 1f;
		}
		if (num6 < -1f)
		{
			num6 = -1f;
		}
		bool flag = (clique != null && clique.Aceso) || (eixoX != null && eixoX.Aceso) || (eixoXmenos != null && eixoXmenos.Aceso) || (eixoY != null && eixoY.Aceso) || (eixoYmenos?.Aceso ?? false);
		if (num5 * num5 + num6 * num6 > 0.02f)
		{
			float num7 = num + num5 * (num3 - num4) * 0.85f;
			float num8 = num2 + num6 * (num3 - num4) * 0.85f;
			using SolidBrush brush = new SolidBrush(Color.FromArgb(120, Tema.Aceso));
			g.FillEllipse(brush, num7 - num4, num8 - num4, 2f * num4, 2f * num4);
		}
		if (clique != null && clique.Aceso)
		{
			using SolidBrush brush2 = new SolidBrush(Color.FromArgb(150, Tema.Aceso));
			g.FillEllipse(brush2, num - num4, num2 - num4, 2f * num4, 2f * num4);
		}
		if (flag)
		{
			using (Pen pen = new Pen(Tema.Aceso, LN(2.4f)))
			{
				g.DrawEllipse(pen, num - num3 - 1f, num2 - num3 - 1f, 2f * (num3 + 1f), 2f * (num3 + 1f));
			}
		}
	}

	private Font FonteLetra(float pixels)
	{
		if (pixels < 5f)
		{
			pixels = 5f;
		}
		if (_fonteLetra == null || Math.Abs(_tamFonteLetra - pixels) > 0.25f)
		{
			if (_fonteLetra != null)
			{
				_fonteLetra.Dispose();
			}
			_fonteLetra = new Font("Segoe UI", pixels, FontStyle.Bold, GraphicsUnit.Pixel);
			_tamFonteLetra = pixels;
		}
		return _fonteLetra;
	}

	private static GraphicsPath Arredondado(RectangleF r, float raio)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		if (r.Width <= 0f || r.Height <= 0f)
		{
			return graphicsPath;
		}
		float num = raio * 2f;
		if (num > r.Width)
		{
			num = r.Width;
		}
		if (num > r.Height)
		{
			num = r.Height;
		}
		graphicsPath.AddArc(r.Left, r.Top, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Top, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.Left, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}
}
