using System;
using System.Drawing;
using System.Windows.Forms;
using MyVersion;

namespace Configurador;

internal sealed class JanelaAjustes : Form
{
	private readonly ArquivoPerfis _arquivo;

	private readonly Perfil _perfil;

	private readonly TextBox _txtDescricao = Tema.Caixa(somenteLeitura: false);

	private readonly BarraDeslizante _barraZona = new BarraDeslizante();

	private readonly NumericUpDown _numZona = Tema.Numero(0, 32767, 0);

	private readonly CheckBox _chkInicial = Tema.CaixaDeMarcar("usar este perfil quando o pad iniciar");

	private readonly BarraDeslizante _barraTique = new BarraDeslizante();

	private readonly NumericUpDown _numTique = Tema.Numero(1, 200, 8);

	private bool _sincronizando;

	public static bool Perguntar(IWin32Window dono, ArquivoPerfis arquivo, Perfil perfil)
	{
		using JanelaAjustes janelaAjustes = new JanelaAjustes(arquivo, perfil);
		return janelaAjustes.ShowDialog(dono) == DialogResult.OK;
	}

	private JanelaAjustes(ArquivoPerfis arquivo, Perfil perfil)
	{
		_arquivo = arquivo;
		_perfil = perfil;
		Text = "Ajustes do perfil e do arquivo";
		BackColor = Tema.Fundo;
		ForeColor = Tema.Texto;
		Font = Tema.Fonte;
		base.StartPosition = FormStartPosition.CenterParent;
		base.ClientSize = new Size(620, 400);
		MinimumSize = new Size(560, 380);
		Construir();
		Carregar();
		LigarBarra(_barraZona, _numZona);
		LigarBarra(_barraTique, _numTique);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		Tema.BarraDeTituloEscura(base.Handle);
		Tema.IconeDaJanela(this);
	}

	private void Construir()
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.BackColor = Tema.Fundo;
		tableLayoutPanel.Padding = new Padding(14, 10, 14, 10);
		tableLayoutPanel.ColumnCount = 2;
		tableLayoutPanel.RowCount = 9;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0f));
		Label label = Tema.Rotulo("Perfil: " + _perfil.Nome, fraco: false);
		label.Font = Tema.Fonte10N;
		label.ForeColor = Tema.Realce;
		tableLayoutPanel.Controls.Add(label, 0, 0);
		tableLayoutPanel.SetColumnSpan(label, 2);
		tableLayoutPanel.Controls.Add(Rotulo("Descricao do perfil (aparece na lista do pad)"), 0, 1);
		_txtDescricao.Margin = new Padding(0, 3, 0, 3);
		tableLayoutPanel.Controls.Add(_txtDescricao, 1, 1);
		tableLayoutPanel.Controls.Add(Rotulo("Zona morta dos analogicos (0 = nenhuma)"), 0, 2);
		tableLayoutPanel.Controls.Add(BarraComNumero(_barraZona, _numZona), 1, 2);
		tableLayoutPanel.Controls.Add(_chkInicial, 0, 3);
		tableLayoutPanel.SetColumnSpan(_chkInicial, 2);
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.BackColor = Tema.Borda;
		panel.Margin = new Padding(0, 5, 0, 5);
		tableLayoutPanel.Controls.Add(panel, 0, 4);
		tableLayoutPanel.SetColumnSpan(panel, 2);
		tableLayoutPanel.Controls.Add(Rotulo("Intervalo do pad - tique_ms (o pad le o teclado de tantos em tantos ms)"), 0, 5);
		tableLayoutPanel.Controls.Add(BarraComNumero(_barraTique, _numTique), 1, 5);
		Label label2 = Tema.Rotulo("Zona morta: quanto o analogico precisa sair do centro para o pad considerar movimento.\r\nIntervalo: mexer aqui nao liga nem desliga o pad; o valor vale quando o pad iniciar.", fraco: true);
		label2.Font = Tema.Fonte8;
		label2.TextAlign = ContentAlignment.TopLeft;
		tableLayoutPanel.Controls.Add(label2, 0, 6);
		tableLayoutPanel.SetColumnSpan(label2, 2);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel();
		tableLayoutPanel2.Dock = DockStyle.Fill;
		tableLayoutPanel2.BackColor = Tema.Fundo;
		tableLayoutPanel2.ColumnCount = 3;
		tableLayoutPanel2.RowCount = 1;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		Button button = Tema.Botao("Aplicar", principal: true);
		Button button2 = Tema.Botao("Cancelar", principal: false);
		button.Margin = new Padding(0, 3, 6, 3);
		button2.Margin = new Padding(0, 3, 0, 3);
		button.Click += delegate
		{
			Aplicar();
		};
		button2.Click += delegate
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		};
		tableLayoutPanel2.Controls.Add(new Panel
		{
			BackColor = Tema.Fundo,
			Dock = DockStyle.Fill
		}, 0, 0);
		tableLayoutPanel2.Controls.Add(button, 1, 0);
		tableLayoutPanel2.Controls.Add(button2, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 7);
		tableLayoutPanel.SetColumnSpan(tableLayoutPanel2, 2);
		base.Controls.Add(tableLayoutPanel);
	}

	private static Label Rotulo(string texto)
	{
		Label label = Tema.Rotulo(texto, fraco: false);
		label.Font = Tema.Fonte8;
		label.TextAlign = ContentAlignment.MiddleLeft;
		return label;
	}

	private static Control BarraComNumero(BarraDeslizante barra, NumericUpDown numero)
	{
		TableLayoutPanel obj = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			ColumnCount = 2,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f),
				new ColumnStyle(SizeType.Absolute, 86f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			},
			Margin = new Padding(0, 3, 0, 3)
		};
		barra.Margin = new Padding(0, 4, 8, 4);
		numero.Margin = new Padding(0, 4, 0, 4);
		obj.Controls.Add(barra, 0, 0);
		obj.Controls.Add(numero, 1, 0);
		return obj;
	}

	private void Carregar()
	{
		_txtDescricao.Text = _perfil.Descricao ?? "";
		_sincronizando = true;
		try
		{
			_barraZona.Faixa(0, 32767, Limitar(_perfil.ZonaMorta, 0, 32767));
			_numZona.Increment = 100m;
			_numZona.Value = Limitar(_perfil.ZonaMorta, 0, 32767);
			_barraTique.Faixa(1, 200, Limitar(_arquivo.TiqueMs, 1, 200));
			_numTique.Value = Limitar(_arquivo.TiqueMs, 1, 200);
			_chkInicial.Checked = string.Equals(_arquivo.PerfilInicial, _perfil.Nome, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			_sincronizando = false;
		}
	}

	private void LigarBarra(BarraDeslizante barra, NumericUpDown numero)
	{
		barra.ValorMudou += delegate
		{
			if (_sincronizando)
			{
				return;
			}
			_sincronizando = true;
			try
			{
				numero.Value = Limitar(barra.Valor, (int)numero.Minimum, (int)numero.Maximum);
			}
			finally
			{
				_sincronizando = false;
			}
		};
		numero.ValueChanged += delegate
		{
			if (_sincronizando)
			{
				return;
			}
			_sincronizando = true;
			try
			{
				barra.Valor = (int)numero.Value;
			}
			finally
			{
				_sincronizando = false;
			}
		};
	}

	private void Aplicar()
	{
		_perfil.Descricao = (_txtDescricao.Text ?? "").Trim();
		_perfil.ZonaMorta = Limitar((int)_numZona.Value, 0, 32767);
		_arquivo.TiqueMs = Limitar((int)_numTique.Value, 1, 200);
		if (_chkInicial.Checked)
		{
			_arquivo.PerfilInicial = _perfil.Nome;
		}
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private static int Limitar(int v, int minimo, int maximo)
	{
		if (v < minimo)
		{
			return minimo;
		}
		if (v > maximo)
		{
			return maximo;
		}
		return v;
	}
}
