using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MyVersion;

namespace Configurador;

public sealed class JanelaPrincipal : Form
{
	private ArquivoPerfis _arquivo;

	private string _caminho;

	private string _perfilNaTela;

	private bool _sujo;

	private bool _carregando;

	private bool _sincronizando;

	private int _contadorDoRelogio;

	private readonly ProcessoPad _pad = new ProcessoPad();

	private string _caminhoDoPadExe = ProcessoPad.ProcurarExecutavel();

	private readonly List<Mapeamento> _mapasNaLista = new List<Mapeamento>();

	private readonly ToolTip _dicas = new ToolTip();

	private readonly ListBox _lstPerfis = Tema.Lista(44);

	private readonly DesenhoControle _desenho = new DesenhoControle();

	private readonly ListBox _lstTeclas = Tema.Lista(24);

	private readonly Label _lblSelecionado = new Label();

	private readonly Label _lblDirecao = new Label();

	private readonly Label _lblDica = new Label();

	private readonly Label _lblAjudaDesenho = new Label();

	private readonly Label _lblVivo = new Label();

	private readonly Label _lblStatus = new Label();

	private readonly Label _lblArquivo = new Label();

	private readonly Label _lblEstadoPad = new Label();

	private readonly Label _lblResumo = new Label();

	private RowStyle _linhaDoEixo;

	private readonly Button _btnCapturar = Tema.Botao("Capturar tecla", principal: true);

	private readonly Button _btnTeclaExtra = Tema.Botao("+ tecla extra", principal: false);

	private readonly Button _btnRemover = Tema.Botao("Remover", principal: false);

	private readonly Button _btnAvancado = Tema.Botao("Avancado", principal: false);

	private readonly Button _btnLigarPad = Tema.Botao("Ligar o pad", principal: false);

	private readonly Button _btnNovoPerfil = Tema.Botao("+ Novo perfil", principal: true);

	private readonly Button _btnDuplicar = Tema.Botao("Duplicar", principal: false);

	private readonly Button _btnRenomear = Tema.Botao("Renomear", principal: false);

	private readonly Button _btnExcluir = Tema.Botao("Excluir", principal: false);

	private readonly Button _btnAjustes = Tema.Botao("Ajustes do perfil e do arquivo...", principal: false);

	private readonly Button _btnAbrir = Tema.Botao("Abrir...", principal: false);

	private readonly Button _btnRecarregar = Tema.Botao("Recarregar", principal: false);

	private readonly Button _btnSalvar = Tema.Botao("Salvar", principal: true);

	private readonly Button _btnAtalhos = Tema.Botao("Atalhos de teclado...", principal: false);

	private readonly CheckBox _chkInicial = Tema.CaixaDeMarcar("usar este perfil quando o pad iniciar");

	private readonly CheckBox _chkReiniciar = Tema.CaixaDeMarcar("reiniciar o pad ao salvar");

	private readonly CheckBox _chkMesmaVolta = Tema.CaixaDeMarcar("igual a rampa de ida");

	private readonly BarraDeslizante _barraSensibilidade = new BarraDeslizante();

	private readonly BarraDeslizante _barraRampaIda = new BarraDeslizante();

	private readonly BarraDeslizante _barraRampaVolta = new BarraDeslizante();

	private readonly NumericUpDown _numSensibilidade = Tema.Numero(1, 32767, 32767);

	private readonly NumericUpDown _numRampaIda = Tema.Numero(0, 2000, 150);

	private readonly NumericUpDown _numRampaVolta = Tema.Numero(0, 2000, 150);

	private readonly Panel _painelDoEixo = new Panel();

	private readonly Panel _painelAvancado = new Panel();

	private readonly Timer _tmrVivo = new Timer();

	public JanelaPrincipal(string arquivo)
	{
		Text = "KeyToJoyEmulator - configurador do controle virtual";
		BackColor = Tema.Fundo;
		ForeColor = Tema.Texto;
		Font = Tema.Fonte;
		base.StartPosition = FormStartPosition.CenterScreen;
		base.ClientSize = new Size(1270, 850);
		MinimumSize = new Size(1120, 760);
		ConstruirTela();
		LigarEventos();
		_tmrVivo.Interval = 60;
		_tmrVivo.Tick += TicVivo;
		_tmrVivo.Start();
		AbrirArquivo(string.IsNullOrWhiteSpace(arquivo) ? PerfilPadrao() : arquivo);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		Tema.BarraDeTituloEscura(base.Handle);
		Tema.IconeDaJanela(this);
	}

	private void ConstruirTela()
	{
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.BackColor = Tema.Fundo;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f));
		tableLayoutPanel.Controls.Add(ConstruirMenu(), 0, 0);
		tableLayoutPanel.Controls.Add(ConstruirCorpo(), 0, 1);
		tableLayoutPanel.Controls.Add(ConstruirRodape(), 0, 2);
		base.Controls.Add(tableLayoutPanel);
	}

	private Control ConstruirMenu()
	{
		MenuStrip menuStrip = new MenuStrip();
		menuStrip.Dock = DockStyle.Fill;
		menuStrip.BackColor = Tema.Fundo;
		menuStrip.ForeColor = Tema.Texto2;
		menuStrip.Renderer = new MenuEscuro();
		menuStrip.Padding = new Padding(6, 0, 0, 0);
		menuStrip.Font = Tema.Fonte;
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Arquivo");
		toolStripMenuItem.DropDownItems.Add(NovoItem("Abrir...", EscolherArquivo));
		toolStripMenuItem.DropDownItems.Add(NovoItem("Recarregar do disco", RecarregarDoDisco));
		toolStripMenuItem.DropDownItems.Add(NovoItem("Salvar", delegate
		{
			Salvar();
		}));
		toolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem.DropDownItems.Add(NovoItem("Sair", delegate
		{
			Close();
		}));
		ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("Perfil");
		toolStripMenuItem2.DropDownItems.Add(NovoItem("Novo perfil", CriarPerfil));
		toolStripMenuItem2.DropDownItems.Add(NovoItem("Duplicar perfil", DuplicarPerfil));
		toolStripMenuItem2.DropDownItems.Add(NovoItem("Renomear perfil", RenomearPerfil));
		toolStripMenuItem2.DropDownItems.Add(NovoItem("Excluir perfil", ExcluirPerfil));
		toolStripMenuItem2.DropDownItems.Add(new ToolStripSeparator());
		toolStripMenuItem2.DropDownItems.Add(NovoItem("Ajustes deste perfil e do arquivo...", AbrirAjustes));
		ToolStripMenuItem toolStripMenuItem3 = new ToolStripMenuItem("Ajustes");
		toolStripMenuItem3.DropDownItems.Add(NovoItem("Atalhos de teclado deste perfil...", AbrirAtalhos));
		toolStripMenuItem3.DropDownItems.Add(NovoItem("Ajustes deste perfil e do arquivo...", AbrirAjustes));
		ToolStripMenuItem toolStripMenuItem4 = new ToolStripMenuItem("Ajuda");
		toolStripMenuItem4.DropDownItems.Add(NovoItem("Como usar", MostrarComoUsar));
		toolStripMenuItem4.DropDownItems.Add(NovoItem("Sobre", MostrarSobre));
		menuStrip.Items.Add(toolStripMenuItem);
		menuStrip.Items.Add(toolStripMenuItem2);
		menuStrip.Items.Add(toolStripMenuItem3);
		menuStrip.Items.Add(toolStripMenuItem4);
		base.MainMenuStrip = menuStrip;
		return menuStrip;
	}

	private static ToolStripMenuItem NovoItem(string texto, Action aoClicar)
	{
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(texto);
		toolStripMenuItem.ForeColor = Tema.Texto;
		toolStripMenuItem.BackColor = Tema.Painel2;
		toolStripMenuItem.Click += delegate
		{
			aoClicar();
		};
		return toolStripMenuItem;
	}

	private Control ConstruirCorpo()
	{
		return new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			ColumnCount = 3,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Absolute, 258f),
				new ColumnStyle(SizeType.Percent, 100f),
				new ColumnStyle(SizeType.Absolute, 346f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			},
			Controls = 
			{
				{
					ConstruirEsquerda(),
					0,
					0
				},
				{
					ConstruirCentro(),
					1,
					0
				},
				{
					ConstruirDireita(),
					2,
					0
				}
			}
		};
	}

	private Control ConstruirEsquerda()
	{
		Panel conteudo;
		Panel result = Tema.Cartao("Perfis do arquivo", new Padding(0, 0, 8, 0), out conteudo);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 1,
			RowCount = 6,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f),
				new RowStyle(SizeType.Absolute, 34f),
				new RowStyle(SizeType.Absolute, 32f),
				new RowStyle(SizeType.Absolute, 28f),
				new RowStyle(SizeType.Absolute, 34f),
				new RowStyle(SizeType.Absolute, 34f)
			},
			Controls = { 
			{
				(Control)_lstPerfis,
				0,
				0
			} }
		};
		_btnNovoPerfil.Margin = new Padding(0, 2, 0, 2);
		tableLayoutPanel.Controls.Add(_btnNovoPerfil, 0, 1);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 3,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 34f),
				new ColumnStyle(SizeType.Percent, 33f),
				new ColumnStyle(SizeType.Percent, 33f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			}
		};
		_btnDuplicar.Font = Tema.Fonte8;
		_btnRenomear.Font = Tema.Fonte8;
		_btnExcluir.Font = Tema.Fonte8;
		_btnDuplicar.Margin = new Padding(0, 1, 4, 1);
		_btnRenomear.Margin = new Padding(0, 1, 4, 1);
		_btnExcluir.Margin = new Padding(0, 1, 0, 1);
		tableLayoutPanel2.Controls.Add(_btnDuplicar, 0, 0);
		tableLayoutPanel2.Controls.Add(_btnRenomear, 1, 0);
		tableLayoutPanel2.Controls.Add(_btnExcluir, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 2);
		tableLayoutPanel.Controls.Add(_chkInicial, 0, 3);
		_btnAjustes.Margin = new Padding(0, 2, 0, 2);
		_btnAjustes.Text = "Ajustes do perfil...";
		tableLayoutPanel.Controls.Add(_btnAjustes, 0, 4);
		_btnAtalhos.Margin = new Padding(0, 2, 0, 0);
		tableLayoutPanel.Controls.Add(_btnAtalhos, 0, 5);
		conteudo.Controls.Add(tableLayoutPanel);
		return result;
	}

	private Control ConstruirCentro()
	{
		Panel conteudo;
		Panel result = Tema.Cartao("Controle - clique num botao e aperte a tecla que voce quer", new Padding(0, 0, 8, 0), out conteudo);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 1,
			RowCount = 2,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f),
				new RowStyle(SizeType.Absolute, 20f)
			}
		};
		_desenho.Dock = DockStyle.Fill;
		_desenho.Margin = new Padding(0);
		_lblAjudaDesenho.Text = "As teclas aparecem nas caixinhas. Caixinha apagada = alvo sem tecla (e alvo sem tecla nao vai para o arquivo).";
		_lblAjudaDesenho.Dock = DockStyle.Fill;
		_lblAjudaDesenho.ForeColor = Tema.Texto3;
		_lblAjudaDesenho.Font = Tema.Fonte75;
		_lblAjudaDesenho.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel.Controls.Add(_desenho, 0, 0);
		tableLayoutPanel.Controls.Add(_lblAjudaDesenho, 0, 1);
		conteudo.Controls.Add(tableLayoutPanel);
		return result;
	}

	private Control ConstruirDireita()
	{
		return new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			ColumnCount = 1,
			RowCount = 2,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f),
				new RowStyle(SizeType.Absolute, 128f)
			},
			Controls = 
			{
				{
					ConstruirMapa(),
					0,
					0
				},
				{
					ConstruirVivo(),
					0,
					1
				}
			}
		};
	}

	private Control ConstruirMapa()
	{
		Panel conteudo;
		Panel result = Tema.Cartao("Mapa do alvo escolhido", new Padding(0, 0, 0, 8), out conteudo);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 1,
			RowCount = 8,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Absolute, 32f),
				new RowStyle(SizeType.Absolute, 20f),
				new RowStyle(SizeType.Absolute, 10f),
				new RowStyle(SizeType.Absolute, 18f),
				new RowStyle(SizeType.Absolute, 84f),
				new RowStyle(SizeType.Absolute, 34f),
				new RowStyle(SizeType.Percent, 100f),
				new RowStyle(SizeType.Absolute, 62f)
			}
		};
		_lblSelecionado.Dock = DockStyle.Fill;
		_lblSelecionado.Font = Tema.Fonte12N;
		_lblSelecionado.ForeColor = Tema.Realce;
		_lblSelecionado.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel.Controls.Add(_lblSelecionado, 0, 0);
		_lblDirecao.Dock = DockStyle.Fill;
		_lblDirecao.Font = Tema.Fonte8;
		_lblDirecao.ForeColor = Tema.Texto2;
		_lblDirecao.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel.Controls.Add(_lblDirecao, 0, 1);
		Panel control = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Borda,
			Margin = new Padding(0, 4, 0, 5)
		};
		tableLayoutPanel.Controls.Add(control, 0, 2);
		Label label = Tema.Rotulo("Teclas deste controle (a ordem e a do arquivo)", fraco: true);
		label.Font = Tema.Fonte75;
		tableLayoutPanel.Controls.Add(label, 0, 3);
		_lstTeclas.Margin = new Padding(0, 0, 0, 4);
		tableLayoutPanel.Controls.Add(_lstTeclas, 0, 4);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 3,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 41f),
				new ColumnStyle(SizeType.Percent, 32f),
				new ColumnStyle(SizeType.Percent, 27f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			}
		};
		_btnCapturar.Margin = new Padding(0, 0, 5, 4);
		_btnTeclaExtra.Margin = new Padding(0, 0, 5, 4);
		_btnRemover.Margin = new Padding(0, 0, 0, 4);
		tableLayoutPanel2.Controls.Add(_btnCapturar, 0, 0);
		tableLayoutPanel2.Controls.Add(_btnTeclaExtra, 1, 0);
		tableLayoutPanel2.Controls.Add(_btnRemover, 2, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 5);
		tableLayoutPanel.Controls.Add(ConstruirLateral(), 0, 6);
		_lblDica.Dock = DockStyle.Fill;
		_lblDica.ForeColor = Tema.Texto3;
		_lblDica.Font = Tema.Fonte8;
		_lblDica.TextAlign = ContentAlignment.TopLeft;
		tableLayoutPanel.Controls.Add(_lblDica, 0, 7);
		conteudo.Controls.Add(tableLayoutPanel);
		return result;
	}

	private Control ConstruirLateral()
	{
		TableLayoutPanel obj = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 1,
			RowCount = 2,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			}
		};
		_linhaDoEixo = new RowStyle(SizeType.Absolute, 0f);
		obj.RowStyles.Add(_linhaDoEixo);
		obj.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		_painelDoEixo.Margin = new Padding(0);
		obj.Controls.Add(ConstruirPainelDoEixo(), 0, 0);
		_lblResumo.Dock = DockStyle.Fill;
		_lblResumo.ForeColor = Tema.Texto2;
		_lblResumo.Font = Tema.Fonte8;
		_lblResumo.TextAlign = ContentAlignment.TopLeft;
		_lblResumo.Margin = new Padding(0, 8, 0, 0);
		obj.Controls.Add(_lblResumo, 0, 1);
		return obj;
	}

	private Control ConstruirPainelDoEixo()
	{
		_painelDoEixo.Dock = DockStyle.Fill;
		_painelDoEixo.BackColor = Tema.Painel;
		_painelDoEixo.Margin = new Padding(0);
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
		tableLayoutPanel.Dock = DockStyle.Fill;
		tableLayoutPanel.BackColor = Tema.Painel;
		tableLayoutPanel.ColumnCount = 1;
		tableLayoutPanel.RowCount = 3;
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.Controls.Add(LinhaBarra("Sensibilidade - valor maximo do eixo", _barraSensibilidade, _numSensibilidade), 0, 0);
		_btnAvancado.Margin = new Padding(0, 2, 0, 2);
		tableLayoutPanel.Controls.Add(_btnAvancado, 0, 1);
		_painelAvancado.Dock = DockStyle.Fill;
		_painelAvancado.BackColor = Tema.Painel;
		_painelAvancado.Margin = new Padding(0);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel();
		tableLayoutPanel2.Dock = DockStyle.Fill;
		tableLayoutPanel2.BackColor = Tema.Painel;
		tableLayoutPanel2.ColumnCount = 1;
		tableLayoutPanel2.RowCount = 3;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
		tableLayoutPanel2.Controls.Add(LinhaBarra("Rampa de ida (ms) - 0 = resposta no mesmo tique", _barraRampaIda, _numRampaIda), 0, 0);
		tableLayoutPanel2.Controls.Add(LinhaBarra("Rampa de volta (ms)", _barraRampaVolta, _numRampaVolta), 0, 1);
		tableLayoutPanel2.Controls.Add(_chkMesmaVolta, 0, 2);
		_painelAvancado.Controls.Add(tableLayoutPanel2);
		_painelAvancado.Visible = false;
		tableLayoutPanel.Controls.Add(_painelAvancado, 0, 2);
		_painelDoEixo.Controls.Add(tableLayoutPanel);
		return _painelDoEixo;
	}

	private static Control LinhaBarra(string rotulo, BarraDeslizante barra, NumericUpDown numero)
	{
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			Margin = new Padding(0)
		};
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 1,
			RowCount = 2,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Absolute, 17f),
				new RowStyle(SizeType.Absolute, 27f)
			}
		};
		Label control = new Label
		{
			Text = rotulo,
			Dock = DockStyle.Fill,
			Font = Tema.Fonte75,
			ForeColor = Tema.Texto2,
			TextAlign = ContentAlignment.MiddleLeft
		};
		tableLayoutPanel.Controls.Add(control, 0, 0);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Painel,
			ColumnCount = 2,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f),
				new ColumnStyle(SizeType.Absolute, 78f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			}
		};
		barra.Dock = DockStyle.Fill;
		barra.Margin = new Padding(0, 2, 8, 2);
		numero.Margin = new Padding(0, 2, 0, 2);
		tableLayoutPanel2.Controls.Add(barra, 0, 0);
		tableLayoutPanel2.Controls.Add(numero, 1, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 1);
		obj.Controls.Add(tableLayoutPanel);
		return obj;
	}

	private Control ConstruirVivo()
	{
		Panel conteudo;
		Panel result = Tema.Cartao("Teste ao vivo (o que o Windows ve)", new Padding(0, 0, 0, 0), out conteudo);
		_lblVivo.Dock = DockStyle.Fill;
		_lblVivo.Font = Tema.FonteMono;
		_lblVivo.ForeColor = Tema.Texto2;
		_lblVivo.TextAlign = ContentAlignment.TopLeft;
		_lblVivo.Text = "esperando o primeiro tique...";
		conteudo.Controls.Add(_lblVivo);
		return result;
	}

	private Control ConstruirRodape()
	{
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			Padding = new Padding(12, 6, 12, 8)
		};
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			ColumnCount = 1,
			RowCount = 3,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Percent, 100f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Absolute, 36f),
				new RowStyle(SizeType.Absolute, 26f),
				new RowStyle(SizeType.Absolute, 20f)
			}
		};
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			BackColor = Tema.Fundo,
			ColumnCount = 7,
			RowCount = 1,
			ColumnStyles = 
			{
				new ColumnStyle(SizeType.Absolute, 84f),
				new ColumnStyle(SizeType.Absolute, 108f),
				new ColumnStyle(SizeType.Absolute, 92f),
				new ColumnStyle(SizeType.Absolute, 18f),
				new ColumnStyle(SizeType.Absolute, 190f),
				new ColumnStyle(SizeType.Percent, 100f),
				new ColumnStyle(SizeType.Absolute, 236f)
			},
			RowStyles = 
			{
				new RowStyle(SizeType.Percent, 100f)
			}
		};
		_btnAbrir.Margin = new Padding(0, 3, 5, 3);
		_btnRecarregar.Margin = new Padding(0, 3, 5, 3);
		_btnSalvar.Margin = new Padding(0, 3, 5, 3);
		_btnLigarPad.Margin = new Padding(0, 3, 8, 3);
		_btnLigarPad.Font = Tema.Fonte8N;
		tableLayoutPanel2.Controls.Add(_btnAbrir, 0, 0);
		tableLayoutPanel2.Controls.Add(_btnRecarregar, 1, 0);
		tableLayoutPanel2.Controls.Add(_btnSalvar, 2, 0);
		tableLayoutPanel2.Controls.Add(new Panel
		{
			BackColor = Tema.Fundo,
			Dock = DockStyle.Fill,
			Margin = new Padding(0)
		}, 3, 0);
		tableLayoutPanel2.Controls.Add(_btnLigarPad, 4, 0);
		_lblEstadoPad.Dock = DockStyle.Fill;
		_lblEstadoPad.ForeColor = Tema.Texto2;
		_lblEstadoPad.Font = Tema.Fonte8;
		_lblEstadoPad.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel2.Controls.Add(_lblEstadoPad, 5, 0);
		tableLayoutPanel2.Controls.Add(_chkReiniciar, 6, 0);
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 0);
		_lblStatus.Dock = DockStyle.Fill;
		_lblStatus.ForeColor = Tema.Texto2;
		_lblStatus.Font = Tema.Fonte8;
		_lblStatus.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel.Controls.Add(_lblStatus, 0, 1);
		_lblArquivo.Dock = DockStyle.Fill;
		_lblArquivo.ForeColor = Tema.Texto3;
		_lblArquivo.Font = Tema.Fonte75;
		_lblArquivo.TextAlign = ContentAlignment.MiddleLeft;
		tableLayoutPanel.Controls.Add(_lblArquivo, 0, 2);
		obj.Controls.Add(tableLayoutPanel);
		return obj;
	}

	private void LigarEventos()
	{
		_lstPerfis.DrawItem += DesenharPerfil;
		_lstPerfis.SelectedIndexChanged += delegate
		{
			TrocarDePerfil();
		};
		_desenho.Selecionou += delegate
		{
			MostrarAlvo();
		};
		_lstTeclas.DrawItem += DesenharTecla;
		_lstTeclas.SelectedIndexChanged += delegate
		{
			MostrarParametros();
		};
		_btnCapturar.Click += delegate
		{
			CapturarTecla(nova: false);
		};
		_btnTeclaExtra.Click += delegate
		{
			CapturarTecla(nova: true);
		};
		_btnRemover.Click += RemoverTecla;
		_btnAvancado.Click += delegate
		{
			_painelAvancado.Visible = !_painelAvancado.Visible;
			if (_linhaDoEixo != null)
			{
				_linhaDoEixo.Height = AlturaDoPainelDoEixo();
			}
			_btnAvancado.Text = (_painelAvancado.Visible ? "Avancado  ▲" : "Avancado  ▼");
		};
		_btnNovoPerfil.Click += delegate
		{
			CriarPerfil();
		};
		_btnDuplicar.Click += delegate
		{
			DuplicarPerfil();
		};
		_btnRenomear.Click += delegate
		{
			RenomearPerfil();
		};
		_btnExcluir.Click += delegate
		{
			ExcluirPerfil();
		};
		_btnAjustes.Click += delegate
		{
			AbrirAjustes();
		};
		_btnAtalhos.Click += delegate
		{
			AbrirAtalhos();
		};
		_btnAbrir.Click += delegate
		{
			EscolherArquivo();
		};
		_btnRecarregar.Click += delegate
		{
			RecarregarDoDisco();
		};
		_btnSalvar.Click += delegate
		{
			Salvar();
		};
		_btnLigarPad.Click += delegate
		{
			AlternarPad();
		};
		_chkInicial.CheckedChanged += delegate
		{
			if (!_carregando && _arquivo != null)
			{
				string text = NomeDoPerfilSelecionado();
				if (text != null)
				{
					_arquivo.PerfilInicial = text;
					_sujo = true;
					_lstPerfis.Invalidate();
					MarcarTitulo();
					_lblStatus.Text = "o pad vai comecar pelo perfil '" + text + "'.";
				}
			}
		};
		_chkMesmaVolta.CheckedChanged += delegate
		{
			_barraRampaVolta.Enabled = !_chkMesmaVolta.Checked;
			_numRampaVolta.Enabled = !_chkMesmaVolta.Checked;
			if (!_carregando)
			{
				GravarParametros();
			}
		};
		LigarBarra(_barraSensibilidade, _numSensibilidade, delegate
		{
			GravarParametros();
		});
		LigarBarra(_barraRampaIda, _numRampaIda, delegate
		{
			GravarParametros();
		});
		LigarBarra(_barraRampaVolta, _numRampaVolta, delegate
		{
			GravarParametros();
		});
		base.FormClosing += AoFechar;
	}

	private void LigarBarra(BarraDeslizante barra, NumericUpDown numero, EventHandler aoMexer)
	{
		barra.ValorMudou += delegate
		{
			if (!_sincronizando)
			{
				_sincronizando = true;
				try
				{
					numero.Value = Limitar(barra.Valor, (int)numero.Minimum, (int)numero.Maximum);
				}
				finally
				{
					_sincronizando = false;
				}
				aoMexer(null, EventArgs.Empty);
			}
		};
		numero.ValueChanged += delegate
		{
			if (!_sincronizando)
			{
				_sincronizando = true;
				try
				{
					barra.Valor = (int)numero.Value;
				}
				finally
				{
					_sincronizando = false;
				}
				aoMexer(null, EventArgs.Empty);
			}
		};
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

	private string NomeDoPerfilSelecionado()
	{
		if (_lstPerfis.SelectedIndex < 0 || _lstPerfis.SelectedIndex >= _lstPerfis.Items.Count)
		{
			return null;
		}
		return (string)_lstPerfis.Items[_lstPerfis.SelectedIndex];
	}

	private Perfil PerfilAtual()
	{
		if (_arquivo == null)
		{
			return null;
		}
		string perfilNaTela = _perfilNaTela;
		if (perfilNaTela == null)
		{
			return null;
		}
		if (!_arquivo.Perfis.TryGetValue(perfilNaTela, out var value))
		{
			return null;
		}
		return value;
	}

	private DesenhoControle.Alvo AlvoSelecionado()
	{
		foreach (DesenhoControle.Alvo alvo in _desenho.Alvos)
		{
			if (string.Equals(alvo.Chave, _desenho.Selecionado, StringComparison.Ordinal))
			{
				return alvo;
			}
		}
		return null;
	}

	private static bool CasaComAlvo(Mapeamento m, DesenhoControle.Alvo a)
	{
		if (m == null || a == null)
		{
			return false;
		}
		if (a.Sentido == 0)
		{
			if (m.Tipo == TipoMapeamento.Botao)
			{
				return string.Equals(m.AlvoBotao.ToString(), a.Nome, StringComparison.Ordinal);
			}
			return false;
		}
		if (m.Tipo == TipoMapeamento.Eixo && string.Equals(m.AlvoEixo.ToString(), a.Nome, StringComparison.Ordinal))
		{
			return m.Sentido == a.Sentido;
		}
		return false;
	}

	private static string ChaveDoMapa(Mapeamento m)
	{
		if (m == null)
		{
			return null;
		}
		if (m.Tipo == TipoMapeamento.Botao)
		{
			return m.AlvoBotao.ToString();
		}
		return m.AlvoEixo.ToString() + ((m.Sentido > 0) ? "+" : "-");
	}

	private void AtualizarDesenho()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		Perfil perfil = PerfilAtual();
		if (perfil != null)
		{
			foreach (Mapeamento mapeamento in perfil.Mapeamentos)
			{
				string text = ChaveDoMapa(mapeamento);
				if (text != null)
				{
					string text2 = GravadorPerfil.NomeDeTecla(mapeamento) ?? mapeamento.TeclaNome ?? "?";
					dictionary[text] = (dictionary.TryGetValue(text, out var value) ? (value + " / " + text2) : text2);
				}
			}
		}
		_desenho.DefinirTeclas(dictionary);
	}

	private void TrocarDePerfil()
	{
		if (!_carregando)
		{
			MostrarPerfilNaTela();
			string text = NomeDoPerfilSelecionado();
			if (text != null)
			{
				_lblStatus.Text = "perfil '" + text + "' na tela. O que voce mexer aqui escreve direto no arquivo (use Salvar).";
			}
		}
	}

	private void MostrarPerfilNaTela()
	{
		if (_arquivo != null)
		{
			_carregando = true;
			try
			{
				_perfilNaTela = NomeDoPerfilSelecionado();
				string perfilNaTela = _perfilNaTela;
				_chkInicial.Checked = perfilNaTela != null && string.Equals(_arquivo.PerfilInicial, perfilNaTela, StringComparison.OrdinalIgnoreCase);
				_btnExcluir.Enabled = _arquivo.Perfis.Count > 1;
			}
			finally
			{
				_carregando = false;
			}
			_lstPerfis.Invalidate();
			AtualizarDesenho();
			MostrarAlvo();
			MarcarTitulo();
			AtualizarEstadoDoPad();
		}
	}

	private Mapeamento MapaSelecionado()
	{
		int selectedIndex = _lstTeclas.SelectedIndex;
		if (selectedIndex < 0 || selectedIndex >= _mapasNaLista.Count)
		{
			return null;
		}
		return _mapasNaLista[selectedIndex];
	}

	private float AlturaDoPainelDoEixo()
	{
		if (!_painelAvancado.Visible)
		{
			return 80f;
		}
		return 198f;
	}

	private void MostrarAlvo()
	{
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		_carregando = true;
		try
		{
			_mapasNaLista.Clear();
			_lstTeclas.Items.Clear();
			if (alvo == null)
			{
				_lblSelecionado.Text = "(escolha um alvo no controle)";
				_lblDirecao.Text = "";
				_painelDoEixo.Visible = false;
				_btnCapturar.Enabled = false;
				_btnTeclaExtra.Enabled = false;
				_btnRemover.Enabled = false;
				return;
			}
			_lblSelecionado.Text = alvo.NomePorExtenso;
			bool flag = alvo.Sentido != 0;
			if (!flag)
			{
				_lblDirecao.Text = "Botao: a tecla apertada vira este botao; ao soltar, ele solta.";
			}
			else if (alvo.Nome == "LT" || alvo.Nome == "RT")
			{
				_lblDirecao.Text = "Gatilho: a tecla empurra de 0 ate o valor maximo (0 a 255).";
			}
			else
			{
				_lblDirecao.Text = "Analogico: a tecla empurra o eixo " + ((alvo.Sentido > 0) ? "para cima/direita" : "para baixo/esquerda") + ".";
			}
			Perfil perfil = PerfilAtual();
			if (perfil != null)
			{
				foreach (Mapeamento mapeamento in perfil.Mapeamentos)
				{
					if (CasaComAlvo(mapeamento, alvo))
					{
						_mapasNaLista.Add(mapeamento);
						_lstTeclas.Items.Add(GravadorPerfil.NomeDeTecla(mapeamento) ?? mapeamento.TeclaNome ?? "?");
					}
				}
			}
			_painelDoEixo.Visible = flag;
			if (_linhaDoEixo != null)
			{
				_linhaDoEixo.Height = (flag ? AlturaDoPainelDoEixo() : 0f);
			}
			_btnCapturar.Enabled = true;
			_btnTeclaExtra.Enabled = true;
			_btnRemover.Enabled = _mapasNaLista.Count > 0;
			if (_lstTeclas.Items.Count > 0)
			{
				_lstTeclas.SelectedIndex = 0;
			}
		}
		finally
		{
			_carregando = false;
		}
		MostrarParametros();
		EscreverDica();
		EscreverResumo();
	}

	private void EscreverResumo()
	{
		Perfil perfil = PerfilAtual();
		if (perfil == null)
		{
			_lblResumo.Text = "";
			return;
		}
		List<string> list = new List<string>();
		foreach (DesenhoControle.Alvo alvo in _desenho.Alvos)
		{
			bool flag = false;
			foreach (Mapeamento mapeamento in perfil.Mapeamentos)
			{
				if (CasaComAlvo(mapeamento, alvo))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(alvo.Rotulo);
			}
		}
		if (list.Count == 0)
		{
			_lblResumo.Text = "Perfil completo: os 24 alvos do controle tem tecla.";
		}
		else
		{
			_lblResumo.Text = "SEM TECLA NESTE PERFIL (" + list.Count + " de 24):\r\n" + string.Join(", ", list.ToArray());
		}
	}

	private void MostrarParametros()
	{
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		Mapeamento mapeamento = MapaSelecionado();
		int num = 1;
		int num2 = 32767;
		if (alvo != null && alvo.Sentido != 0)
		{
			Enum.TryParse<Eixo>(alvo.Nome, ignoreCase: true, out var result);
			num2 = (Faixas.EhGatilho(result) ? 255 : 32767);
		}
		int v = mapeamento?.ValorMaximo ?? ((alvo != null && alvo.Sentido != 0) ? ValorMaximoPadraoDe(alvo) : num2);
		int num3 = mapeamento?.RampaMs ?? 150;
		int v2 = ((mapeamento != null && mapeamento.RampaVoltaMs >= 0) ? mapeamento.RampaVoltaMs : num3);
		bool flag = mapeamento == null || mapeamento.RampaVoltaMs < 0;
		bool flag2 = mapeamento != null;
		_sincronizando = true;
		try
		{
			_barraSensibilidade.Faixa(num, num2, Limitar(v, num, num2));
			_numSensibilidade.Minimum = num;
			_numSensibilidade.Maximum = num2;
			_numSensibilidade.Increment = ((num2 <= 255) ? 1 : 64);
			_numSensibilidade.Value = Limitar(v, num, num2);
			_barraRampaIda.Faixa(0, 2000, Limitar(num3, 0, 2000));
			_numRampaIda.Value = Limitar(num3, 0, 2000);
			_barraRampaVolta.Faixa(0, 2000, Limitar(v2, 0, 2000));
			_numRampaVolta.Value = Limitar(v2, 0, 2000);
			_chkMesmaVolta.Checked = flag;
			_barraSensibilidade.Enabled = flag2;
			_numSensibilidade.Enabled = flag2;
			_barraRampaIda.Enabled = flag2;
			_numRampaIda.Enabled = flag2;
			_barraRampaVolta.Enabled = flag2 && !flag;
			_numRampaVolta.Enabled = flag2 && !flag;
			_chkMesmaVolta.Enabled = flag2;
			_btnAvancado.Enabled = flag2;
		}
		finally
		{
			_sincronizando = false;
		}
	}

	private static int ValorMaximoPadraoDe(DesenhoControle.Alvo a)
	{
		if (a != null && Enum.TryParse<Eixo>(a.Nome, ignoreCase: true, out var result))
		{
			return Faixas.ValorMaximoPadrao(result);
		}
		return 32767;
	}

	private void GravarParametros()
	{
		if (_carregando || _sincronizando)
		{
			return;
		}
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		Mapeamento mapeamento = MapaSelecionado();
		if (alvo != null && mapeamento != null && mapeamento.Tipo == TipoMapeamento.Eixo)
		{
			int num = (int)_numSensibilidade.Value;
			int num2 = (int)_numRampaIda.Value;
			int num3 = (_chkMesmaVolta.Checked ? (-1) : ((int)_numRampaVolta.Value));
			if (num != mapeamento.ValorMaximo || num2 != mapeamento.RampaMs || num3 != mapeamento.RampaVoltaMs)
			{
				mapeamento.ValorMaximo = num;
				mapeamento.RampaMs = num2;
				mapeamento.RampaVoltaMs = num3;
				MarcarSujo();
				EscreverDica();
			}
		}
	}

	private void EscreverDica()
	{
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		if (alvo == null || PerfilAtual() == null)
		{
			_lblDica.Text = "";
			return;
		}
		if (_mapasNaLista.Count == 0)
		{
			_lblDica.Text = "Este alvo (" + alvo.NomePorExtenso + ") ainda nao tem tecla.\r\nClique em 'Capturar tecla' e aperte a tecla que voce quer. Alvo sem tecla nao e gravado no arquivo.";
			return;
		}
		string text = "";
		foreach (Mapeamento mapasNaListum in _mapasNaLista)
		{
			string text2 = GravadorPerfil.NomeDeTecla(mapasNaListum) ?? mapasNaListum.TeclaNome ?? "?";
			text = text + ((text.Length == 0) ? "" : " ou ") + text2;
		}
		string text3;
		if (alvo.Sentido == 0)
		{
			text3 = "Segurar " + text + " mantem o botao " + alvo.Nome + " apertado; ao soltar, ele solta.";
		}
		else
		{
			Mapeamento mapeamento = _mapasNaLista[0];
			string text4 = ((mapeamento.RampaVoltaMs < 0) ? ("a mesma rampa de ida (" + mapeamento.RampaMs + " ms)") : (mapeamento.RampaVoltaMs + " ms"));
			text3 = "Segurar " + text + " empurra " + alvo.Rotulo + " ate " + mapeamento.ValorMaximo + " em " + mapeamento.RampaMs + " ms; ao soltar, volta em " + text4 + ".";
		}
		if (_mapasNaLista.Count > 1)
		{
			text3 += "   Todas as teclas da lista fazem a mesma coisa.";
		}
		_lblDica.Text = text3;
	}

	private void DesenharPerfil(object remetente, DrawItemEventArgs e)
	{
		if (e.Index < 0 || _arquivo == null)
		{
			return;
		}
		string text = (string)_lstPerfis.Items[e.Index];
		bool flag = (e.State & DrawItemState.Selected) != 0;
		using (SolidBrush brush = new SolidBrush(flag ? Tema.RealceEscuro : Tema.Painel))
		{
			e.Graphics.FillRectangle(brush, e.Bounds);
		}
		if (flag)
		{
			using SolidBrush brush2 = new SolidBrush(Tema.Realce);
			e.Graphics.FillRectangle(brush2, e.Bounds.Left, e.Bounds.Top + 6, 3, e.Bounds.Height - 12);
		}
		if (!_arquivo.Perfis.TryGetValue(text, out var value))
		{
			value = null;
		}
		using (SolidBrush brush3 = new SolidBrush(Tema.Texto))
		{
			e.Graphics.DrawString(text, Tema.Fonte10N, brush3, new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 5, e.Bounds.Width - 34, 18));
		}
		string text2 = "";
		if (value != null)
		{
			int count = value.Mapeamentos.Count;
			text2 = count + ((count == 1) ? " tecla" : " teclas");
			if (value.ZonaMorta > 0)
			{
				text2 = text2 + "   zona morta " + value.ZonaMorta;
			}
			if (!string.IsNullOrWhiteSpace(value.Descricao))
			{
				text2 = value.Descricao + "   -   " + text2;
			}
		}
		using (SolidBrush brush4 = new SolidBrush(Tema.Texto2))
		{
			e.Graphics.DrawString(text2, Tema.Fonte75, brush4, new Rectangle(e.Bounds.Left + 12, e.Bounds.Top + 23, e.Bounds.Width - 20, 16));
		}
		if (!string.Equals(_arquivo.PerfilInicial, text, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		using SolidBrush brush5 = new SolidBrush(Tema.Amarelo);
		using StringFormat format = new StringFormat
		{
			Alignment = StringAlignment.Far
		};
		e.Graphics.DrawString("inicial", Tema.Fonte75, brush5, new Rectangle(e.Bounds.Right - 56, e.Bounds.Top + 6, 48, 16), format);
	}

	private void DesenharTecla(object remetente, DrawItemEventArgs e)
	{
		if (e.Index < 0 || e.Index >= _lstTeclas.Items.Count)
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
		e.Graphics.DrawString((string)_lstTeclas.Items[e.Index], Tema.Fonte8N, brush2, new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height), format);
	}

	private void MarcarSujo()
	{
		if (!_carregando)
		{
			_sujo = true;
			MarcarTitulo();
		}
	}

	private void MarcarTitulo()
	{
		string text = ((_caminho != null) ? Path.GetFileName(_caminho) : "(arquivo novo)");
		Text = "KeyToJoyEmulator - configurador do controle virtual" + (_sujo ? " *" : "") + "   [" + text + "]";
	}

	private void CapturarTecla(bool nova)
	{
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		Perfil perfil = PerfilAtual();
		if (alvo == null || perfil == null)
		{
			return;
		}
		string nomeCapturado;
		using (CapturaDeTecla capturaDeTecla = new CapturaDeTecla(combinacao: false))
		{
			if (capturaDeTecla.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}
			nomeCapturado = capturaDeTecla.NomeCapturado;
		}
		if (string.IsNullOrWhiteSpace(nomeCapturado))
		{
			return;
		}
		if (!TabelaTeclas.TentarResolver(nomeCapturado, out var tecla))
		{
			_lblStatus.Text = "nao entendi a tecla '" + nomeCapturado + "'.";
			return;
		}
		Mapeamento mapeamento = (nova ? null : MapaSelecionado());
		if (mapeamento == null)
		{
			mapeamento = new Mapeamento();
			mapeamento.Sentido = 1;
			mapeamento.RampaMs = 150;
			mapeamento.RampaVoltaMs = -1;
			if (alvo.Sentido == 0)
			{
				if (!Enum.TryParse<Botao>(alvo.Nome, ignoreCase: true, out var result))
				{
					_lblStatus.Text = "alvo de botao invalido: " + alvo.Nome;
					return;
				}
				mapeamento.Tipo = TipoMapeamento.Botao;
				mapeamento.AlvoBotao = result;
				mapeamento.ValorMaximo = Faixas.ValorMaximoPadrao(Eixo.LX);
			}
			else
			{
				if (!Enum.TryParse<Eixo>(alvo.Nome, ignoreCase: true, out var result2))
				{
					_lblStatus.Text = "alvo de eixo invalido: " + alvo.Nome;
					return;
				}
				mapeamento.Tipo = TipoMapeamento.Eixo;
				mapeamento.AlvoEixo = result2;
				mapeamento.Sentido = alvo.Sentido;
				mapeamento.ValorMaximo = Faixas.ValorMaximoPadrao(result2);
			}
			perfil.Mapeamentos.Add(mapeamento);
		}
		mapeamento.TeclaNome = TabelaTeclas.Canonico(nomeCapturado) ?? nomeCapturado;
		mapeamento.Tecla = tecla;
		string b = ChaveDoMapa(mapeamento);
		string text = GravadorPerfil.NomeDeTecla(mapeamento);
		for (int num = perfil.Mapeamentos.Count - 1; num >= 0; num--)
		{
			Mapeamento mapeamento2 = perfil.Mapeamentos[num];
			if (mapeamento2 != mapeamento && string.Equals(ChaveDoMapa(mapeamento2), b, StringComparison.Ordinal) && string.Equals(GravadorPerfil.NomeDeTecla(mapeamento2), text, StringComparison.OrdinalIgnoreCase))
			{
				perfil.Mapeamentos.RemoveAt(num);
			}
		}
		MarcarSujo();
		AtualizarDesenho();
		_lblStatus.Text = "a tecla '" + text + "' agora vale para " + alvo.NomePorExtenso.ToLowerInvariant() + ".";
		MostrarAlvo();
	}

	private void RemoverTecla(object remetente, EventArgs e)
	{
		DesenhoControle.Alvo alvo = AlvoSelecionado();
		Perfil perfil = PerfilAtual();
		Mapeamento mapeamento = MapaSelecionado();
		if (perfil != null && mapeamento != null)
		{
			string text = GravadorPerfil.NomeDeTecla(mapeamento) ?? "?";
			perfil.Mapeamentos.Remove(mapeamento);
			MarcarSujo();
			AtualizarDesenho();
			_lblStatus.Text = "tirei a tecla '" + text + "' do alvo " + ((alvo != null) ? alvo.NomePorExtenso.ToLowerInvariant() : "?") + ".";
			MostrarAlvo();
		}
	}

	private static string PerfilPadrao()
	{
		string text = Path.Combine(Caminhos.PastaDoPrograma(), "profiles", "corrida.json");
		if (File.Exists(text))
		{
			return text;
		}
		string text2 = Caminhos.PastaDoPrograma();
		for (int i = 0; i < 6; i++)
		{
			if (string.IsNullOrEmpty(text2))
			{
				break;
			}
			string text3 = Path.Combine(text2, "my-version", "profiles", "corrida.json");
			if (File.Exists(text3))
			{
				return text3;
			}
			text2 = Directory.GetParent(text2)?.FullName;
		}
		return text;
	}

	private void AbrirArquivo(string caminho)
	{
		if (string.IsNullOrWhiteSpace(caminho))
		{
			_arquivo = new ArquivoPerfis
			{
				Versao = 1,
				TiqueMs = 8,
				PerfilInicial = "padrao"
			};
			_arquivo.Perfis["padrao"] = new Perfil
			{
				Nome = "padrao"
			};
			_caminho = null;
			_sujo = false;
			RecarregarLista("padrao", "arquivo novo em memoria. Escolha um perfil, clique num botao do controle e aperte a tecla dele.");
			return;
		}
		if (!File.Exists(caminho))
		{
			_arquivo = new ArquivoPerfis
			{
				Versao = 1,
				TiqueMs = 8,
				PerfilInicial = "padrao"
			};
			_arquivo.Perfis["padrao"] = new Perfil
			{
				Nome = "padrao"
			};
			_caminho = null;
			_sujo = false;
			RecarregarLista("padrao", "nao encontrei o arquivo " + caminho + " - comecei um novo em memoria (use Abrir... para escolher outro).");
			return;
		}
		ArquivoPerfis arquivo;
		List<string> avisos;
		try
		{
			arquivo = CarregadorPerfil.Carregar(caminho, out avisos);
		}
		catch (PerfilInvalidoException ex)
		{
			_lblStatus.Text = "nao consegui abrir: " + ex.Message;
			MessageBox.Show(this, "O arquivo foi recusado pelo carregador:\r\n\r\n" + ex.Message, "abrir perfil", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return;
		}
		catch (Exception ex2)
		{
			_lblStatus.Text = "erro ao abrir: " + ex2.Message;
			return;
		}
		_arquivo = arquivo;
		_caminho = caminho;
		_sujo = false;
		string text = "arquivo carregado: " + caminho;
		if (avisos.Count > 0)
		{
			text = text + "   |   avisos: " + string.Join(" | ", avisos.ToArray());
		}
		RecarregarLista(null, text);
	}

	private void EscolherArquivo()
	{
		if (ConfirmarPerdaDeAlteracoes())
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Arquivo de perfis (*.json)|*.json|Todos os arquivos (*.*)|*.*";
			openFileDialog.Title = "Abrir arquivo de perfis";
			openFileDialog.InitialDirectory = ((_caminho != null) ? Path.GetDirectoryName(_caminho) : Caminhos.PastaDoPrograma());
			if (openFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				AbrirArquivo(openFileDialog.FileName);
			}
		}
	}

	private void RecarregarDoDisco()
	{
		if (_caminho == null)
		{
			AbrirArquivo(null);
		}
		else if (ConfirmarPerdaDeAlteracoes())
		{
			AbrirArquivo(_caminho);
		}
	}

	private bool ConfirmarPerdaDeAlteracoes()
	{
		if (!_sujo)
		{
			return true;
		}
		return MessageBox.Show(this, "Descartar as alteracoes nao salvas?", "KeyToJoyEmulator", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes;
	}

	private void RecarregarLista(string selecionar, string status)
	{
		_carregando = true;
		try
		{
			_lstPerfis.Items.Clear();
			foreach (KeyValuePair<string, Perfil> perfi in _arquivo.Perfis)
			{
				_lstPerfis.Items.Add(perfi.Key);
			}
			int selectedIndex = 0;
			string b = selecionar ?? _arquivo.PerfilInicial;
			for (int i = 0; i < _lstPerfis.Items.Count; i++)
			{
				if (string.Equals((string)_lstPerfis.Items[i], b, StringComparison.OrdinalIgnoreCase))
				{
					selectedIndex = i;
					break;
				}
			}
			if (_lstPerfis.Items.Count > 0)
			{
				_lstPerfis.SelectedIndex = selectedIndex;
			}
		}
		finally
		{
			_carregando = false;
		}
		_perfilNaTela = null;
		_desenho.Selecionado = "A";
		MostrarPerfilNaTela();
		_lblArquivo.Text = ((_caminho != null) ? ("arquivo: " + _caminho) : "arquivo: (novo, ainda nao salvo - use Salvar para escolher onde gravar)");
		_lblStatus.Text = status;
		AtualizarEstadoDoPad();
	}

	private string NomeUnico(string baseNome)
	{
		if (!_arquivo.Perfis.ContainsKey(baseNome))
		{
			return baseNome;
		}
		for (int i = 2; i < 1000; i++)
		{
			string text = baseNome + "-" + i;
			if (!_arquivo.Perfis.ContainsKey(text))
			{
				return text;
			}
		}
		return baseNome + "-" + DateTime.Now.Ticks;
	}

	private static Perfil CopiarPerfil(Perfil origem, string novoNome)
	{
		Perfil perfil = new Perfil
		{
			Nome = novoNome,
			Descricao = origem.Descricao,
			ZonaMorta = origem.ZonaMorta
		};
		foreach (Mapeamento mapeamento in origem.Mapeamentos)
		{
			perfil.Mapeamentos.Add(new Mapeamento
			{
				TeclaNome = mapeamento.TeclaNome,
				Tecla = mapeamento.Tecla,
				Tipo = mapeamento.Tipo,
				AlvoBotao = mapeamento.AlvoBotao,
				AlvoEixo = mapeamento.AlvoEixo,
				Sentido = mapeamento.Sentido,
				RampaMs = mapeamento.RampaMs,
				RampaVoltaMs = mapeamento.RampaVoltaMs,
				ValorMaximo = mapeamento.ValorMaximo
			});
		}
		foreach (Atalho atalho in origem.Atalhos)
		{
			perfil.Atalhos.Add(new Atalho
			{
				Combinacao = atalho.Combinacao,
				PerfilDestino = atalho.PerfilDestino,
				Teclas = new List<CodigoTecla>(atalho.Teclas)
			});
		}
		return perfil;
	}

	private void CriarPerfil()
	{
		if (_arquivo != null)
		{
			string text = NomeUnico("perfil-novo");
			_arquivo.Perfis[text] = new Perfil
			{
				Nome = text
			};
			if (string.IsNullOrWhiteSpace(_arquivo.PerfilInicial))
			{
				_arquivo.PerfilInicial = text;
			}
			MarcarSujo();
			RecarregarLista(text, "perfil '" + text + "' criado. Clique num botao do controle e aperte a tecla que voce quer.");
		}
	}

	private void DuplicarPerfil()
	{
		Perfil perfil = PerfilAtual();
		if (perfil != null)
		{
			string text = NomeUnico(perfil.Nome + "-copia");
			_arquivo.Perfis[text] = CopiarPerfil(perfil, text);
			MarcarSujo();
			RecarregarLista(text, "perfil '" + text + "' criado como copia de '" + perfil.Nome + "'.");
		}
	}

	private void RenomearPerfil()
	{
		Perfil perfil = PerfilAtual();
		string text = NomeDoPerfilSelecionado();
		if (perfil == null || text == null)
		{
			return;
		}
		string text2 = DialogoTexto.Perguntar(this, "Renomear perfil", "Novo nome do perfil:", text);
		if (text2 == null)
		{
			return;
		}
		text2 = text2.Trim();
		if (text2.Length == 0)
		{
			MessageBox.Show(this, "Nome vazio nao vale.", "renomear");
		}
		else
		{
			if (text2 == text)
			{
				return;
			}
			if (_arquivo.Perfis.ContainsKey(text2))
			{
				MessageBox.Show(this, "Ja existe um perfil chamado '" + text2 + "'.", "renomear");
				return;
			}
			Dictionary<string, Perfil> dictionary = new Dictionary<string, Perfil>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, Perfil> perfi in _arquivo.Perfis)
			{
				dictionary[(perfi.Key == text) ? text2 : perfi.Key] = perfi.Value;
			}
			_arquivo.Perfis = dictionary;
			perfil.Nome = text2;
			if (string.Equals(_arquivo.PerfilInicial, text, StringComparison.OrdinalIgnoreCase))
			{
				_arquivo.PerfilInicial = text2;
			}
			foreach (KeyValuePair<string, Perfil> perfi2 in _arquivo.Perfis)
			{
				foreach (Atalho atalho in perfi2.Value.Atalhos)
				{
					if (string.Equals(atalho.PerfilDestino, text, StringComparison.OrdinalIgnoreCase))
					{
						atalho.PerfilDestino = text2;
					}
				}
			}
			MarcarSujo();
			RecarregarLista(text2, "perfil renomeado de '" + text + "' para '" + text2 + "'.");
		}
	}

	private void ExcluirPerfil()
	{
		Perfil perfil = PerfilAtual();
		string text = NomeDoPerfilSelecionado();
		if (perfil == null || text == null)
		{
			return;
		}
		if (_arquivo.Perfis.Count <= 1)
		{
			MessageBox.Show(this, "O arquivo precisa de pelo menos um perfil.", "excluir");
		}
		else
		{
			if (MessageBox.Show(this, "Excluir o perfil '" + text + "'?", "excluir", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
			{
				return;
			}
			int num = 0;
			foreach (KeyValuePair<string, Perfil> perfi in _arquivo.Perfis)
			{
				for (int num2 = perfi.Value.Atalhos.Count - 1; num2 >= 0; num2--)
				{
					if (string.Equals(perfi.Value.Atalhos[num2].PerfilDestino, text, StringComparison.OrdinalIgnoreCase))
					{
						perfi.Value.Atalhos.RemoveAt(num2);
						num++;
					}
				}
			}
			_arquivo.Perfis.Remove(text);
			if (string.Equals(_arquivo.PerfilInicial, text, StringComparison.OrdinalIgnoreCase))
			{
				using Dictionary<string, Perfil>.Enumerator enumerator = _arquivo.Perfis.GetEnumerator();
				if (enumerator.MoveNext())
				{
					KeyValuePair<string, Perfil> current2 = enumerator.Current;
					_arquivo.PerfilInicial = current2.Key;
				}
			}
			MarcarSujo();
			string text2 = "perfil '" + text + "' excluido";
			if (num > 0)
			{
				text2 = text2 + " (e " + num + " atalho(s) que apontavam para ele)";
			}
			RecarregarLista(null, text2 + ".");
		}
	}

	private bool Validar(out string erro)
	{
		erro = null;
		if (_arquivo == null)
		{
			erro = "nenhum arquivo aberto.";
			return false;
		}
		if (_arquivo.Perfis.Count == 0)
		{
			erro = "o arquivo precisa de pelo menos um perfil.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(_arquivo.PerfilInicial) || !_arquivo.Perfis.ContainsKey(_arquivo.PerfilInicial))
		{
			erro = "escolha qual perfil o pad usa quando inicia: marque 'usar este perfil quando o pad iniciar'.";
			return false;
		}
		foreach (KeyValuePair<string, Perfil> perfi in _arquivo.Perfis)
		{
			foreach (Mapeamento mapeamento in perfi.Value.Mapeamentos)
			{
				if (string.IsNullOrWhiteSpace(GravadorPerfil.NomeDeTecla(mapeamento)))
				{
					erro = "o perfil '" + perfi.Key + "' tem um alvo sem tecla.";
					return false;
				}
			}
		}
		return true;
	}

	private bool Salvar()
	{
		if (_arquivo == null || _carregando)
		{
			return false;
		}
		if (!Validar(out var erro))
		{
			_lblStatus.Text = "nao salvei: " + erro;
			MessageBox.Show(this, erro, "nao salvei", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return false;
		}
		string text = _caminho;
		if (string.IsNullOrWhiteSpace(text))
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Arquivo de perfis (*.json)|*.json";
			saveFileDialog.FileName = "perfis.json";
			saveFileDialog.InitialDirectory = Caminhos.PastaDoPrograma();
			if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
			{
				return false;
			}
			text = saveFileDialog.FileName;
		}
		if (!GravadorPerfil.SalvarComValidacao(_arquivo, text, out erro, out var avisos))
		{
			_lblStatus.Text = "nao salvei: " + erro;
			MessageBox.Show(this, erro, "nao salvei", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			return false;
		}
		_caminho = text;
		_sujo = false;
		MarcarTitulo();
		_lblArquivo.Text = "arquivo: " + text;
		string text2 = "salvo em " + text + " (a versao anterior ficou em " + Path.GetFileName(text) + ".bak)";
		if (avisos.Count > 0)
		{
			text2 = text2 + "   |   avisos: " + string.Join(" | ", avisos.ToArray());
		}
		_lblStatus.Text = text2;
		if (_chkReiniciar.Checked && ProcessoPad.QuantosRodando() > 0)
		{
			if (!_pad.Parar(out var erro2))
			{
				_lblStatus.Text = text2 + "   |   nao consegui parar o pad: " + erro2;
			}
			else if (LigarPad(out erro2))
			{
				_lblStatus.Text = text2 + "   |   pad reiniciado com o arquivo novo.";
			}
			else
			{
				_lblStatus.Text = text2 + "   |   o pad nao voltou: " + erro2;
			}
		}
		AtualizarEstadoDoPad();
		return true;
	}

	private void AbrirAjustes()
	{
		Perfil perfil = PerfilAtual();
		if (_arquivo != null && perfil != null && JanelaAjustes.Perguntar(this, _arquivo, perfil))
		{
			MarcarSujo();
			_lstPerfis.Invalidate();
			MostrarPerfilNaTela();
			_lblStatus.Text = "ajustes do perfil '" + perfil.Nome + "' aplicados (zona morta " + perfil.ZonaMorta + " | intervalo do pad " + _arquivo.TiqueMs + " ms).";
		}
	}

	private void AbrirAtalhos()
	{
		Perfil perfil = PerfilAtual();
		if (_arquivo != null && perfil != null && JanelaAtalhos.Perguntar(this, _arquivo, perfil))
		{
			MarcarSujo();
			_lstPerfis.Invalidate();
			_lblStatus.Text = "atalhos do perfil '" + perfil.Nome + "': " + perfil.Atalhos.Count + " combinacao(oes).";
		}
	}

	private void MostrarComoUsar()
	{
		MessageBox.Show(this, "1) Escolha o perfil na lista da esquerda (ou crie um novo).\r\n2) Clique no botao do controle que voce quer configurar.\r\n3) Clique em 'Capturar tecla' e aperte a tecla que voce quer usar.\r\n4) Clique em 'Salvar'.\r\n5) Clique em 'Ligar o pad' para o jogo enxergar o controle virtual.\r\n\r\nAs teclas aparecem escritas nas caixinhas ligadas ao desenho. Alvo sem tecla fica apagado e nao vai para o arquivo.\r\nPara analogicos e gatilhos, o painel da direita tem a sensibilidade e, em 'Avancado', as rampas.\r\nSegurar a tecla mantem o botao apertado; soltar, solta. Teclas extras (duas teclas no mesmo alvo) valem igual.", "como usar", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}

	private void MostrarSobre()
	{
		MessageBox.Show(this, "KeyToJoyEmulator - teclado vira controle de Xbox 360 para o jogo.\r\n\r\nEste configurador escreve o arquivo de perfis; ele NAO cria o controle virtual.\r\nO arquivo e revalidado pelo proprio programa antes de trocar o seu (a versao anterior fica em .bak).\r\n\r\nA captura de tecla usa o mesmo gancho de teclado que o programa usa em jogo.", "sobre", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}

	private bool EscolherProgramaDoPad()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "KeyToJoyEmulator.exe|KeyToJoyEmulator.exe|Executaveis (*.exe)|*.exe";
		openFileDialog.Title = "Onde esta o KeyToJoyEmulator.exe";
		openFileDialog.FileName = "KeyToJoyEmulator.exe";
		string text = (string.IsNullOrEmpty(_caminhoDoPadExe) ? null : Path.GetDirectoryName(_caminhoDoPadExe));
		if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
		{
			openFileDialog.InitialDirectory = text;
		}
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return false;
		}
		_caminhoDoPadExe = openFileDialog.FileName;
		AtualizarEstadoDoPad();
		return true;
	}

	private void AlternarPad()
	{
		if (ProcessoPad.QuantosRodando() > 0)
		{
			PararPadPeloBotao();
			return;
		}
		if (!File.Exists(_caminhoDoPadExe))
		{
			string text = ProcessoPad.ProcurarExecutavel();
			if (text != null && File.Exists(text))
			{
				_caminhoDoPadExe = text;
			}
			else if (!EscolherProgramaDoPad())
			{
				_lblStatus.Text = "nao liguei o pad: preciso saber onde esta o KeyToJoyEmulator.exe.";
				AtualizarEstadoDoPad();
				return;
			}
		}
		IniciarPadPeloBotao();
	}

	private void IniciarPadPeloBotao()
	{
		if (LigarPad(out var erro))
		{
			_lblStatus.Text = "pad ligado. O controle virtual leva de 0,4 a 2,4 s para aparecer (veja o 'Teste ao vivo').";
		}
		else
		{
			_lblStatus.Text = "nao liguei o pad: " + erro;
			MessageBox.Show(this, erro, "ligar o pad", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		AtualizarEstadoDoPad();
	}

	private bool LigarPad(out string erro)
	{
		erro = null;
		if (_caminho == null || !File.Exists(_caminho))
		{
			erro = "salve o arquivo de perfis antes: o pad le os perfis do disco quando inicia.";
			return false;
		}
		if (_sujo)
		{
			if (MessageBox.Show(this, "Tem alteracoes nao salvas. Salvar antes de iniciar o pad?", "iniciar pad", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				erro = "ha alteracoes nao salvas (salve para o pad pegar o perfil novo).";
				return false;
			}
			if (!Salvar())
			{
				erro = "nao consegui salvar o arquivo de perfis.";
				return false;
			}
		}
		string text = Path.Combine(Path.GetDirectoryName(_caminho), "KeyToJoyEmulator-interface.log");
		bool num = _pad.Iniciar(_caminhoDoPadExe.Trim(), _caminho, text, out erro);
		if (num)
		{
			_lblStatus.Text = "log do pad: " + text;
		}
		return num;
	}

	private void PararPadPeloBotao()
	{
		if (_pad.LigadoPorMim || ProcessoPad.QuantosRodando() <= 0 || MessageBox.Show(this, "Existe um KeyToJoyEmulator rodando que NAO foi ligado por esta janela. Parar ele mesmo assim?", "parar pad", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
		{
			if (_pad.Parar(out var erro))
			{
				_lblStatus.Text = "pad parado.";
			}
			else
			{
				_lblStatus.Text = "nao parei o pad: " + erro;
			}
			AtualizarEstadoDoPad();
		}
	}

	private void AtualizarEstadoDoPad()
	{
		int num = ProcessoPad.QuantosRodando();
		bool flag = num > 0;
		_btnLigarPad.Text = (flag ? "● Desligar o pad" : "● Ligar o pad");
		_btnLigarPad.BackColor = (flag ? Color.FromArgb(96, 40, 36) : Color.FromArgb(28, 88, 62));
		_btnLigarPad.ForeColor = (flag ? Color.FromArgb(255, 208, 208) : Color.FromArgb(198, 239, 206));
		_btnLigarPad.FlatAppearance.BorderColor = (flag ? Tema.Vermelho : Tema.Verde);
		_btnLigarPad.FlatAppearance.MouseOverBackColor = (flag ? Color.FromArgb(140, 56, 48) : Color.FromArgb(38, 118, 82));
		_dicas.SetToolTip(_btnLigarPad, (flag ? "Para o pad: o controle virtual sai do Windows." : "Liga o pad: cria o controle virtual com o arquivo salvo.") + "\r\nPrograma: " + _caminhoDoPadExe);
		if (flag)
		{
			_lblEstadoPad.Text = "pad LIGADO (" + num + " processo" + ((num > 1) ? "s" : "") + ")" + (_pad.LigadoPorMim ? " - ligado por esta janela" : " - ligado por fora desta janela");
		}
		else
		{
			_lblEstadoPad.Text = (File.Exists(_caminhoDoPadExe) ? "pad parado" : "pad parado - nao achei o KeyToJoyEmulator.exe; clique no botao para escolher o programa");
		}
	}

	private void TicVivo(object remetente, EventArgs e)
	{
		_contadorDoRelogio++;
		if (_contadorDoRelogio % 20 == 0)
		{
			AtualizarEstadoDoPad();
		}
		if (!LeitorControle.Ler(out var estado))
		{
			_desenho.SemLeitura();
			_lblVivo.Text = "nenhum controle XInput responde agora.\r\nLigue o pad no botao '● Ligar o pad' e espere alguns segundos.";
			return;
		}
		_desenho.AoVivo(estado.Botoes, estado.LX, estado.LY, estado.RX, estado.RY, estado.LT, estado.RT);
		_lblVivo.Text = "controle " + estado.Indice + "   pacote " + estado.Pacote + "   botoes 0x" + estado.Botoes.ToString("X4") + "\r\nLX " + estado.LX + "   LY " + estado.LY + "\r\nRX " + estado.RX + "   RY " + estado.RY + "\r\nLT " + estado.LT + "   RT " + estado.RT;
	}

	private void AoFechar(object remetente, FormClosingEventArgs e)
	{
		_tmrVivo.Stop();
		if (_sujo)
		{
			switch (MessageBox.Show(this, "Tem alteracoes nao salvas no arquivo de perfis. Salvar antes de sair?", "KeyToJoyEmulator", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
			{
			case DialogResult.Cancel:
				e.Cancel = true;
				_tmrVivo.Start();
				return;
			case DialogResult.Yes:
				if (!Salvar())
				{
					e.Cancel = true;
					_tmrVivo.Start();
					return;
				}
				break;
			}
		}
		if ((_pad.LigadoPorMim || ProcessoPad.QuantosRodando() > 0) && MessageBox.Show(this, "O pad (KeyToJoyEmulator) esta rodando. Quer parar ele agora?\r\n\r\nSim = para o pad e sai\r\nNao = deixa o pad rodando e sai", "KeyToJoyEmulator", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
		{
			_pad.Parar(out var _);
		}
	}
}
