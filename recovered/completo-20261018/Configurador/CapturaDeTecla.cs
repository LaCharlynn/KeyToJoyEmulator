using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyVersion;

namespace Configurador;

public sealed class CapturaDeTecla : Form
{
	private struct MSG
	{
		public nint hwnd;

		public uint message;

		public nint wParam;

		public nint lParam;

		public uint time;

		public int x;

		public int y;
	}

	private readonly bool _combinacao;

	private readonly List<CodigoTecla> _teclas = new List<CodigoTecla>();

	private readonly ManualResetEvent _pronto = new ManualResetEvent(initialState: false);

	private Thread _thread;

	private GanchoTeclado _gancho;

	private string _erroDoGancho;

	private volatile string _ultimaLinhaCrua;

	private readonly Label _lblInstrucao = new Label();

	private readonly Label _lblCru = new Label();

	private readonly Label _lblNome = new Label();

	private readonly Label _lblCombinacao = new Label();

	private readonly Button _btnLimpar = new Button();

	private readonly Button _btnUsar = new Button();

	private readonly Button _btnCancelar = new Button();

	public string NomeCapturado { get; private set; }

	public string CombinacaoCapturada { get; private set; }

	[DllImport("user32.dll")]
	private static extern bool PeekMessageW(out MSG lpMsg, nint hWnd, uint min, uint max, uint remover);

	public CapturaDeTecla(bool combinacao)
	{
		_combinacao = combinacao;
		Text = (combinacao ? "Capturar combinacao" : "Capturar tecla");
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.StartPosition = FormStartPosition.CenterParent;
		base.MinimizeBox = false;
		base.MaximizeBox = false;
		base.ShowInTaskbar = false;
		base.ClientSize = new Size(580, 290);
		Tema.IconeDaJanela(this);
		_lblInstrucao.SetBounds(12, 12, 556, 38);
		_lblInstrucao.Text = (combinacao ? "Aperte as teclas da combinacao (todas juntas, como voce vai usar no jogo).\r\nA janela so fecha nos botoes, com o mouse - qualquer tecla pode ser capturada." : "Aperte a tecla que voce quer mapear.\r\nA janela so fecha nos botoes, com o mouse - qualquer tecla pode ser capturada.");
		_lblCru.SetBounds(12, 56, 556, 46);
		_lblCru.Font = new Font(FontFamily.GenericMonospace, 9f);
		_lblCru.BorderStyle = BorderStyle.FixedSingle;
		_lblCru.Text = "(esperando voce apertar uma tecla...)";
		_lblNome.SetBounds(12, 108, 556, 26);
		_lblNome.Font = new Font(Font, FontStyle.Bold);
		_lblNome.Text = "nome no arquivo de perfis: (nada ainda)";
		_lblCombinacao.SetBounds(12, 138, 556, 60);
		_lblCombinacao.Text = "(nenhuma tecla ainda)";
		_lblCombinacao.Visible = combinacao;
		_btnLimpar.SetBounds(12, 238, 110, 32);
		_btnLimpar.Text = "Limpar";
		_btnLimpar.TabStop = false;
		_btnLimpar.Click += delegate
		{
			_teclas.Clear();
			_ultimaLinhaCrua = null;
			Mostrar();
		};
		_btnUsar.SetBounds(336, 238, 130, 32);
		_btnUsar.Text = (combinacao ? "Usar combinacao" : "Usar esta tecla");
		_btnUsar.TabStop = false;
		_btnUsar.Enabled = false;
		_btnUsar.Click += BotaoUsar;
		_btnCancelar.SetBounds(474, 238, 94, 32);
		_btnCancelar.Text = "Cancelar";
		_btnCancelar.TabStop = false;
		_btnCancelar.Click += delegate
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		};
		base.Controls.Add(_lblInstrucao);
		base.Controls.Add(_lblCru);
		base.Controls.Add(_lblNome);
		base.Controls.Add(_lblCombinacao);
		base.Controls.Add(_btnLimpar);
		base.Controls.Add(_btnUsar);
		base.Controls.Add(_btnCancelar);
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		base.ActiveControl = null;
		_thread = new Thread(RodarGancho);
		_thread.IsBackground = true;
		_thread.Name = "captura-de-tecla";
		_thread.Start();
		if (!_pronto.WaitOne(3000))
		{
			_lblCru.Text = "o gancho de teclado nao respondeu em 3 s";
		}
		else if (_erroDoGancho != null)
		{
			_lblCru.Text = "nao consegui instalar o gancho de teclado: " + _erroDoGancho;
			MessageBox.Show(this, "Nao consegui instalar o gancho de teclado:\r\n" + _erroDoGancho, "captura de tecla", MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private void RodarGancho()
	{
		PeekMessageW(out var _, IntPtr.Zero, 0u, 0u, 0u);
		GanchoTeclado ganchoTeclado = new GanchoTeclado(AoEvento, AoTexto);
		if (!ganchoTeclado.Instalar(out var erro))
		{
			_erroDoGancho = erro;
			_pronto.Set();
		}
		else
		{
			_gancho = ganchoTeclado;
			_pronto.Set();
			ganchoTeclado.BombearMensagens();
		}
	}

	private void AoTexto(string texto)
	{
		if (texto != null && texto.Contains("apertou"))
		{
			_ultimaLinhaCrua = texto;
		}
	}

	private void AoEvento(CodigoTecla tecla, bool pressionada)
	{
		if (!pressionada)
		{
			return;
		}
		try
		{
			BeginInvoke(new Action<CodigoTecla>(TeclaApertada), tecla);
		}
		catch
		{
		}
	}

	private void TeclaApertada(CodigoTecla tecla)
	{
		if (_combinacao)
		{
			if (!_teclas.Contains(tecla))
			{
				_teclas.Add(tecla);
			}
		}
		else
		{
			_teclas.Clear();
			_teclas.Add(tecla);
		}
		Mostrar();
	}

	private void Mostrar()
	{
		List<string> list = new List<string>();
		bool flag = true;
		foreach (CodigoTecla tecla in _teclas)
		{
			string text = TabelaTeclas.NomeDe(tecla);
			if (text == null)
			{
				flag = false;
				text = "SEM NOME (" + tecla.ToString() + ")";
			}
			list.Add(text);
		}
		_lblCru.Text = _ultimaLinhaCrua ?? "(esperando voce apertar uma tecla...)";
		if (_combinacao)
		{
			_lblCombinacao.Text = ((list.Count == 0) ? "(nenhuma tecla ainda)" : ("combinacao: " + string.Join(" + ", list.ToArray())));
			_lblNome.Text = (flag ? ("vai para o arquivo como: " + string.Join("+", list.ToArray())) : "nome no arquivo de perfis: uma das teclas nao tem nome hoje");
		}
		else
		{
			_lblNome.Text = ((list.Count == 0) ? "nome no arquivo de perfis: (nada ainda)" : (flag ? ("nome no arquivo de perfis: " + list[0]) : "nome no arquivo de perfis: esta tecla nao tem nome hoje"));
		}
		_btnUsar.Enabled = list.Count > 0 && flag;
	}

	private void BotaoUsar(object remetente, EventArgs e)
	{
		List<string> list = new List<string>();
		foreach (CodigoTecla tecla in _teclas)
		{
			string text = TabelaTeclas.NomeDe(tecla);
			if (text == null)
			{
				return;
			}
			list.Add(text);
		}
		if (list.Count != 0)
		{
			if (_combinacao)
			{
				CombinacaoCapturada = string.Join("+", list.ToArray());
			}
			else
			{
				NomeCapturado = list[0];
			}
			base.DialogResult = DialogResult.OK;
			Close();
		}
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		return true;
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		DesligarGancho();
		base.OnFormClosed(e);
	}

	private void DesligarGancho()
	{
		GanchoTeclado gancho = _gancho;
		if (gancho == null)
		{
			return;
		}
		_gancho = null;
		try
		{
			gancho.PedirParada();
		}
		catch
		{
		}
		if (_thread != null)
		{
			try
			{
				_thread.Join(1500);
			}
			catch
			{
			}
		}
		try
		{
			gancho.Dispose();
		}
		catch
		{
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DesligarGancho();
		}
		base.Dispose(disposing);
	}
}
