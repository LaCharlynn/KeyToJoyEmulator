using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyVersion;

namespace Configurador;

internal sealed class JanelaAtalhos : Form
{
	private readonly ArquivoPerfis _arquivo;

	private readonly Perfil _perfil;

	private readonly List<Atalho> _atalhos = new List<Atalho>();

	private readonly ListBox _lstAtalhos = Tema.Lista(26);

	private readonly ListBox _lstPerfis = Tema.Lista(22);

	private readonly Label _lblCombinacao = new Label();

	private readonly Button _btnCapturar = Tema.Botao("Capturar combinacao...", principal: true);

	private readonly Button _btnAdicionar = Tema.Botao("Adicionar", principal: false);

	private readonly Button _btnRemover = Tema.Botao("Remover o atalho escolhido", principal: false);

	private string _combinacao;

	public static bool Perguntar(IWin32Window dono, ArquivoPerfis arquivo, Perfil perfil)
	{
		using JanelaAtalhos janelaAtalhos = new JanelaAtalhos(arquivo, perfil);
		return janelaAtalhos.ShowDialog(dono) == DialogResult.OK;
	}

	private JanelaAtalhos(ArquivoPerfis arquivo, Perfil perfil)
	{
		_arquivo = arquivo;
		_perfil = perfil;
		Text = "Atalhos do perfil '" + perfil.Nome + "'";
		BackColor = Tema.Fundo;
		ForeColor = Tema.Texto;
		Font = Tema.Fonte;
		base.StartPosition = FormStartPosition.CenterParent;
		base.ClientSize = new Size(600, 520);
		MinimumSize = new Size(560, 480);
		foreach (Atalho atalho in perfil.Atalhos)
		{
			_atalhos.Add(atalho);
		}
		Construir();
		RecarregarListaDaTela(manterEscolha: false);
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
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 8;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 94f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		Label label = Tema.Rotulo("Atalhos do perfil '" + _perfil.Nome + "'  (a combinacao troca o perfil do pad na hora)", fraco: true);
		label.Font = Tema.Fonte8;
		tableLayoutPanel.Controls.Add(label, 0, 0);
		tableLayoutPanel.Controls.Add(_lstAtalhos, 0, 1);
		_lstAtalhos.DrawItem += DesenharAtalho;
		_btnRemover.Margin = new Padding(0, 3, 0, 3);
		_btnRemover.Click += delegate
		{
			Remover();
		};
		tableLayoutPanel.Controls.Add(_btnRemover, 0, 2);
		Panel panel = new Panel();
		panel.Dock = DockStyle.Fill;
		panel.BackColor = Tema.Borda;
		panel.Margin = new Padding(0, 4, 0, 4);
		tableLayoutPanel.Controls.Add(panel, 0, 3);
		_lblCombinacao.Dock = DockStyle.Fill;
		_lblCombinacao.Font = Tema.Fonte8N;
		_lblCombinacao.ForeColor = Tema.Texto2;
		_lblCombinacao.TextAlign = ContentAlignment.MiddleLeft;
		_lblCombinacao.Text = "Combinacao: (aperte o botao abaixo e depois a combinacao)";
		tableLayoutPanel.Controls.Add(_lblCombinacao, 0, 4);
		_btnCapturar.Margin = new Padding(0, 3, 0, 3);
		_btnCapturar.Click += delegate
		{
			Capturar();
		};
		tableLayoutPanel.Controls.Add(_btnCapturar, 0, 5);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel();
		tableLayoutPanel2.Dock = DockStyle.Fill;
		tableLayoutPanel2.BackColor = Tema.Fundo;
		tableLayoutPanel2.Margin = new Padding(0);
		tableLayoutPanel2.ColumnCount = 3;
		tableLayoutPanel2.RowCount = 1;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		Label label2 = Tema.Rotulo("Trocar para o perfil:", fraco: false);
		label2.Font = Tema.Fonte8;
		tableLayoutPanel2.Controls.Add(label2, 0, 0);
		_lstPerfis.Margin = new Padding(0, 2, 8, 2);
		_lstPerfis.DrawItem += DesenharPerfil;
		foreach (KeyValuePair<string, Perfil> perfi in _arquivo.Perfis)
		{
			_lstPerfis.Items.Add(perfi.Key);
		}
		if (_lstPerfis.Items.Count > 0)
		{
			_lstPerfis.SelectedIndex = 0;
		}
		tableLayoutPanel2.Controls.Add(_lstPerfis, 1, 0);
		int num = ((_lstPerfis.Items.Count > 5) ? 5 : _lstPerfis.Items.Count);
		int num2 = _lstPerfis.PreferredHeight - Math.Max(0, _lstPerfis.Items.Count - num) * _lstPerfis.ItemHeight;
		tableLayoutPanel.RowStyles[6].Height = Math.Max(34f, (float)(num2 + _lstPerfis.Margin.Vertical) + 4f);
		_btnAdicionar.Margin = new Padding(0, 2, 0, 2);
		_btnAdicionar.Click += delegate
		{
			Adicionar();
		};
		tableLayoutPanel2.Controls.Add(_btnAdicionar, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 6);
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel();
		tableLayoutPanel3.Dock = DockStyle.Fill;
		tableLayoutPanel3.BackColor = Tema.Fundo;
		tableLayoutPanel3.ColumnCount = 3;
		tableLayoutPanel3.RowCount = 1;
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		Button button = Tema.Botao("Aplicar", principal: true);
		Button button2 = Tema.Botao("Cancelar", principal: false);
		button.Margin = new Padding(0, 6, 6, 0);
		button2.Margin = new Padding(0, 6, 0, 0);
		button.Click += delegate
		{
			Aplicar();
		};
		button2.Click += delegate
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		};
		tableLayoutPanel3.Controls.Add(new Panel
		{
			BackColor = Tema.Fundo,
			Dock = DockStyle.Fill
		}, 0, 0);
		tableLayoutPanel3.Controls.Add(button, 1, 0);
		tableLayoutPanel3.Controls.Add(button2, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel3, 0, 7);
		base.Controls.Add(tableLayoutPanel);
	}

	private void RecarregarListaDaTela(bool manterEscolha)
	{
		int selectedIndex = _lstAtalhos.SelectedIndex;
		_lstAtalhos.Items.Clear();
		foreach (Atalho atalho in _atalhos)
		{
			_lstAtalhos.Items.Add(atalho.Combinacao + "   ->   " + atalho.PerfilDestino);
		}
		if (_lstAtalhos.Items.Count > 0)
		{
			_lstAtalhos.SelectedIndex = ((manterEscolha && selectedIndex >= 0 && selectedIndex < _lstAtalhos.Items.Count) ? selectedIndex : 0);
		}
	}

	private void DesenharAtalho(object remetente, DrawItemEventArgs e)
	{
		if (e.Index < 0 || e.Index >= _lstAtalhos.Items.Count)
		{
			return;
		}
		using (SolidBrush brush = new SolidBrush(((e.State & DrawItemState.Selected) != 0) ? Tema.RealceEscuro : Tema.Painel2))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}
		using SolidBrush brush2 = new SolidBrush(Tema.Texto);
		using StringFormat format = new StringFormat
		{
			LineAlignment = StringAlignment.Center
		};
		e.Graphics.DrawString((string)_lstAtalhos.Items[e.Index], Tema.Fonte8N, brush2, new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height), format);
	}

	private void DesenharPerfil(object remetente, DrawItemEventArgs e)
	{
		if (e.Index < 0 || e.Index >= _lstPerfis.Items.Count)
		{
			return;
		}
		using (SolidBrush brush = new SolidBrush(((e.State & DrawItemState.Selected) != 0) ? Tema.RealceEscuro : Tema.Painel2))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}
		using SolidBrush brush2 = new SolidBrush(Tema.Texto);
		using StringFormat format = new StringFormat
		{
			LineAlignment = StringAlignment.Center
		};
		e.Graphics.DrawString((string)_lstPerfis.Items[e.Index], Tema.Fonte8, brush2, new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height), format);
	}

	private void Capturar()
	{
		string combinacaoCapturada;
		using (CapturaDeTecla capturaDeTecla = new CapturaDeTecla(combinacao: true))
		{
			if (capturaDeTecla.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			combinacaoCapturada = capturaDeTecla.CombinacaoCapturada;
		}
		if (string.IsNullOrWhiteSpace(combinacaoCapturada))
		{
			return;
		}
		List<string> list = new List<string>();
		string[] array = combinacaoCapturada.Split('+');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length != 0)
			{
				if (!TabelaTeclas.TentarResolver(text, out var tecla))
				{
					_lblCombinacao.Text = "Nao entendi a tecla '" + text + "'. Tente de novo.";
					_combinacao = null;
					return;
				}
				string item = TabelaTeclas.NomeDe(tecla);
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count == 0)
		{
			_lblCombinacao.Text = "A combinacao ficou vazia. Tente de novo.";
			_combinacao = null;
		}
		else
		{
			_combinacao = string.Join("+", list.ToArray());
			_lblCombinacao.Text = "Combinacao: " + _combinacao + "   (escolha o perfil e clique em Adicionar)";
		}
	}

	private void Adicionar()
	{
		if (_combinacao == null)
		{
			_lblCombinacao.Text = "Clique primeiro em 'Capturar combinacao...'.";
			return;
		}
		if (_lstPerfis.SelectedIndex < 0)
		{
			_lblCombinacao.Text = "Escolha para qual perfil a combinacao deve trocar.";
			return;
		}
		string text = (string)_lstPerfis.Items[_lstPerfis.SelectedIndex];
		for (int num = _atalhos.Count - 1; num >= 0; num--)
		{
			if (string.Equals(_atalhos[num].Combinacao, _combinacao, StringComparison.OrdinalIgnoreCase))
			{
				_atalhos.RemoveAt(num);
			}
		}
		Atalho atalho = new Atalho
		{
			Combinacao = _combinacao,
			PerfilDestino = text
		};
		string[] array = _combinacao.Split('+');
		for (int i = 0; i < array.Length; i++)
		{
			if (TabelaTeclas.TentarResolver(array[i].Trim(), out var tecla) && !atalho.Teclas.Contains(tecla))
			{
				atalho.Teclas.Add(tecla);
			}
		}
		_atalhos.Add(atalho);
		_lblCombinacao.Text = "Atalho " + _combinacao + " -> " + text + " acrescentado.";
		_combinacao = null;
		RecarregarListaDaTela(manterEscolha: false);
	}

	private void Remover()
	{
		int selectedIndex = _lstAtalhos.SelectedIndex;
		if (selectedIndex >= 0 && selectedIndex < _atalhos.Count)
		{
			string combinacao = _atalhos[selectedIndex].Combinacao;
			_atalhos.RemoveAt(selectedIndex);
			RecarregarListaDaTela(manterEscolha: false);
			_lblCombinacao.Text = "Atalho " + combinacao + " removido.";
		}
	}

	private void Aplicar()
	{
		_perfil.Atalhos = _atalhos;
		base.DialogResult = DialogResult.OK;
		Close();
	}
}
