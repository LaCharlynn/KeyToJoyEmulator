using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Configurador;

public sealed class BarraDeslizante : Control
{
	private int _minimo;

	private int _maximo = 100;

	private int _valor;

	private bool _arrastando;

	public int Minimo
	{
		get
		{
			return _minimo;
		}
		set
		{
			_minimo = value;
			if (_valor < _minimo)
			{
				_valor = _minimo;
			}
			Invalidate();
		}
	}

	public int Maximo
	{
		get
		{
			return _maximo;
		}
		set
		{
			_maximo = ((value < _minimo + 1) ? (_minimo + 1) : value);
			if (_valor > _maximo)
			{
				_valor = _maximo;
			}
			Invalidate();
		}
	}

	public int Valor
	{
		get
		{
			return _valor;
		}
		set
		{
			int num = value;
			if (num < _minimo)
			{
				num = _minimo;
			}
			if (num > _maximo)
			{
				num = _maximo;
			}
			if (num != _valor)
			{
				_valor = num;
				Invalidate();
			}
		}
	}

	public string Etiqueta { get; set; }

	public event EventHandler ValorMudou;

	public BarraDeslizante()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		base.Height = 26;
		BackColor = Tema.Painel;
	}

	private int XDoValor(int valor)
	{
		int num = base.ClientSize.Width - 16;
		if (_maximo <= _minimo)
		{
			return 8;
		}
		return 8 + (int)Math.Round((double)(valor - _minimo) * (double)num / (double)(_maximo - _minimo));
	}

	private int ValorDoX(int x)
	{
		int num = base.ClientSize.Width - 16;
		if (num <= 0)
		{
			return _minimo;
		}
		double num2 = (double)(x - 8) / (double)num;
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > 1.0)
		{
			num2 = 1.0;
		}
		return _minimo + (int)Math.Round(num2 * (double)(_maximo - _minimo));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int num = base.ClientSize.Height / 2;
		int num2 = XDoValor(_valor);
		bool enabled = base.Enabled;
		using (SolidBrush brush = new SolidBrush(Tema.Painel2))
		{
			using GraphicsPath path = Trilho(8, num - 3, base.ClientSize.Width - 16, 6);
			graphics.FillPath(brush, path);
		}
		if (num2 > 8)
		{
			using SolidBrush brush2 = new SolidBrush(enabled ? Tema.Realce : Tema.Borda);
			using GraphicsPath path2 = Trilho(8, num - 3, num2 - 8, 6);
			graphics.FillPath(brush2, path2);
		}
		using (SolidBrush brush3 = new SolidBrush(enabled ? Tema.Texto : Tema.Texto3))
		{
			graphics.FillEllipse(brush3, num2 - 7, num - 7, 14, 14);
		}
		if (!string.IsNullOrEmpty(Etiqueta))
		{
			using (SolidBrush brush4 = new SolidBrush(Tema.Texto2))
			{
				graphics.DrawString(Etiqueta, Tema.Fonte75, brush4, new PointF(10f, num - 22));
			}
		}
	}

	private static GraphicsPath Trilho(int x, int y, int largura, int altura)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		if (largura < 2 * altura)
		{
			largura = 2 * altura;
		}
		graphicsPath.AddArc(x, y, altura, altura, 90f, 180f);
		graphicsPath.AddArc(x + largura - altura, y, altura, altura, 270f, 180f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (base.Enabled)
		{
			_arrastando = true;
			base.Capture = true;
			Mexer(e.X);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (_arrastando)
		{
			Mexer(e.X);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		_arrastando = false;
		base.Capture = false;
	}

	private void Mexer(int x)
	{
		int num = ValorDoX(x);
		if (num != _valor)
		{
			_valor = num;
			Invalidate();
			if (this.ValorMudou != null)
			{
				this.ValorMudou(this, EventArgs.Empty);
			}
		}
	}

	public void PorValor(int valor)
	{
		int num = valor;
		if (num < _minimo)
		{
			num = _minimo;
		}
		if (num > _maximo)
		{
			num = _maximo;
		}
		_valor = num;
		Invalidate();
	}

	public void Faixa(int minimo, int maximo, int valor)
	{
		_minimo = minimo;
		_maximo = ((maximo < minimo + 1) ? (minimo + 1) : maximo);
		PorValor(valor);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (base.Enabled)
		{
			int num = Math.Max(1, (_maximo - _minimo) / 100);
			if (e.KeyCode == Keys.Left)
			{
				Mexer(XDoValor(_valor - num));
				OnKeyUp(e);
			}
			if (e.KeyCode == Keys.Right)
			{
				Mexer(XDoValor(_valor + num));
				OnKeyUp(e);
			}
			base.OnKeyDown(e);
		}
	}
}
