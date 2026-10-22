using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Configurador;

internal static class Tema
{
	public static readonly Color Fundo = Color.FromArgb(23, 25, 26);

	public static readonly Color Painel = Color.FromArgb(31, 34, 35);

	public static readonly Color Painel2 = Color.FromArgb(41, 45, 46);

	public static readonly Color Painel3 = Color.FromArgb(51, 56, 57);

	public static readonly Color Borda = Color.FromArgb(58, 63, 64);

	public static readonly Color Texto = Color.FromArgb(231, 234, 236);

	public static readonly Color Texto2 = Color.FromArgb(146, 153, 157);

	public static readonly Color Texto3 = Color.FromArgb(104, 110, 114);

	public static readonly Color Realce = Color.FromArgb(26, 188, 156);

	public static readonly Color RealceEscuro = Color.FromArgb(20, 92, 79);

	public static readonly Color Aceso = Color.FromArgb(46, 204, 113);

	public static readonly Color Verde = Color.FromArgb(38, 150, 96);

	public static readonly Color Vermelho = Color.FromArgb(190, 66, 58);

	public static readonly Color Amarelo = Color.FromArgb(226, 188, 82);

	public static readonly Font Fonte = new Font("Segoe UI", 9f);

	public static readonly Font Fonte8 = new Font("Segoe UI", 8f);

	public static readonly Font Fonte8N = new Font("Segoe UI", 8f, FontStyle.Bold);

	public static readonly Font Fonte75 = new Font("Segoe UI", 7.5f);

	public static readonly Font Fonte10N = new Font("Segoe UI", 10f, FontStyle.Bold);

	public static readonly Font Fonte12N = new Font("Segoe UI", 12f, FontStyle.Bold);

	public static readonly Font FonteMono = new Font("Consolas", 8.25f);

	public static Label Rotulo(string texto, bool fraco)
	{
		return new Label
		{
			Text = texto,
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = (fraco ? Texto2 : Texto),
			AutoSize = false
		};
	}

	public static Label TituloDoCartao(string texto)
	{
		return new Label
		{
			Text = texto.ToUpperInvariant(),
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			ForeColor = Texto2,
			Font = Fonte8N,
			Margin = new Padding(2, 0, 0, 2)
		};
	}

	public static Button Botao(string texto, bool principal)
	{
		Button button = new Button();
		button.Text = texto;
		button.FlatStyle = FlatStyle.Flat;
		button.UseVisualStyleBackColor = false;
		button.FlatAppearance.BorderSize = 1;
		button.FlatAppearance.BorderColor = (principal ? RealceEscuro : Borda);
		button.BackColor = (principal ? RealceEscuro : Painel2);
		button.ForeColor = Texto;
		button.FlatAppearance.MouseOverBackColor = (principal ? Realce : Painel3);
		button.FlatAppearance.MouseDownBackColor = (principal ? RealceEscuro : Borda);
		button.Dock = DockStyle.Fill;
		button.Margin = new Padding(0, 0, 5, 0);
		button.Font = Fonte;
		button.TabStop = false;
		return button;
	}

	public static Button BotaoPequeno(string texto, int largura)
	{
		Button button = Botao(texto, principal: false);
		button.Dock = DockStyle.None;
		button.Width = largura;
		button.Height = 26;
		button.Margin = new Padding(0, 0, 5, 0);
		return button;
	}

	public static TextBox Caixa(bool somenteLeitura)
	{
		return new TextBox
		{
			BorderStyle = BorderStyle.FixedSingle,
			BackColor = (somenteLeitura ? Painel : Painel2),
			ForeColor = (somenteLeitura ? Texto2 : Texto),
			ReadOnly = somenteLeitura,
			Dock = DockStyle.Fill,
			Margin = new Padding(0)
		};
	}

	public static NumericUpDown Numero(int minimo, int maximo, int valor)
	{
		NumericUpDown i = new NumericUpDown();
		i.Minimum = minimo;
		i.Maximum = maximo;
		i.Value = valor;
		i.ThousandsSeparator = true;
		i.BorderStyle = BorderStyle.FixedSingle;
		i.BackColor = Painel2;
		i.ForeColor = Texto;
		i.Dock = DockStyle.Fill;
		i.Margin = new Padding(0);
		foreach (Control control3 in i.Controls)
		{
			if (string.Equals(control3.GetType().Name, "UpDownButtons", StringComparison.Ordinal))
			{
				control3.Visible = false;
			}
		}
		i.Paint += delegate(object remetente, PaintEventArgs e)
		{
			foreach (Control control4 in i.Controls)
			{
				if (string.Equals(control4.GetType().Name, "UpDownEdit", StringComparison.Ordinal))
				{
					int right = control4.Right;
					int num = i.ClientSize.Width - right;
					if (num <= 0)
					{
						break;
					}
					using SolidBrush brush = new SolidBrush(Painel2);
					e.Graphics.FillRectangle(brush, new Rectangle(right, 0, num, i.ClientSize.Height));
					break;
				}
			}
		};
		return i;
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint janela, int atributo, ref int valor, int tamanho);

	public static void IconeDaJanela(Form janela)
	{
		if (janela == null)
		{
			return;
		}
		try
		{
			string processPath = Environment.ProcessPath;
			if (!string.IsNullOrEmpty(processPath))
			{
				janela.Icon = Icon.ExtractAssociatedIcon(processPath);
			}
		}
		catch (ArgumentException)
		{
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	public static void BarraDeTituloEscura(nint janela)
	{
		int valor = 1;
		try
		{
			if (DwmSetWindowAttribute(janela, 20, ref valor, 4) != 0)
			{
				DwmSetWindowAttribute(janela, 19, ref valor, 4);
			}
		}
		catch (DllNotFoundException)
		{
		}
		catch (EntryPointNotFoundException)
		{
		}
	}

	public static Panel Cartao(string titulo, Padding margem, out Panel conteudo)
	{
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Painel,
			Margin = margem,
			Padding = new Padding(10, 6, 10, 8)
		};
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Painel,
			ColumnCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			}
		};
		int row = 0;
		if (titulo != null)
		{
			tableLayoutPanel.RowCount = 2;
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			tableLayoutPanel.Controls.Add(TituloDoCartao(titulo), 0, 0);
			row = 1;
		}
		else
		{
			tableLayoutPanel.RowCount = 1;
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		}
		conteudo = new Panel();
		conteudo.Dock = DockStyle.Fill;
		conteudo.BackColor = Painel;
		conteudo.Margin = new Padding(0);
		tableLayoutPanel.Controls.Add(conteudo, 0, row);
		obj.Controls.Add(tableLayoutPanel);
		return obj;
	}

	public static ListBox Lista(int alturaDaLinha)
	{
		ListBox i = new ListBox();
		i.BorderStyle = BorderStyle.None;
		i.BackColor = Painel;
		i.ForeColor = Texto;
		i.DrawMode = DrawMode.OwnerDrawFixed;
		i.ItemHeight = alturaDaLinha;
		i.IntegralHeight = false;
		i.Dock = DockStyle.Fill;
		i.HandleCreated += delegate
		{
			EscurecerBarraDeRolagem(i.Handle);
		};
		return i;
	}

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTheme(nint janela, string subApp, string subId);

	private static void EscurecerBarraDeRolagem(nint janela)
	{
		if (janela == IntPtr.Zero)
		{
			return;
		}
		try
		{
			SetWindowTheme(janela, "DarkMode_Explorer", null);
		}
		catch
		{
		}
	}

	public static Panel Separador()
	{
		return new Panel
		{
			Height = 1,
			Dock = DockStyle.Top,
			BackColor = Borda,
			Margin = new Padding(0, 6, 0, 6)
		};
	}

	public static CheckBox CaixaDeMarcar(string texto)
	{
		CheckBox checkBox = new CheckBox();
		checkBox.Text = texto;
		checkBox.ForeColor = Texto;
		checkBox.BackColor = Color.Transparent;
		checkBox.FlatStyle = FlatStyle.Flat;
		checkBox.FlatAppearance.BorderColor = Texto2;
		checkBox.Dock = DockStyle.Fill;
		checkBox.Margin = new Padding(0);
		return checkBox;
	}
}
