// JanelaPrincipal.cs - a interface de configuracao de perfis e teclas.
//
// Regras de projeto (para nao criar um segundo conjunto de regras):
//  * quem le e quem escreve o arquivo de perfis e o NUCLEO (CarregadorPerfil / GravadorPerfil);
//    a interface so mexe no modelo e manda salvar, e o que ela salva e revalidado pelo proprio
//    carregador antes de trocar o arquivo (GravadorPerfil.SalvarComValidacao);
//  * a captura de tecla usa o MESMO gancho de teclado do programa (MyVersion.GanchoTeclado);
//  * a interface NAO cria controle virtual: ela comanda o KeyToJoyEmulator.exe como processo separado;
//  * o "teste ao vivo" pergunta ao Windows (XInput), nunca ao KeyToJoyEmulator.
//
// ORGANIZACAO DA TELA (redesenhada em 2026-10-18; pedido do usuario: "achei ela muito poluida",
// "o usuario deve fazer apenas a configuracao dos botoes", usando a imagem em anexo como base):
//   topo     = menu (Arquivo, Perfil, Ajustes, Ajuda)
//   esquerda = perfis do arquivo (escolher, criar, duplicar, renomear, excluir, marcar o inicial)
//   centro   = o controle de Xbox One desenhado, com a tecla de cada alvo numa caixinha presa por
//              uma linha de chamada (DesenhoControle.cs). Clicar no alvo escolhe o que editar.
//   direita  = "Mapa": as teclas do alvo escolhido e, sendo analogico/gatilho, a sensibilidade
//              (valor maximo) e, no "Avancado", as rampas de ida e de volta.
//   rodape   = abrir/recarregar/salvar, o botao unico que liga e desliga o pad, e a linha de recado.
//
// O MODELO E A FONTE DA VERDADE: cada mexida na tela escreve direto no modelo (nao existe grade
// intermediaria), entao trocar de perfil ou salvar nao precisa "recolher" nada - e nao ha como a tela
// e o arquivo sairem diferentes.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyVersion;

namespace Configurador
{
    public sealed class JanelaPrincipal : Form
    {
        // ---- estado ----
        private ArquivoPerfis _arquivo;
        private string _caminho;
        private string _perfilNaTela;
        private bool _sujo;
        private bool _carregando;      // true enquanto a tela esta sendo preenchida por codigo
        private bool _sincronizando;   // true enquanto barra e caixa de numero se copiam
        private int _contadorDoRelogio;

        private readonly ProcessoPad _pad = new ProcessoPad();
        private string _caminhoDoPadExe = ProcessoPad.ProcurarExecutavel();
        private readonly List<Mapeamento> _mapasNaLista = new List<Mapeamento>();
        private readonly ToolTip _dicas = new ToolTip();

        // ---- controles ----
        private readonly ListBox _lstPerfis = Tema.Lista(58);
        private readonly DesenhoControle _desenho = new DesenhoControle();
        private readonly ListBox _lstTeclas = Tema.Lista(24);   // guarda os dados; quem aparece sao os chips
        private readonly ListBox _lstAlvos = Tema.Lista(34);    // a aba "Lista de teclas"

        private readonly Label _lblSelecionado = new Label();
        private readonly Label _lblGrupo = new Label();
        private readonly Label _lblToken = new Label();
        private readonly Label _lblDirecao = new Label();
        private readonly Label _lblDica = new Label();
        private readonly Label _lblComportamentoNota = new Label();
        private readonly Label _lblAjudaDesenho = new Label();
        private readonly Label _lblVivoEstado = new Label();
        private readonly Label _lblVivoMensagem = new Label();
        private readonly Label _lblVivoEvento = new Label();
        private readonly Label _lblStatus = new Label();
        private readonly Label _lblArquivo = new Label();
        private readonly Label _lblEstadoPad = new Label();
        private readonly Label _lblResumo = new Label();
        private readonly Label _lblResumoTitulo = new Label();
        private Label _lblQtdPerfis;
        private Label _lblQtdTeclas;
        private readonly Label _lblTotalAlvos = new Label();
        private readonly Label _lblPerfilTitulo = new Label();
        private readonly Label _lblPerfilBadge = new Label();
        private readonly Label _lblSalvoEstado = new Label();
        private readonly Label _lblRodape = new Label();
        private readonly Label _lblAjuda = new Label();

        private readonly PontoDeStatus _pontoPad = new PontoDeStatus();
        private readonly PontoDeStatus _pontoVivo = new PontoDeStatus();
        private readonly PontoDeStatus _pontoResumo = new PontoDeStatus();
        private readonly PontoDeStatus _pontoAlvo = new PontoDeStatus();

        private readonly FlowLayoutPanel _painelTeclas = new FlowLayoutPanel();
        private readonly Panel _painelControle = new Panel();
        private readonly Panel _painelLista = new Panel();
        private readonly PainelComBorda _painelUnmapped = new PainelComBorda();
        private readonly PainelComBorda _caixaVivo = new PainelComBorda();
        private readonly PainelComBorda _token = new PainelComBorda();
        private readonly Panel _inspetor = new Panel();
        private BotaoAba _abaControle;
        private BotaoAba _abaLista;
        private ContextMenuStrip _menuAjuda;

        private readonly BotaoDeIcone _btnCapturar = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnArquivo = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnAjustes = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnAtalhos = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnRecarregar = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnSalvar = new BotaoDeIcone();
        private readonly BotaoDeIcone _btnNovoPerfil = new BotaoDeIcone();

        private readonly Button _btnTeclaExtra = Tema.BotaoContorno("+ Tecla extra");
        private readonly Button _btnRemover = Tema.BotaoContorno("Remover");
        private readonly Button _btnAvancado = Tema.BotaoContorno("Avancado");
        private readonly Button _btnDuplicar = Tema.BotaoContorno("Duplicar");
        private readonly Button _btnRenomear = Tema.BotaoContorno("Renomear");
        private readonly Button _btnExcluir = Tema.BotaoContorno("Excluir");
        private readonly Button _btnLigarPad = new Button();
        private readonly Button _btnAbrir = new Button();   // legado: quem abre agora e o botao do arquivo

        private readonly CheckBox _chkInicial = Tema.CaixaDeMarcar("Usar ao iniciar o pad");
        private readonly CheckBox _chkReiniciar = Tema.CaixaDeMarcar("Reiniciar ao salvar");
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
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1270, 980);
            MinimumSize = new Size(1120, 900);

            ConstruirTela();
            LigarEventos();

            _tmrVivo.Interval = 60;
            _tmrVivo.Tick += TicVivo;
            _tmrVivo.Start();

            // sem argumento, abre o perfil que fica ao lado do programa (profiles/corrida.json)
            AbrirArquivo(string.IsNullOrWhiteSpace(arquivo) ? PerfilPadrao() : arquivo);
        }

        // =====================================================================================
        // barra de titulo escura
        //
        // Isto estava escrito aqui dentro; em 2026-10-18 virou uma funcao do tema
        // (Tema.BarraDeTituloEscura) para valer tambem nos dialogos, que ficavam com a barra clara no
        // meio da tela escura. O porque do atributo 20 e depois 19 esta comentado la.
        // =====================================================================================

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Tema.BarraDeTituloEscura(Handle);
            Tema.IconeDaJanela(this);   // a logo do programa na barra de titulo e na barra de tarefas
        }

        // =====================================================================================
        // montagem da tela
        // =====================================================================================

        private void ConstruirTela()
        {
            TableLayoutPanel raiz = new TableLayoutPanel();
            raiz.Dock = DockStyle.Fill;
            raiz.BackColor = Tema.Fundo;
            raiz.ColumnCount = 1;
            raiz.RowCount = 3;
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 66f));    // topo
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // corpo
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 67f));    // rodape + barra de estado

            raiz.Controls.Add(ConstruirTopo(), 0, 0);
            raiz.Controls.Add(ConstruirCorpo(), 0, 1);
            raiz.Controls.Add(ConstruirRodape(), 0, 2);

            Controls.Add(raiz);
        }

        // -------------------------------------------------------------------------------------
        // topo: a marca, o botao de ajuda, o selo e o estado do pad
        // -------------------------------------------------------------------------------------

        private Control ConstruirTopo()
        {
            Panel barra = new Panel();
            barra.Dock = DockStyle.Fill;
            barra.BackColor = Tema.Fundo;
            barra.Padding = new Padding(24, 0, 24, 0);
            barra.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, barra.ClientSize.Height - 1, barra.ClientSize.Width, barra.ClientSize.Height - 1);
            };

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.Fundo;
            t.ColumnCount = 5;
            t.RowCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ---- marca: "✣ KeyToJoy / configurador" ----
            Label icone = new Label();
            icone.Text = "✣";
            icone.Font = Tema.Simbolo26;
            icone.ForeColor = Tema.Realce;
            icone.AutoSize = true;
            icone.Anchor = AnchorStyles.Left;
            icone.TextAlign = ContentAlignment.MiddleLeft;
            icone.BackColor = Tema.Fundo;
            icone.Margin = new Padding(0);
            t.Controls.Add(icone, 0, 0);

            TableLayoutPanel nomes = new TableLayoutPanel();
            nomes.BackColor = Tema.Fundo;
            nomes.ColumnCount = 2;
            nomes.RowCount = 1;
            nomes.AutoSize = true;
            nomes.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            nomes.Anchor = AnchorStyles.Left;
            nomes.Margin = new Padding(0);
            nomes.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nomes.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nomes.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label titulo = new Label();
            titulo.Text = "KeyToJoy";
            titulo.Font = Tema.Fonte16N;
            titulo.ForeColor = Tema.Texto;
            titulo.AutoSize = true;
            titulo.Anchor = AnchorStyles.Left;
            titulo.BackColor = Tema.Fundo;
            titulo.Margin = new Padding(0);
            nomes.Controls.Add(titulo, 0, 0);

            Label versao = new Label();
            versao.Text = "/ configurador";
            versao.Font = Tema.Fonte11;
            versao.ForeColor = Tema.Texto3;
            versao.AutoSize = true;
            versao.Anchor = AnchorStyles.Left;
            versao.BackColor = Tema.Fundo;
            versao.Margin = new Padding(9, 0, 0, 0);
            nomes.Controls.Add(versao, 1, 0);

            t.Controls.Add(nomes, 1, 0);

            // ---- direita: ajuda ----
            _lblAjuda.Text = "AJUDA";
            _lblAjuda.Font = Tema.Fonte10;
            _lblAjuda.ForeColor = Tema.Texto3;
            _lblAjuda.AutoSize = true;
            _lblAjuda.Anchor = AnchorStyles.Right;
            _lblAjuda.TextAlign = ContentAlignment.MiddleRight;
            _lblAjuda.BackColor = Tema.Fundo;
            _lblAjuda.Cursor = Cursors.Hand;
            _lblAjuda.Margin = new Padding(0, 0, 22, 0);
            _lblAjuda.Click += delegate { MostrarMenuDeAjuda(); };
            _lblAjuda.MouseEnter += delegate { _lblAjuda.ForeColor = Tema.Texto; };
            _lblAjuda.MouseLeave += delegate { _lblAjuda.ForeColor = Tema.Texto3; };
            t.Controls.Add(_lblAjuda, 2, 0);

            // ---- selo "CONFIGURADOR" (tamanho fixo para nao quebrar linha) ----
            PainelComBorda selo = new PainelComBorda();
            selo.CorBorda = Tema.Borda;
            selo.Raio = 4;
            selo.BackColor = Tema.Fundo;
            selo.Padding = new Padding(1);
            selo.Size = new Size(126, 26);
            selo.Anchor = AnchorStyles.Right;
            selo.Margin = new Padding(0, 0, 20, 0);
            Label seloTexto = new Label();
            seloTexto.Text = "CONFIGURADOR";
            seloTexto.Font = Tema.Fonte9;
            seloTexto.ForeColor = Tema.Texto3;
            seloTexto.AutoSize = false;
            seloTexto.Dock = DockStyle.Fill;
            seloTexto.TextAlign = ContentAlignment.MiddleCenter;
            seloTexto.BackColor = Tema.Fundo;
            selo.Controls.Add(seloTexto);
            t.Controls.Add(selo, 3, 0);

            // ---- ponto + estado do pad ----
            TableLayoutPanel estado = new TableLayoutPanel();
            estado.BackColor = Tema.Fundo;
            estado.ColumnCount = 2;
            estado.RowCount = 1;
            estado.AutoSize = true;
            estado.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            estado.Anchor = AnchorStyles.Left;
            estado.Margin = new Padding(0);
            estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            estado.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _pontoPad.Size = new Size(6, 6);
            _pontoPad.Anchor = AnchorStyles.Left;
            _pontoPad.Margin = new Padding(0, 0, 9, 0);
            estado.Controls.Add(_pontoPad, 0, 0);

            _lblEstadoPad.Text = "pad desligado";
            _lblEstadoPad.Font = Tema.Fonte10;
            _lblEstadoPad.ForeColor = Tema.Texto2;
            _lblEstadoPad.AutoSize = true;
            _lblEstadoPad.Anchor = AnchorStyles.Left;
            _lblEstadoPad.TextAlign = ContentAlignment.MiddleLeft;
            _lblEstadoPad.BackColor = Tema.Fundo;
            _lblEstadoPad.Margin = new Padding(0);
            estado.Controls.Add(_lblEstadoPad, 1, 0);

            t.Controls.Add(estado, 4, 0);

            barra.Controls.Add(t);
            return barra;
        }

        private Control ConstruirCorpo()
        {
            TableLayoutPanel corpo = new TableLayoutPanel();
            corpo.Dock = DockStyle.Fill;
            corpo.BackColor = Tema.Fundo;
            corpo.ColumnCount = 3;
            corpo.RowCount = 1;
            corpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            corpo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            corpo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285f));
            corpo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            corpo.Controls.Add(ConstruirBarraLateral(), 0, 0);
            corpo.Controls.Add(ConstruirCentro(), 1, 0);
            corpo.Controls.Add(ConstruirInspetor(), 2, 0);
            return corpo;
        }

        // -------------------------------------------------------------------------------------
        // lateral: arquivo atual, perfis e as acoes do perfil
        // -------------------------------------------------------------------------------------

        private Control ConstruirBarraLateral()
        {
            Panel fundo = new Panel();
            fundo.Dock = DockStyle.Fill;
            fundo.BackColor = Tema.FundoSidebar;
            fundo.Padding = new Padding(15, 24, 15, 18);
            fundo.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, fundo.ClientSize.Width - 1, 0, fundo.ClientSize.Width - 1, fundo.ClientSize.Height);
            };

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.FundoSidebar;
            t.ColumnCount = 1;
            t.RowCount = 10;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 16f));    // ARQUIVO ATUAL / JSON
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));    // botao do arquivo
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));    // PERFIS / 00
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // lista de perfis
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));    // + novo perfil
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));    // duplicar/renomear/excluir
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));    // usar ao iniciar
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));    // ajustes
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));    // atalhos
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));    // nota

            Label valorArquivo;
            t.Controls.Add(RotuloSecao("Arquivo atual", "JSON", out valorArquivo), 0, 0);

            _btnArquivo.IconeEsquerda = "▤";
            _btnArquivo.Texto = "(sem arquivo)";
            _btnArquivo.IconeDireita = "↗";
            _btnArquivo.FonteEsq = Tema.Simbolo18;
            _btnArquivo.FonteTexto = Tema.Fonte11;
            _btnArquivo.FonteDir = Tema.Simbolo11;
            _btnArquivo.Fundo = Tema.BotaoFundo;
            _btnArquivo.CorBorda = Tema.Borda;
            _btnArquivo.TextoCor = Tema.BioTexto;
            _btnArquivo.TextoCorHover = Tema.Texto;
            _btnArquivo.CorEsq = Tema.Realce;
            _btnArquivo.CorDir = Tema.Texto3;
            _btnArquivo.BackColor = Tema.FundoSidebar;
            _btnArquivo.Dock = DockStyle.Fill;
            _btnArquivo.Margin = new Padding(0, 12, 0, 0);
            _btnArquivo.Clicar += delegate { EscolherArquivo(); };
            t.Controls.Add(_btnArquivo, 0, 1);

            Label valorPerfis;
            Panel rotuloPerfis = RotuloSecao("Perfis", "00", out valorPerfis);
            rotuloPerfis.Margin = new Padding(3, 30, 3, 12);
            _lblQtdPerfis = valorPerfis;
            t.Controls.Add(rotuloPerfis, 0, 2);

            _lstPerfis.Margin = new Padding(0);
            _lstPerfis.BackColor = Tema.FundoSidebar;
            t.Controls.Add(_lstPerfis, 0, 3);

            _btnNovoPerfil.IconeEsquerda = "";
            _btnNovoPerfil.Texto = "+ Novo perfil";
            _btnNovoPerfil.IconeDireita = "";
            _btnNovoPerfil.FonteTexto = Tema.Fonte11;
            _btnNovoPerfil.Fundo = Color.Empty;
            _btnNovoPerfil.FundoHover = Tema.Hover;
            _btnNovoPerfil.CorBorda = Tema.SeloBorda;
            _btnNovoPerfil.Tracejada = true;
            _btnNovoPerfil.TextoCor = Tema.Realce;
            _btnNovoPerfil.TextoCorHover = Tema.Realce;
            _btnNovoPerfil.CorEsq = Tema.Realce;
            _btnNovoPerfil.CorDir = Tema.Realce;
            _btnNovoPerfil.BackColor = Tema.FundoSidebar;
            _btnNovoPerfil.Dock = DockStyle.Fill;
            _btnNovoPerfil.Margin = new Padding(0, 14, 0, 0);
            _btnNovoPerfil.Clicar += delegate { CriarPerfil(); };
            t.Controls.Add(_btnNovoPerfil, 0, 4);

            TableLayoutPanel pequenos = new TableLayoutPanel();
            pequenos.Dock = DockStyle.Fill;
            pequenos.BackColor = Tema.FundoSidebar;
            pequenos.ColumnCount = 3;
            pequenos.RowCount = 1;
            pequenos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            pequenos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            pequenos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            pequenos.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _btnDuplicar.Dock = DockStyle.Fill;
            _btnRenomear.Dock = DockStyle.Fill;
            _btnExcluir.Dock = DockStyle.Fill;
            _btnDuplicar.Margin = new Padding(0, 1, 4, 1);
            _btnRenomear.Margin = new Padding(0, 1, 4, 1);
            _btnExcluir.Margin = new Padding(0, 1, 0, 1);
            pequenos.Controls.Add(_btnDuplicar, 0, 0);
            pequenos.Controls.Add(_btnRenomear, 1, 0);
            pequenos.Controls.Add(_btnExcluir, 2, 0);
            t.Controls.Add(pequenos, 0, 5);

            _chkInicial.AutoSize = false;
            _chkInicial.Dock = DockStyle.Fill;
            _chkInicial.TextAlign = ContentAlignment.MiddleLeft;
            _chkInicial.Margin = new Padding(0, 12, 0, 12);
            t.Controls.Add(_chkInicial, 0, 6);

            _btnAjustes.IconeEsquerda = "⚙";
            _btnAjustes.Texto = "Ajustes do perfil";
            _btnAjustes.IconeDireita = "↗";
            _btnAjustes.FonteEsq = Tema.Simbolo11;
            _btnAjustes.FonteTexto = Tema.Fonte10;
            _btnAjustes.FonteDir = Tema.Simbolo9;
            _btnAjustes.Fundo = Color.Empty;
            _btnAjustes.FundoHover = Tema.Hover;
            _btnAjustes.CorBorda = Color.Empty;
            _btnAjustes.TextoCor = Tema.Texto2;
            _btnAjustes.TextoCorHover = Tema.Texto;
            _btnAjustes.CorEsq = Tema.Texto2;
            _btnAjustes.CorDir = Tema.Texto3;
            _btnAjustes.BackColor = Tema.FundoSidebar;
            _btnAjustes.Dock = DockStyle.Fill;
            _btnAjustes.Clicar += delegate { AbrirAjustes(); };
            t.Controls.Add(_btnAjustes, 0, 7);

            _btnAtalhos.IconeEsquerda = "⌘";
            _btnAtalhos.Texto = "Atalhos de teclado";
            _btnAtalhos.IconeDireita = "↗";
            _btnAtalhos.FonteEsq = Tema.Simbolo11;
            _btnAtalhos.FonteTexto = Tema.Fonte10;
            _btnAtalhos.FonteDir = Tema.Simbolo9;
            _btnAtalhos.Fundo = Color.Empty;
            _btnAtalhos.FundoHover = Tema.Hover;
            _btnAtalhos.CorBorda = Color.Empty;
            _btnAtalhos.TextoCor = Tema.Texto2;
            _btnAtalhos.TextoCorHover = Tema.Texto;
            _btnAtalhos.CorEsq = Tema.Texto2;
            _btnAtalhos.CorDir = Tema.Texto3;
            _btnAtalhos.BackColor = Tema.FundoSidebar;
            _btnAtalhos.Dock = DockStyle.Fill;
            _btnAtalhos.Clicar += delegate { AbrirAtalhos(); };
            t.Controls.Add(_btnAtalhos, 0, 8);

            Panel nota = new Panel();
            nota.Dock = DockStyle.Fill;
            nota.BackColor = Tema.FundoSidebar;
            nota.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, 0, nota.ClientSize.Width, 0);
            };
            Label nota1 = new Label();
            nota1.Text = "TECLADO → CONTROLE VIRTUAL";
            nota1.Font = Tema.Fonte8;
            nota1.ForeColor = Color.FromArgb(0x6d, 0x77, 0x7c);
            nota1.AutoSize = true;
            nota1.BackColor = Tema.FundoSidebar;
            nota1.Location = new Point(0, 17);
            Label nota2 = new Label();
            nota2.Text = "24 entradas · XInput";
            nota2.Font = Tema.Fonte8;
            nota2.ForeColor = Color.FromArgb(0x50, 0x5b, 0x61);
            nota2.AutoSize = true;
            nota2.BackColor = Tema.FundoSidebar;
            nota2.Location = new Point(0, 32);
            nota.Controls.Add(nota1);
            nota.Controls.Add(nota2);
            t.Controls.Add(nota, 0, 9);

            fundo.Controls.Add(t);
            return fundo;
        }

        // -------------------------------------------------------------------------------------
        // centro: cabecalho, abas e o controle desenhado (ou a lista de teclas)
        // -------------------------------------------------------------------------------------

        private Control ConstruirCentro()
        {
            Panel centro = new Panel();
            centro.Dock = DockStyle.Fill;
            centro.BackColor = Tema.Fundo;
            centro.Padding = new Padding(25, 28, 25, 20);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.Fundo;
            t.ColumnCount = 1;
            t.RowCount = 3;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));   // cabecalho
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 66f));   // abas
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // conteudo

            t.Controls.Add(ConstruirCabecalho(), 0, 0);
            t.Controls.Add(ConstruirAbas(), 0, 1);
            t.Controls.Add(ConstruirConteudoDoCentro(), 0, 2);

            centro.Controls.Add(t);
            return centro;
        }

        private Control ConstruirCabecalho()
        {
            TableLayoutPanel cab = new TableLayoutPanel();
            cab.Dock = DockStyle.Fill;
            cab.BackColor = Tema.Fundo;
            cab.ColumnCount = 2;
            cab.RowCount = 1;
            cab.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            cab.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cab.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            TableLayoutPanel textos = new TableLayoutPanel();
            textos.Dock = DockStyle.Fill;
            textos.BackColor = Tema.Fundo;
            textos.ColumnCount = 1;
            textos.RowCount = 3;
            textos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            textos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            textos.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            textos.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label sobrancelha = new Label();
            sobrancelha.Text = "CONFIGURACAO DO CONTROLE";
            sobrancelha.Font = Tema.Fonte9;
            sobrancelha.ForeColor = Tema.Texto3;
            sobrancelha.Dock = DockStyle.Fill;
            sobrancelha.TextAlign = ContentAlignment.MiddleLeft;
            sobrancelha.BackColor = Tema.Fundo;
            textos.Controls.Add(sobrancelha, 0, 0);

            _lblPerfilTitulo.Text = "(nenhum perfil)";
            _lblPerfilTitulo.Font = Tema.Fonte16N;
            _lblPerfilTitulo.ForeColor = Tema.Texto;
            _lblPerfilTitulo.Dock = DockStyle.Fill;
            _lblPerfilTitulo.TextAlign = ContentAlignment.MiddleLeft;
            _lblPerfilTitulo.BackColor = Tema.Fundo;
            textos.Controls.Add(_lblPerfilTitulo, 0, 1);

            Label sub = new Label();
            sub.Text = "Seu teclado. Um novo controle.";
            sub.Font = Tema.Fonte10;
            sub.ForeColor = Tema.Texto3;
            sub.Dock = DockStyle.Fill;
            sub.TextAlign = ContentAlignment.TopLeft;
            sub.BackColor = Tema.Fundo;
            textos.Controls.Add(sub, 0, 2);

            cab.Controls.Add(textos, 0, 0);

            PainelComBorda selo = new PainelComBorda();
            selo.CorBorda = Tema.SeloBorda;
            selo.CorFundo = Tema.SeloFundo;
            selo.Raio = 4;
            selo.BackColor = Tema.Fundo;
            selo.Padding = new Padding(8, 6, 8, 6);
            selo.AutoSize = true;
            selo.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            selo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            selo.Margin = new Padding(10, 25, 0, 0);

            _lblPerfilBadge.Text = "PERFIL INICIAL";
            _lblPerfilBadge.Font = Tema.Fonte9;
            _lblPerfilBadge.ForeColor = Tema.Realce;
            _lblPerfilBadge.AutoSize = true;
            _lblPerfilBadge.BackColor = Tema.SeloFundo;
            selo.Controls.Add(_lblPerfilBadge);
            cab.Controls.Add(selo, 1, 0);

            return cab;
        }

        private Control ConstruirAbas()
        {
            Panel abas = new Panel();
            abas.Dock = DockStyle.Fill;
            abas.BackColor = Tema.Fundo;
            abas.Padding = new Padding(0, 26, 0, 0);
            abas.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, abas.ClientSize.Height - 1, abas.ClientSize.Width, abas.ClientSize.Height - 1);
            };

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.Fundo;
            t.ColumnCount = 3;
            t.RowCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _abaControle = new BotaoAba("Controle");
            _abaControle.Size = new Size(74, 40);
            _abaControle.Dock = DockStyle.None;
            _abaControle.Ativo = true;
            _abaControle.Clicar += delegate { TrocarAba(false); };
            t.Controls.Add(_abaControle, 0, 0);

            _abaLista = new BotaoAba("Lista de teclas");
            _abaLista.Size = new Size(126, 40);
            _abaLista.Dock = DockStyle.None;
            _abaLista.Clicar += delegate { TrocarAba(true); };
            t.Controls.Add(_abaLista, 1, 0);

            _lblTotalAlvos.Text = "0 / 24 mapeados";
            _lblTotalAlvos.Font = Tema.Fonte9;
            _lblTotalAlvos.ForeColor = Tema.Texto3;
            _lblTotalAlvos.Dock = DockStyle.Fill;
            _lblTotalAlvos.TextAlign = ContentAlignment.MiddleRight;
            _lblTotalAlvos.BackColor = Tema.Fundo;
            t.Controls.Add(_lblTotalAlvos, 2, 0);

            abas.Controls.Add(t);
            return abas;
        }

        private Control ConstruirConteudoDoCentro()
        {
            Panel conteudo = new Panel();
            conteudo.Dock = DockStyle.Fill;
            conteudo.BackColor = Tema.Fundo;

            // ---- aba "Controle": o desenho antigo, com a nota e a linha de baixo ----
            _painelControle.Dock = DockStyle.Fill;
            _painelControle.BackColor = Tema.Fundo;

            TableLayoutPanel c = new TableLayoutPanel();
            c.Dock = DockStyle.Fill;
            c.BackColor = Tema.Fundo;
            c.ColumnCount = 1;
            c.RowCount = 3;
            c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            c.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // desenho
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));   // nota
            c.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // linha de baixo

            _desenho.Dock = DockStyle.Fill;
            _desenho.BackColor = Tema.Fundo;
            _desenho.Margin = new Padding(0, 23, 0, 0);
            c.Controls.Add(_desenho, 0, 0);

            Panel nota = new Panel();
            nota.Dock = DockStyle.Fill;
            nota.BackColor = Tema.Fundo;
            FlowLayoutPanel central = new FlowLayoutPanel();
            central.AutoSize = true;
            central.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            central.FlowDirection = FlowDirection.LeftToRight;
            central.WrapContents = false;
            central.Anchor = AnchorStyles.None;
            central.BackColor = Tema.Fundo;
            Label seta = new Label();
            seta.Text = "↖";
            seta.Font = Tema.Simbolo11;
            seta.ForeColor = Tema.Realce;
            seta.AutoSize = true;
            seta.BackColor = Tema.Fundo;
            seta.Margin = new Padding(0, 3, 5, 0);
            central.Controls.Add(seta);
            _lblAjudaDesenho.Text = "Clique num alvo do controle para editar a tecla dele.";
            _lblAjudaDesenho.Font = Tema.Fonte9;
            _lblAjudaDesenho.ForeColor = Tema.Texto3;
            _lblAjudaDesenho.AutoSize = true;
            _lblAjudaDesenho.BackColor = Tema.Fundo;
            central.Controls.Add(_lblAjudaDesenho);
            nota.Controls.Add(central);
            c.Controls.Add(nota, 0, 1);

            Panel info = new Panel();
            info.Dock = DockStyle.Fill;
            info.BackColor = Tema.Fundo;
            info.Padding = new Padding(0, 14, 0, 0);
            info.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, 0, info.ClientSize.Width, 0);
            };
            TableLayoutPanel ii = new TableLayoutPanel();
            ii.Dock = DockStyle.Fill;
            ii.BackColor = Tema.Fundo;
            ii.ColumnCount = 2;
            ii.RowCount = 1;
            ii.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            ii.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ii.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel esquerdaInfo = new Panel();
            esquerdaInfo.Dock = DockStyle.Fill;
            esquerdaInfo.BackColor = Tema.Fundo;
            PontoDeStatus pontinho = new PontoDeStatus();
            pontinho.Ligado = true;
            pontinho.ComBrilho = false;
            pontinho.Size = new Size(5, 5);
            pontinho.Location = new Point(0, 6);
            esquerdaInfo.Controls.Add(pontinho);
            Label info1 = new Label();
            info1.Text = "Mapeamento direto";
            info1.Font = Tema.Fonte8;
            info1.ForeColor = Color.FromArgb(0x90, 0xa3, 0x98);
            info1.AutoSize = true;
            info1.BackColor = Tema.Fundo;
            info1.Location = new Point(11, 0);
            esquerdaInfo.Controls.Add(info1);
            ii.Controls.Add(esquerdaInfo, 0, 0);

            Label info2 = new Label();
            info2.Text = "Pressionar mantém · soltar libera";
            info2.Font = Tema.Fonte8;
            info2.ForeColor = Tema.Texto3;
            info2.AutoSize = true;
            info2.Anchor = AnchorStyles.Right;
            info2.TextAlign = ContentAlignment.MiddleRight;
            info2.BackColor = Tema.Fundo;
            ii.Controls.Add(info2, 1, 0);
            info.Controls.Add(ii);
            c.Controls.Add(info, 0, 2);

            _painelControle.Controls.Add(c);

            // ---- aba "Lista de teclas" ----
            _painelLista.Dock = DockStyle.Fill;
            _painelLista.BackColor = Tema.Fundo;
            _painelLista.Visible = false;

            TableLayoutPanel lv = new TableLayoutPanel();
            lv.Dock = DockStyle.Fill;
            lv.BackColor = Tema.Fundo;
            lv.ColumnCount = 1;
            lv.RowCount = 2;
            lv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            lv.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));   // cabecalho da tabela
            lv.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // lista

            Panel cabecalho = new Panel();
            cabecalho.Dock = DockStyle.Fill;
            cabecalho.BackColor = Tema.Fundo;
            cabecalho.Padding = new Padding(0, 0, 0, 10);
            cabecalho.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, cabecalho.ClientSize.Height - 1, cabecalho.ClientSize.Width, cabecalho.ClientSize.Height - 1);
            };
            Label colAlvo = new Label();
            colAlvo.Text = "GRUPO";
            colAlvo.Font = Tema.Fonte9;
            colAlvo.ForeColor = Tema.Texto3;
            colAlvo.AutoSize = true;
            colAlvo.Dock = DockStyle.Left;
            colAlvo.TextAlign = ContentAlignment.BottomLeft;
            colAlvo.BackColor = Tema.Fundo;
            cabecalho.Controls.Add(colAlvo);
            Label colTecla = new Label();
            colTecla.Text = "TECLA";
            colTecla.Font = Tema.Fonte9;
            colTecla.ForeColor = Tema.Texto3;
            colTecla.AutoSize = true;
            colTecla.Dock = DockStyle.Right;
            colTecla.TextAlign = ContentAlignment.BottomRight;
            colTecla.BackColor = Tema.Fundo;
            colTecla.Padding = new Padding(0, 0, 14, 0);
            cabecalho.Controls.Add(colTecla);
            lv.Controls.Add(cabecalho, 0, 0);

            _lstAlvos.Margin = new Padding(0);
            _lstAlvos.BackColor = Tema.Fundo;
            lv.Controls.Add(_lstAlvos, 0, 1);

            _painelLista.Controls.Add(lv);

            conteudo.Controls.Add(_painelLista);
            conteudo.Controls.Add(_painelControle);
            return conteudo;
        }

        // -------------------------------------------------------------------------------------
        // inspetor: o alvo escolhido, as teclas e o teste ao vivo
        // -------------------------------------------------------------------------------------

        private Control ConstruirInspetor()
        {
            _inspetor.Dock = DockStyle.Fill;
            _inspetor.BackColor = Tema.FundoInspetor;
            _inspetor.Padding = new Padding(20, 27, 20, 20);
            _inspetor.AutoScroll = true;
            Tema.EscurecerBarraDeRolagem(_inspetor);
            _inspetor.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, 0, 0, _inspetor.ClientSize.Height);
            };

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top;
            t.AutoSize = true;
            t.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            t.BackColor = Tema.FundoInspetor;
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label valorMapeamento;
            Linha(t, RotuloSecao("Mapeamento", "EDITAR", out valorMapeamento), 16f, new Padding(0));
            Linha(t, ConstruirCabecaDoAlvo(), 83f, new Padding(0, 23, 0, 0));

            _lblDirecao.Text = "";
            _lblDirecao.Font = Tema.Fonte10;
            _lblDirecao.ForeColor = Tema.Texto2;
            _lblDirecao.AutoSize = true;
            _lblDirecao.MaximumSize = new Size(240, 0);
            _lblDirecao.BackColor = Tema.FundoInspetor;
            _lblDirecao.Dock = DockStyle.Top;
            LinhaAuto(t, _lblDirecao, new Padding(0, 19, 0, 0));

            Label valorTeclas;
            Linha(t, RotuloSecao("Teclas atribuidas", "00", out valorTeclas), 44f, new Padding(0, 26, 0, 2));
            _lblQtdTeclas = valorTeclas;

            _painelTeclas.AutoSize = true;
            _painelTeclas.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _painelTeclas.WrapContents = true;
            _painelTeclas.FlowDirection = FlowDirection.LeftToRight;
            _painelTeclas.BackColor = Tema.FundoInspetor;
            _painelTeclas.MinimumSize = new Size(0, 44);
            _painelTeclas.Margin = new Padding(0, 13, 0, 0);
            _painelTeclas.Dock = DockStyle.Top;
            LinhaAuto(t, _painelTeclas, new Padding(0));

            _btnCapturar.IconeEsquerda = "⌨";
            _btnCapturar.Texto = "Capturar tecla";
            _btnCapturar.IconeDireita = "↵";
            _btnCapturar.FonteEsq = Tema.Simbolo15;
            _btnCapturar.FonteTexto = Tema.Fonte10;
            _btnCapturar.FonteDir = Tema.Simbolo11;
            _btnCapturar.Fundo = Tema.Texto;
            _btnCapturar.FundoHover = Tema.BotaoClaroHover;
            _btnCapturar.CorBorda = Tema.Texto;
            _btnCapturar.TextoCor = Tema.BotaoClaro;
            _btnCapturar.TextoCorHover = Color.FromArgb(0x10, 0x1b, 0x14);
            _btnCapturar.CorEsq = Tema.BotaoClaro;
            _btnCapturar.CorDir = Color.FromArgb(0x6a, 0x73, 0x6d);
            _btnCapturar.BackColor = Tema.FundoInspetor;
            _btnCapturar.Alinhamento = ContentAlignment.MiddleCenter;
            _btnCapturar.Clicar += delegate { CapturarTecla(false); };
            Linha(t, _btnCapturar, 49f, new Padding(0, 12, 0, 0));

            TableLayoutPanel acoesTecla = new TableLayoutPanel();
            acoesTecla.Dock = DockStyle.Fill;
            acoesTecla.BackColor = Tema.FundoInspetor;
            acoesTecla.ColumnCount = 2;
            acoesTecla.RowCount = 1;
            acoesTecla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            acoesTecla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            acoesTecla.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _btnTeclaExtra.Dock = DockStyle.Fill;
            _btnRemover.Dock = DockStyle.Fill;
            _btnTeclaExtra.Margin = new Padding(0, 0, 7, 0);
            _btnRemover.Margin = new Padding(0);
            acoesTecla.Controls.Add(_btnTeclaExtra, 0, 0);
            acoesTecla.Controls.Add(_btnRemover, 1, 0);
            Linha(t, acoesTecla, 38f, new Padding(0, 8, 0, 0));

            Panel dica = new Panel();
            dica.Dock = DockStyle.Fill;
            dica.BackColor = Tema.FundoInspetor;
            Label dicaSeta = new Label();
            dicaSeta.Text = "↳";
            dicaSeta.Font = Tema.Simbolo11;
            dicaSeta.ForeColor = Color.FromArgb(0x89, 0x96, 0x8f);
            dicaSeta.AutoSize = true;
            dicaSeta.BackColor = Tema.FundoInspetor;
            dicaSeta.Dock = DockStyle.Left;
            dicaSeta.TextAlign = ContentAlignment.TopLeft;
            dica.Controls.Add(dicaSeta);
            _lblDica.Text = "Clique em capturar e pressione uma tecla.\r\nUse Esc para cancelar.";
            _lblDica.Font = Tema.Fonte9;
            _lblDica.ForeColor = Tema.Texto3;
            _lblDica.Dock = DockStyle.Fill;
            _lblDica.TextAlign = ContentAlignment.TopLeft;
            _lblDica.BackColor = Tema.FundoInspetor;
            _lblDica.Padding = new Padding(20, 0, 0, 0);
            dica.Controls.Add(_lblDica);
            Linha(t, dica, 46f, new Padding(0, 14, 0, 0));

            Panel divisor = new Panel();
            divisor.Dock = DockStyle.Fill;
            divisor.BackColor = Tema.Borda;
            Linha(t, divisor, 49f, new Padding(0, 24, 0, 24));

            Label valorComportamento;
            Linha(t, RotuloSecao("Comportamento", "", out valorComportamento), 16f, new Padding(0));

            TableLayoutPanel comportamento = new TableLayoutPanel();
            comportamento.Dock = DockStyle.Fill;
            comportamento.BackColor = Tema.FundoInspetor;
            comportamento.ColumnCount = 2;
            comportamento.RowCount = 1;
            comportamento.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            comportamento.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            comportamento.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Label rotuloComportamento = new Label();
            rotuloComportamento.Text = "Modo de acionamento";
            rotuloComportamento.Font = Tema.Fonte9;
            rotuloComportamento.ForeColor = Tema.Texto2;
            rotuloComportamento.Dock = DockStyle.Fill;
            rotuloComportamento.TextAlign = ContentAlignment.MiddleLeft;
            rotuloComportamento.BackColor = Tema.FundoInspetor;
            comportamento.Controls.Add(rotuloComportamento, 0, 0);

            PainelComBorda seloComportamento = new PainelComBorda();
            seloComportamento.CorBorda = Color.FromArgb(0x38, 0x41, 0x3e);
            seloComportamento.Raio = 3;
            seloComportamento.BackColor = Tema.FundoInspetor;
            seloComportamento.Padding = new Padding(5, 4, 5, 4);
            seloComportamento.AutoSize = true;
            seloComportamento.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            seloComportamento.Anchor = AnchorStyles.Right;
            Label textoComportamento = new Label();
            textoComportamento.Text = "SEGURAR";
            textoComportamento.Font = Tema.Fonte9;
            textoComportamento.ForeColor = Color.FromArgb(0xa8, 0xb7, 0xae);
            textoComportamento.AutoSize = true;
            textoComportamento.BackColor = Tema.FundoInspetor;
            seloComportamento.Controls.Add(textoComportamento);
            comportamento.Controls.Add(seloComportamento, 1, 0);
            Linha(t, comportamento, 37f, new Padding(0, 15, 0, 0));

            _lblComportamentoNota.Text = "";
            _lblComportamentoNota.Font = Tema.Fonte9;
            _lblComportamentoNota.ForeColor = Tema.Texto3;
            _lblComportamentoNota.AutoSize = true;
            _lblComportamentoNota.MaximumSize = new Size(240, 0);
            _lblComportamentoNota.BackColor = Tema.FundoInspetor;
            _lblComportamentoNota.Dock = DockStyle.Top;
            LinhaAuto(t, _lblComportamentoNota, new Padding(0, 12, 0, 0));

            ConstruirPainelDoEixo();
            LinhaAuto(t, _painelDoEixo, new Padding(0, 18, 0, 0));

            ConstruirCaixaSemTecla();
            LinhaAuto(t, _painelUnmapped, new Padding(0, 24, 0, 0));

            LinhaAuto(t, ConstruirVivo(), new Padding(0, 30, 0, 0));

            // a lista escondida que guarda os dados das teclas (quem aparece sao os chips)
            _lstTeclas.Visible = false;
            _lstTeclas.Size = new Size(1, 1);
            _lstTeclas.Dock = DockStyle.None;
            _inspetor.Controls.Add(_lstTeclas);

            _inspetor.Controls.Add(t);
            return _inspetor;
        }

        private Control ConstruirCabecaDoAlvo()
        {
            TableLayoutPanel g = new TableLayoutPanel();
            g.Dock = DockStyle.Fill;
            g.BackColor = Tema.FundoInspetor;
            g.ColumnCount = 3;
            g.RowCount = 1;
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55f));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 5f));
            g.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _token.CorBorda = Tema.TokenBorda;
            _token.CorFundo = Tema.TokenFundo;
            _token.Raio = 8;
            _token.BackColor = Tema.FundoInspetor;
            _token.Size = new Size(43, 43);
            _token.Margin = new Padding(0);
            _token.Anchor = AnchorStyles.Left;
            _lblToken.Text = "A";
            _lblToken.Font = Tema.Fonte18N;
            _lblToken.ForeColor = Tema.Realce;
            _lblToken.Dock = DockStyle.Fill;
            _lblToken.TextAlign = ContentAlignment.MiddleCenter;
            _lblToken.BackColor = Color.Transparent;
            _token.Controls.Add(_lblToken);
            g.Controls.Add(_token, 0, 0);

            TableLayoutPanel textos = new TableLayoutPanel();
            textos.Dock = DockStyle.Fill;
            textos.BackColor = Tema.FundoInspetor;
            textos.ColumnCount = 1;
            textos.RowCount = 2;
            textos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            textos.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));
            textos.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));

            _lblSelecionado.Text = "Botao A";
            _lblSelecionado.Font = Tema.Fonte12N;
            _lblSelecionado.ForeColor = Tema.Texto;
            _lblSelecionado.Dock = DockStyle.Fill;
            _lblSelecionado.TextAlign = ContentAlignment.BottomLeft;
            _lblSelecionado.BackColor = Tema.FundoInspetor;
            textos.Controls.Add(_lblSelecionado, 0, 0);

            _lblGrupo.Text = "BOTOES DE ACAO";
            _lblGrupo.Font = Tema.Fonte9;
            _lblGrupo.ForeColor = Tema.Texto3;
            _lblGrupo.Dock = DockStyle.Fill;
            _lblGrupo.TextAlign = ContentAlignment.TopLeft;
            _lblGrupo.BackColor = Tema.FundoInspetor;
            _lblGrupo.Padding = new Padding(0, 4, 0, 0);
            textos.Controls.Add(_lblGrupo, 0, 1);
            g.Controls.Add(textos, 1, 0);

            _pontoAlvo.Ligado = true;
            _pontoAlvo.Size = new Size(5, 5);
            _pontoAlvo.Margin = new Padding(0, 8, 0, 0);
            _pontoAlvo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            g.Controls.Add(_pontoAlvo, 2, 0);

            return g;
        }

        private void ConstruirPainelDoEixo()
        {
            _painelDoEixo.BackColor = Tema.FundoInspetor;
            _painelDoEixo.AutoSize = true;
            _painelDoEixo.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _painelDoEixo.Dock = DockStyle.Top;

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top;
            t.AutoSize = true;
            t.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            t.BackColor = Tema.FundoInspetor;
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label valorEixo;
            Linha(t, RotuloSecao("Ajustes do eixo", "SENSIBILIDADE E RAMPAS", out valorEixo), 16f, new Padding(0));
            Linha(t, LinhaBarra("Sensibilidade - valor maximo do eixo", _barraSensibilidade, _numSensibilidade), 46f, new Padding(0, 8, 0, 0));

            _btnAvancado.Dock = DockStyle.Fill;
            _btnAvancado.Margin = new Padding(0, 2, 0, 2);
            Linha(t, _btnAvancado, 30f, new Padding(0));

            _painelAvancado.BackColor = Tema.FundoInspetor;
            _painelAvancado.AutoSize = true;
            _painelAvancado.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _painelAvancado.Dock = DockStyle.Top;

            TableLayoutPanel av = new TableLayoutPanel();
            av.Dock = DockStyle.Top;
            av.AutoSize = true;
            av.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            av.BackColor = Tema.FundoInspetor;
            av.ColumnCount = 1;
            av.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            Linha(av, LinhaBarra("Rampa de ida (ms) - 0 = resposta no mesmo tique", _barraRampaIda, _numRampaIda), 46f, new Padding(0));
            Linha(av, LinhaBarra("Rampa de volta (ms)", _barraRampaVolta, _numRampaVolta), 46f, new Padding(0));
            Linha(av, _chkMesmaVolta, 26f, new Padding(0, 4, 0, 0));
            _painelAvancado.Controls.Add(av);
            _painelAvancado.Visible = false;
            Linha(t, _painelAvancado, 0f, new Padding(0));

            _painelDoEixo.Controls.Add(t);
        }

        private static Control LinhaBarra(string rotulo, BarraDeslizante barra, NumericUpDown numero)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Tema.FundoInspetor;
            p.Margin = new Padding(0);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.FundoInspetor;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 17f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 27f));

            Label l = new Label();
            l.Text = rotulo;
            l.Dock = DockStyle.Fill;
            l.Font = Tema.Fonte75;
            l.ForeColor = Tema.Texto2;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.BackColor = Tema.FundoInspetor;
            t.Controls.Add(l, 0, 0);

            TableLayoutPanel baixo = new TableLayoutPanel();
            baixo.Dock = DockStyle.Fill;
            baixo.BackColor = Tema.FundoInspetor;
            baixo.ColumnCount = 2;
            baixo.RowCount = 1;
            baixo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            baixo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78f));
            baixo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            barra.Dock = DockStyle.Fill;
            barra.Margin = new Padding(0, 2, 8, 2);
            numero.Margin = new Padding(0, 2, 0, 2);
            baixo.Controls.Add(barra, 0, 0);
            baixo.Controls.Add(numero, 1, 0);
            t.Controls.Add(baixo, 0, 1);

            p.Controls.Add(t);
            return p;
        }

        private void ConstruirCaixaSemTecla()
        {
            _painelUnmapped.CorBorda = Tema.AvisoBorda;
            _painelUnmapped.CorFundo = Tema.AvisoFundo;
            _painelUnmapped.Raio = 2;
            _painelUnmapped.BackColor = Tema.FundoInspetor;
            _painelUnmapped.Padding = new Padding(12);
            _painelUnmapped.AutoSize = true;
            _painelUnmapped.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _painelUnmapped.Dock = DockStyle.Top;

            TableLayoutPanel u = new TableLayoutPanel();
            u.Dock = DockStyle.Top;
            u.AutoSize = true;
            u.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            u.BackColor = Tema.AvisoFundo;
            u.ColumnCount = 1;
            u.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Panel titulo = new Panel();
            titulo.Dock = DockStyle.Top;
            titulo.AutoSize = true;
            titulo.Height = 14;
            titulo.BackColor = Tema.AvisoFundo;
            _pontoResumo.Ligado = true;
            _pontoResumo.ComBrilho = false;
            _pontoResumo.CorLigado = Tema.Amarelo;
            _pontoResumo.Size = new Size(5, 5);
            _pontoResumo.Location = new Point(0, 5);
            titulo.Controls.Add(_pontoResumo);
            _lblResumoTitulo.Text = "0 alvos sem tecla";
            _lblResumoTitulo.Font = Tema.Fonte9;
            _lblResumoTitulo.ForeColor = Tema.Amarelo;
            _lblResumoTitulo.AutoSize = true;
            _lblResumoTitulo.BackColor = Tema.AvisoFundo;
            _lblResumoTitulo.Location = new Point(12, 1);
            titulo.Controls.Add(_lblResumoTitulo);
            u.Controls.Add(titulo);

            _lblResumo.Text = "";
            _lblResumo.Font = Tema.Fonte9;
            _lblResumo.ForeColor = Tema.AvisoTexto;
            _lblResumo.AutoSize = true;
            _lblResumo.MaximumSize = new Size(215, 0);
            _lblResumo.BackColor = Tema.AvisoFundo;
            _lblResumo.Dock = DockStyle.Top;
            _lblResumo.Margin = new Padding(0, 8, 0, 0);
            u.Controls.Add(_lblResumo);

            _painelUnmapped.Controls.Add(u);
        }

        private Control ConstruirVivo()
        {
            TableLayoutPanel v = new TableLayoutPanel();
            v.Dock = DockStyle.Top;
            v.AutoSize = true;
            v.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            v.BackColor = Tema.FundoInspetor;
            v.ColumnCount = 1;
            v.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label valorVivo;
            Linha(v, RotuloSecao("Teste ao vivo", "XINPUT", out valorVivo), 16f, new Padding(0));

            TableLayoutPanel estado = new TableLayoutPanel();
            estado.Dock = DockStyle.Top;
            estado.AutoSize = true;
            estado.BackColor = Tema.FundoInspetor;
            estado.ColumnCount = 2;
            estado.RowCount = 1;
            estado.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6f));
            estado.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            estado.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _pontoVivo.Size = new Size(6, 6);
            _pontoVivo.Dock = DockStyle.Fill;
            _pontoVivo.Margin = new Padding(0, 0, 8, 0);
            estado.Controls.Add(_pontoVivo, 0, 0);
            _lblVivoEstado.Text = "Aguardando conexao";
            _lblVivoEstado.Font = Tema.Fonte10;
            _lblVivoEstado.ForeColor = Tema.Texto2;
            _lblVivoEstado.AutoSize = true;
            _lblVivoEstado.BackColor = Tema.FundoInspetor;
            estado.Controls.Add(_lblVivoEstado, 1, 0);
            Linha(v, estado, 36f, new Padding(0, 16, 0, 0));

            _lblVivoMensagem.Text = "Ligue o pad no botao 'Ligar o pad' e espere alguns segundos.";
            _lblVivoMensagem.Font = Tema.Fonte9;
            _lblVivoMensagem.ForeColor = Tema.Texto3;
            _lblVivoMensagem.AutoSize = true;
            _lblVivoMensagem.MaximumSize = new Size(240, 0);
            _lblVivoMensagem.BackColor = Tema.FundoInspetor;
            _lblVivoMensagem.Dock = DockStyle.Top;
            LinhaAuto(v, _lblVivoMensagem, new Padding(0, 10, 0, 0));

            _caixaVivo.CorBorda = Color.FromArgb(0x30, 0x3b, 0x40);
            _caixaVivo.CorFundo = Color.Empty;
            _caixaVivo.Raio = 4;
            _caixaVivo.Tracejada = true;
            _caixaVivo.BackColor = Tema.FundoInspetor;
            _caixaVivo.Height = 39;
            _caixaVivo.Dock = DockStyle.Top;
            _lblVivoEvento.Text = "— nenhum comando —";
            _lblVivoEvento.Font = Tema.Fonte9;
            _lblVivoEvento.ForeColor = Tema.Texto3;
            _lblVivoEvento.Dock = DockStyle.Fill;
            _lblVivoEvento.TextAlign = ContentAlignment.MiddleCenter;
            _lblVivoEvento.BackColor = Color.Transparent;
            _caixaVivo.Controls.Add(_lblVivoEvento);
            Linha(v, _caixaVivo, 52f, new Padding(0, 13, 0, 0));

            Label integracao = new Label();
            integracao.Text = "O teste le o que o Windows ve (XInput); o pad precisa estar ligado.";
            integracao.Font = Tema.Fonte8;
            integracao.ForeColor = Tema.Texto3;
            integracao.AutoSize = true;
            integracao.MaximumSize = new Size(240, 0);
            integracao.BackColor = Tema.FundoInspetor;
            integracao.Dock = DockStyle.Top;
            LinhaAuto(v, integracao, new Padding(0, 10, 0, 0));

            return v;
        }

        // -------------------------------------------------------------------------------------
        // rodape: recarregar/salvar, reiniciar, ligar o pad e a linha de recado
        // -------------------------------------------------------------------------------------

        private Control ConstruirRodape()
        {
            Panel fundo = new Panel();
            fundo.Dock = DockStyle.Fill;
            fundo.BackColor = Tema.FundoRodape;
            fundo.Padding = new Padding(20, 0, 20, 0);
            fundo.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(p, 0, 0, fundo.ClientSize.Width, 0);
            };

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.FundoRodape;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 23f));

            TableLayoutPanel acoes = new TableLayoutPanel();
            acoes.Dock = DockStyle.Fill;
            acoes.BackColor = Tema.FundoRodape;
            acoes.ColumnCount = 3;
            acoes.RowCount = 1;
            acoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            acoes.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            acoes.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            acoes.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            FlowLayoutPanel esquerda = new FlowLayoutPanel();
            esquerda.Dock = DockStyle.Fill;
            esquerda.BackColor = Tema.FundoRodape;
            esquerda.FlowDirection = FlowDirection.LeftToRight;
            esquerda.WrapContents = false;
            esquerda.Margin = new Padding(0);
            esquerda.Padding = new Padding(0);

            _btnRecarregar.IconeEsquerda = "↻";
            _btnRecarregar.Texto = "Recarregar";
            _btnRecarregar.IconeDireita = "";
            _btnRecarregar.FonteEsq = Tema.Simbolo11;
            _btnRecarregar.FonteTexto = Tema.Fonte10;
            _btnRecarregar.Fundo = Color.Empty;
            _btnRecarregar.CorBorda = Tema.Borda;
            _btnRecarregar.TextoCor = Tema.Texto2;
            _btnRecarregar.TextoCorHover = Tema.Texto;
            _btnRecarregar.CorEsq = Tema.Texto2;
            _btnRecarregar.BackColor = Tema.FundoRodape;
            _btnRecarregar.Size = new Size(124, 34);
            _btnRecarregar.Margin = new Padding(0, 5, 7, 0);
            _btnRecarregar.Clicar += delegate { RecarregarDoDisco(); };
            esquerda.Controls.Add(_btnRecarregar);

            _btnSalvar.IconeEsquerda = "↓";
            _btnSalvar.Texto = "Salvar JSON";
            _btnSalvar.IconeDireita = "";
            _btnSalvar.FonteEsq = Tema.Simbolo11;
            _btnSalvar.FonteTexto = Tema.Fonte10;
            _btnSalvar.Fundo = Tema.SalvarFundo;
            _btnSalvar.FundoHover = Tema.PowerFundo;
            _btnSalvar.CorBorda = Tema.SalvarBorda;
            _btnSalvar.TextoCor = Tema.Realce;
            _btnSalvar.TextoCorHover = Tema.Realce;
            _btnSalvar.CorEsq = Tema.Realce;
            _btnSalvar.BackColor = Tema.FundoRodape;
            _btnSalvar.Size = new Size(136, 34);
            _btnSalvar.Margin = new Padding(0, 5, 7, 0);
            _btnSalvar.Clicar += delegate { Salvar(); };
            esquerda.Controls.Add(_btnSalvar);

            _lblSalvoEstado.Text = "✓ Sem alteracoes";
            _lblSalvoEstado.Font = Tema.Fonte8;
            _lblSalvoEstado.ForeColor = Tema.Texto3;
            _lblSalvoEstado.AutoSize = true;
            _lblSalvoEstado.BackColor = Tema.FundoRodape;
            _lblSalvoEstado.Margin = new Padding(4, 15, 0, 0);
            esquerda.Controls.Add(_lblSalvoEstado);
            acoes.Controls.Add(esquerda, 0, 0);

            _chkReiniciar.AutoSize = true;
            _chkReiniciar.Anchor = AnchorStyles.Right;
            _chkReiniciar.Margin = new Padding(0, 0, 10, 0);
            acoes.Controls.Add(_chkReiniciar, 1, 0);

            _btnLigarPad.Text = "Ligar o pad";
            _btnLigarPad.FlatStyle = FlatStyle.Flat;
            _btnLigarPad.UseVisualStyleBackColor = false;
            _btnLigarPad.FlatAppearance.BorderSize = 1;
            _btnLigarPad.BackColor = Tema.Realce;
            _btnLigarPad.ForeColor = Tema.PowerTexto;
            _btnLigarPad.FlatAppearance.BorderColor = Tema.Realce;
            _btnLigarPad.FlatAppearance.MouseOverBackColor = Tema.BotaoClaroHover;
            _btnLigarPad.Font = Tema.Fonte10;
            _btnLigarPad.Size = new Size(165, 40);
            _btnLigarPad.Anchor = AnchorStyles.Right;
            _btnLigarPad.Margin = new Padding(0);
            _btnLigarPad.TabStop = false;
            _btnLigarPad.Cursor = Cursors.Hand;
            _btnLigarPad.Click += delegate { AlternarPad(); };
            acoes.Controls.Add(_btnLigarPad, 2, 0);
            t.Controls.Add(acoes, 0, 0);

            Panel status = new Panel();
            status.Dock = DockStyle.Fill;
            status.BackColor = Tema.FundoRodape;
            status.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Tema.LinhaStatus))
                    e.Graphics.DrawLine(p, 0, 0, status.ClientSize.Width, 0);
            };
            TableLayoutPanel linhaStatus = new TableLayoutPanel();
            linhaStatus.Dock = DockStyle.Fill;
            linhaStatus.BackColor = Tema.FundoRodape;
            linhaStatus.ColumnCount = 2;
            linhaStatus.RowCount = 1;
            linhaStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            linhaStatus.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            linhaStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _lblStatus.Text = "pronto.";
            _lblStatus.Font = Tema.Fonte8;
            _lblStatus.ForeColor = Tema.Texto3;
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblStatus.AutoEllipsis = true;
            _lblStatus.BackColor = Tema.FundoRodape;
            linhaStatus.Controls.Add(_lblStatus, 0, 0);

            _lblRodape.Text = "INTERFACE LOCAL  /  v2.0";
            _lblRodape.Font = Tema.Fonte8;
            _lblRodape.ForeColor = Tema.Texto3;
            _lblRodape.AutoSize = true;
            _lblRodape.Dock = DockStyle.Fill;
            _lblRodape.TextAlign = ContentAlignment.MiddleRight;
            _lblRodape.BackColor = Tema.FundoRodape;
            linhaStatus.Controls.Add(_lblRodape, 1, 0);
            status.Controls.Add(linhaStatus);
            t.Controls.Add(status, 0, 1);

            fundo.Controls.Add(t);
            return fundo;
        }

        // -------------------------------------------------------------------------------------
        // menu da ajuda (fica no botao "AJUDA", no topo; substitui a barra de menu antiga)
        // -------------------------------------------------------------------------------------

        private void MostrarMenuDeAjuda()
        {
            if (_menuAjuda == null)
            {
                ContextMenuStrip m = new ContextMenuStrip();
                m.Renderer = new MenuEscuro();
                m.BackColor = Tema.Painel2;
                m.ForeColor = Tema.Texto;
                m.Font = Tema.Fonte10;
                m.ShowImageMargin = false;
                m.Items.Add(NovoItem("Como usar", MostrarComoUsar));
                m.Items.Add(NovoItem("Sobre", MostrarSobre));
                m.Items.Add(new ToolStripSeparator());
                m.Items.Add(NovoItem("Sair", delegate { Close(); }));
                _menuAjuda = m;
            }
            _menuAjuda.Show(_lblAjuda, new Point(0, _lblAjuda.Height + 4));
        }

        private static ToolStripMenuItem NovoItem(string texto, System.Action aoClicar)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(texto);
            item.ForeColor = Tema.Texto;
            item.BackColor = Tema.Painel2;
            item.Click += delegate { aoClicar(); };
            return item;
        }

        // -------------------------------------------------------------------------------------
        // ajudantes de leiaute e de conteudo
        // -------------------------------------------------------------------------------------

        private static Panel RotuloSecao(string esquerda, string direita, out Label valor)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Color.Transparent;
            p.Margin = new Padding(0);

            Label esq = new Label();
            esq.Text = esquerda.ToUpperInvariant();
            esq.AutoSize = true;
            esq.ForeColor = Tema.Texto3;
            esq.Font = Tema.Fonte9;
            esq.BackColor = Color.Transparent;
            esq.Dock = DockStyle.Left;
            esq.TextAlign = ContentAlignment.MiddleLeft;
            p.Controls.Add(esq);

            valor = null;
            if (!string.IsNullOrEmpty(direita))
            {
                valor = new Label();
                valor.Text = direita;
                valor.AutoSize = true;
                valor.ForeColor = Tema.Texto3;
                valor.Font = Tema.Fonte9;
                valor.BackColor = Color.Transparent;
                valor.Dock = DockStyle.Right;
                valor.TextAlign = ContentAlignment.MiddleRight;
                p.Controls.Add(valor);
            }
            return p;
        }

        private static void Linha(TableLayoutPanel t, Control c, float altura, Padding margem)
        {
            int i = t.RowCount;
            t.RowCount = i + 1;
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, altura));
            c.Margin = margem;
            t.Controls.Add(c, 0, i);
        }

        private static void LinhaAuto(TableLayoutPanel t, Control c, Padding margem)
        {
            int i = t.RowCount;
            t.RowCount = i + 1;
            t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            c.Margin = margem;
            t.Controls.Add(c, 0, i);
        }

        private static string GrupoDoAlvo(DesenhoControle.Alvo a)
        {
            if (a == null) return "";
            switch (a.Nome)
            {
                case "A":
                case "B":
                case "X":
                case "Y": return "Botoes de acao";
                case "LB":
                case "RB":
                case "LT":
                case "RT": return "Ombros e gatilhos";
                case "UP":
                case "DOWN":
                case "LEFT":
                case "RIGHT": return "Direcional";
                case "LS":
                case "RS": return "Analogicos (clique)";
                case "LX":
                case "LY":
                case "RX":
                case "RY": return "Analogicos";
                case "BACK":
                case "START": return "Sistema";
                default: return "Outros";
            }
        }

        private DesenhoControle.Alvo AlvoPorChave(string chave)
        {
            foreach (DesenhoControle.Alvo a in _desenho.Alvos)
                if (string.Equals(a.Chave, chave, StringComparison.Ordinal)) return a;
            return null;
        }

        private string TeclasDoAlvo(DesenhoControle.Alvo a)
        {
            Perfil p = PerfilAtual();
            if (p == null) return "—";
            string lista = null;
            foreach (Mapeamento m in p.Mapeamentos)
            {
                if (!CasaComAlvo(m, a)) continue;
                string nome = GravadorPerfil.NomeDeTecla(m) ?? m.TeclaNome ?? "?";
                lista = lista == null ? nome : lista + " / " + nome;
            }
            return lista ?? "—";
        }

        private void MontarChips()
        {
            _painelTeclas.Controls.Clear();
            for (int i = 0; i < _lstTeclas.Items.Count; i++)
            {
                int indice = i;
                ChipDeTecla c = new ChipDeTecla((string)_lstTeclas.Items[i]);
                c.Clicou += delegate { _lstTeclas.SelectedIndex = indice; };
                c.Removeu += delegate { _lstTeclas.SelectedIndex = indice; RemoverTecla(null, EventArgs.Empty); };
                _painelTeclas.Controls.Add(c);
            }
            _lblQtdTeclas.Text = _lstTeclas.Items.Count.ToString("D2");
            AtualizarChipsAtivos();
        }

        private void AtualizarChipsAtivos()
        {
            int sel = _lstTeclas.SelectedIndex;
            for (int i = 0; i < _painelTeclas.Controls.Count; i++)
            {
                ChipDeTecla c = _painelTeclas.Controls[i] as ChipDeTecla;
                if (c != null) c.Ativo = (i == sel);
            }
        }

        private void MontarListaAlvos()
        {
            string chaveAtual = _desenho.Selecionado;
            _lstAlvos.BeginUpdate();
            _lstAlvos.Items.Clear();
            foreach (DesenhoControle.Alvo a in _desenho.Alvos) _lstAlvos.Items.Add(a.Chave);
            int indice = -1;
            for (int i = 0; i < _lstAlvos.Items.Count; i++)
                if (string.Equals((string)_lstAlvos.Items[i], chaveAtual, StringComparison.Ordinal)) { indice = i; break; }
            if (indice >= 0) _lstAlvos.SelectedIndex = indice;
            _lstAlvos.EndUpdate();
        }

        private void TrocarAba(bool lista)
        {
            _abaControle.Ativo = !lista;
            _abaLista.Ativo = lista;
            _painelControle.Visible = !lista;
            _painelLista.Visible = lista;
            if (lista) MontarListaAlvos();
        }

        private void AtualizarContadores()
        {
            Perfil p = PerfilAtual();
            int total = _desenho.Alvos.Count;
            int mapeados = 0;
            if (p != null)
            {
                foreach (DesenhoControle.Alvo a in _desenho.Alvos)
                {
                    foreach (Mapeamento m in p.Mapeamentos)
                        if (CasaComAlvo(m, a)) { mapeados++; break; }
                }
            }
            _lblTotalAlvos.Text = mapeados + " / " + total + " mapeados";
            _lblQtdPerfis.Text = (_arquivo != null ? _arquivo.Perfis.Count : 0).ToString("D2");
        }

        private void DesenharAlvo(object remetente, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstAlvos.Items.Count) return;

            string chave = (string)_lstAlvos.Items[e.Index];
            DesenhoControle.Alvo a = AlvoPorChave(chave);
            bool escolhido = (e.State & DrawItemState.Selected) != 0;
            Rectangle r = e.Bounds;

            using (SolidBrush fundo = new SolidBrush(escolhido ? Tema.RealceEscuro : Tema.Fundo))
                e.Graphics.FillRectangle(fundo, r);
            if (escolhido)
                using (SolidBrush marca = new SolidBrush(Tema.Realce))
                    e.Graphics.FillRectangle(marca, r.Left, r.Top, 2, r.Height);

            using (StringFormat meio = new StringFormat { LineAlignment = StringAlignment.Center })
            {
                using (SolidBrush tinta = new SolidBrush(Tema.Texto3))
                    e.Graphics.DrawString(GrupoDoAlvo(a).ToUpperInvariant(), Tema.Fonte8, tinta,
                                          new Rectangle(r.Left + 14, r.Top, 104, r.Height), meio);

                using (SolidBrush tinta = new SolidBrush(escolhido ? Tema.Realce : Tema.Texto))
                    e.Graphics.DrawString(a != null ? a.NomePorExtenso : chave, Tema.Fonte10, tinta,
                                          new Rectangle(r.Left + 124, r.Top, r.Width - 124 - 150, r.Height), meio);

                string teclas = TeclasDoAlvo(a);
                using (SolidBrush tinta = new SolidBrush(teclas == "—" ? Tema.Texto3 : (escolhido ? Tema.Realce : Tema.Texto2)))
                using (StringFormat direita = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(teclas, Tema.Fonte10, tinta,
                                          new Rectangle(r.Left, r.Top, r.Width - 14, r.Height), direita);
            }

            using (Pen p = new Pen(Tema.Borda))
                e.Graphics.DrawLine(p, r.Left + 14, r.Bottom - 1, r.Right - 14, r.Bottom - 1);
        }

        // =====================================================================================
        // eventos
        // =====================================================================================

        private void LigarEventos()
        {
            _lstPerfis.DrawItem += DesenharPerfil;
            _lstPerfis.SelectedIndexChanged += delegate { TrocarDePerfil(); };

            _desenho.Selecionou += delegate { MostrarAlvo(); };

            _lstTeclas.SelectedIndexChanged += delegate { MostrarParametros(); AtualizarChipsAtivos(); };

            _lstAlvos.DrawItem += DesenharAlvo;
            _lstAlvos.SelectedIndexChanged += delegate
            {
                if (_carregando) return;
                if (_lstAlvos.SelectedIndex < 0 || _lstAlvos.SelectedIndex >= _lstAlvos.Items.Count) return;
                _desenho.Selecionado = (string)_lstAlvos.Items[_lstAlvos.SelectedIndex];
                MostrarAlvo();
            };

            _btnCapturar.Clicar += delegate { CapturarTecla(false); };
            _btnTeclaExtra.Click += delegate { CapturarTecla(true); };
            _btnRemover.Click += RemoverTecla;
            _btnAvancado.Click += delegate
            {
                _painelAvancado.Visible = !_painelAvancado.Visible;
                _btnAvancado.Text = _painelAvancado.Visible ? "Avancado  ▲" : "Avancado  ▼";
            };

            _btnNovoPerfil.Clicar += delegate { CriarPerfil(); };
            _btnDuplicar.Click += delegate { DuplicarPerfil(); };
            _btnRenomear.Click += delegate { RenomearPerfil(); };
            _btnExcluir.Click += delegate { ExcluirPerfil(); };
            _btnAjustes.Clicar += delegate { AbrirAjustes(); };
            _btnAtalhos.Clicar += delegate { AbrirAtalhos(); };

            _btnAbrir.Click += delegate { EscolherArquivo(); };
            _btnRecarregar.Clicar += delegate { RecarregarDoDisco(); };
            _btnSalvar.Clicar += delegate { Salvar(); };
            _btnLigarPad.Click += delegate { AlternarPad(); };

            _chkInicial.CheckedChanged += delegate
            {
                if (_carregando || _arquivo == null) return;
                string nome = NomeDoPerfilSelecionado();
                if (nome == null) return;
                _arquivo.PerfilInicial = nome;
                _sujo = true;
                _lstPerfis.Invalidate();
                MarcarTitulo();
                _lblStatus.Text = "o pad vai comecar pelo perfil '" + nome + "'.";
            };

            _chkMesmaVolta.CheckedChanged += delegate
            {
                _barraRampaVolta.Enabled = !_chkMesmaVolta.Checked;
                _numRampaVolta.Enabled = !_chkMesmaVolta.Checked;
                if (!_carregando) GravarParametros();
            };

            LigarBarra(_barraSensibilidade, _numSensibilidade, delegate { GravarParametros(); });
            LigarBarra(_barraRampaIda, _numRampaIda, delegate { GravarParametros(); });
            LigarBarra(_barraRampaVolta, _numRampaVolta, delegate { GravarParametros(); });

            FormClosing += AoFechar;
        }

        /// <summary>
        /// Liga uma barra deslizante a uma caixa de numero: quem mexer num lado copia para o outro.
        /// A trava _sincronizando evita que a copia chame de volta o outro lado (laco infinito).
        /// </summary>
        private void LigarBarra(BarraDeslizante barra, NumericUpDown numero, EventHandler aoMexer)
        {
            barra.ValorMudou += delegate
            {
                if (_sincronizando) return;
                _sincronizando = true;
                try { numero.Value = Limitar(barra.Valor, (int)numero.Minimum, (int)numero.Maximum); }
                finally { _sincronizando = false; }
                aoMexer(null, EventArgs.Empty);
            };

            numero.ValueChanged += delegate
            {
                if (_sincronizando) return;
                _sincronizando = true;
                try { barra.Valor = (int)numero.Value; }
                finally { _sincronizando = false; }
                aoMexer(null, EventArgs.Empty);
            };
        }

        private static int Limitar(int v, int minimo, int maximo)
        {
            if (v < minimo) return minimo;
            if (v > maximo) return maximo;
            return v;
        }

        // =====================================================================================
        // perfil na tela
        // =====================================================================================

        private string NomeDoPerfilSelecionado()
        {
            if (_lstPerfis.SelectedIndex < 0 || _lstPerfis.SelectedIndex >= _lstPerfis.Items.Count) return null;
            return (string)_lstPerfis.Items[_lstPerfis.SelectedIndex];
        }

        private Perfil PerfilAtual()
        {
            if (_arquivo == null) return null;
            string nome = _perfilNaTela;
            if (nome == null) return null;
            Perfil p;
            return _arquivo.Perfis.TryGetValue(nome, out p) ? p : null;
        }

        private DesenhoControle.Alvo AlvoSelecionado()
        {
            foreach (DesenhoControle.Alvo a in _desenho.Alvos)
                if (string.Equals(a.Chave, _desenho.Selecionado, StringComparison.Ordinal)) return a;
            return null;
        }

        private static bool CasaComAlvo(Mapeamento m, DesenhoControle.Alvo a)
        {
            if (m == null || a == null) return false;
            if (a.Sentido == 0)
                return m.Tipo == TipoMapeamento.Botao &&
                       string.Equals(m.AlvoBotao.ToString(), a.Nome, StringComparison.Ordinal);
            return m.Tipo == TipoMapeamento.Eixo &&
                   string.Equals(m.AlvoEixo.ToString(), a.Nome, StringComparison.Ordinal) &&
                   m.Sentido == a.Sentido;
        }

        private static string ChaveDoMapa(Mapeamento m)
        {
            if (m == null) return null;
            if (m.Tipo == TipoMapeamento.Botao) return m.AlvoBotao.ToString();
            return m.AlvoEixo.ToString() + (m.Sentido > 0 ? "+" : "-");
        }

        /// <summary>Poe nas caixinhas do desenho a tecla de cada alvo do perfil que esta na tela.</summary>
        private void AtualizarDesenho()
        {
            Dictionary<string, string> teclas = new Dictionary<string, string>(StringComparer.Ordinal);
            Perfil p = PerfilAtual();
            if (p != null)
            {
                foreach (Mapeamento m in p.Mapeamentos)
                {
                    string chave = ChaveDoMapa(m);
                    if (chave == null) continue;
                    string texto = GravadorPerfil.NomeDeTecla(m) ?? m.TeclaNome ?? "?";
                    string antigo;
                    teclas[chave] = teclas.TryGetValue(chave, out antigo) ? antigo + " / " + texto : texto;
                }
            }
            _desenho.DefinirTeclas(teclas);
        }

        private void TrocarDePerfil()
        {
            if (_carregando) return;
            MostrarPerfilNaTela();
            string nome = NomeDoPerfilSelecionado();
            if (nome != null) _lblStatus.Text = "perfil '" + nome + "' na tela. O que voce mexer aqui escreve direto no arquivo (use Salvar).";
        }

        private void MostrarPerfilNaTela()
        {
            if (_arquivo == null) return;

            _carregando = true;
            try
            {
                _perfilNaTela = NomeDoPerfilSelecionado();
                string nome = _perfilNaTela;
                _chkInicial.Checked = nome != null && string.Equals(_arquivo.PerfilInicial, nome, StringComparison.OrdinalIgnoreCase);
                _btnExcluir.Enabled = _arquivo.Perfis.Count > 1;
            }
            finally { _carregando = false; }

            _lstPerfis.Invalidate();
            _lblPerfilTitulo.Text = _perfilNaTela ?? "(nenhum perfil)";
            _lblPerfilBadge.Text = (_perfilNaTela != null && string.Equals(_arquivo.PerfilInicial, _perfilNaTela, StringComparison.OrdinalIgnoreCase))
                ? "PERFIL INICIAL" : "PERFIL ATUAL";
            MontarListaAlvos();
            AtualizarContadores();
            AtualizarDesenho();
            MostrarAlvo();
            MarcarTitulo();
            AtualizarEstadoDoPad();
        }

        // =====================================================================================
        // alvo escolhido: lista de teclas e ajustes do eixo
        // =====================================================================================

        private Mapeamento MapaSelecionado()
        {
            int i = _lstTeclas.SelectedIndex;
            if (i < 0 || i >= _mapasNaLista.Count) return null;
            return _mapasNaLista[i];
        }

        /// <summary>
        /// Altura da faixa do painel do eixo. Fechado, cabem so o rotulo da sensibilidade (46) e o botao
        /// "Avancado" (30), com folga. Aberto, cabem tambem as duas rampas (46 + 46) e o "igual a rampa
        /// de ida" (26) - isto e, 46 + 30 + 118 = 194, mais folga. Antes esta conta nao era feita e o
        /// checkbox das rampas ficava CORTADO fora da tela (defeito visto na foto de 2026-10-18 14:41).
        /// </summary>
        private float AlturaDoPainelDoEixo()
        {
            return _painelAvancado.Visible ? 198f : 80f;
        }

        private bool TituloDoAlvoCabeNumaLinha(string texto, Font fonte)
        {
            int largura = _lblSelecionado.IsHandleCreated && _lblSelecionado.ClientSize.Width > 80
                ? _lblSelecionado.ClientSize.Width : 196;
            return TextRenderer.MeasureText(texto, fonte).Width <= largura;
        }

        private void MostrarAlvo()
        {
            DesenhoControle.Alvo a = AlvoSelecionado();

            _carregando = true;
            try
            {
                _mapasNaLista.Clear();
                _lstTeclas.Items.Clear();

                if (a == null)
                {
                    _lblSelecionado.Text = "(escolha um alvo no controle)";
                    _lblGrupo.Text = "";
                    _lblToken.Text = "?";
                    _lblDirecao.Text = "";
                    _pontoAlvo.Ligado = false;
                    _painelDoEixo.Visible = false;
                    _btnCapturar.Enabled = false;
                    _btnTeclaExtra.Enabled = false;
                    _btnRemover.Enabled = false;
                    _lblComportamentoNota.Text = "";
                    MontarChips();
                    return;
                }

                _lblSelecionado.Text = a.NomePorExtenso;
                // Titulo de eixo e comprido ("Analogico esquerdo - cima.") e nao cabia numa linha de
                // 16 px: quebrava em duas e a segunda saia cortada (foto janela-eixo.png, 2026-10-21).
                // Regra: 16 px se entrar num linha, senao 11 px medio (o inspetor tem ~196 px para ele).
                _lblSelecionado.Font = TituloDoAlvoCabeNumaLinha(a.NomePorExtenso, Tema.Fonte12N)
                    ? Tema.Fonte12N : Tema.Fonte11M;
                _lblGrupo.Text = GrupoDoAlvo(a).ToUpperInvariant();
                _lblToken.Text = a.Chave;
                _pontoAlvo.Ligado = true;
                bool eixo = a.Sentido != 0;
                if (!eixo)
                    _lblDirecao.Text = "A tecla do teclado aciona este botao no controle virtual; ao soltar, ele solta.";
                else if (a.Nome == "LT" || a.Nome == "RT")
                    _lblDirecao.Text = "A tecla empurra o gatilho de 0 ate o valor maximo.";
                else
                    _lblDirecao.Text = "A tecla empurra o analogico " +
                                       (a.Sentido > 0 ? "para cima/direita." : "para baixo/esquerda.");

                Perfil p = PerfilAtual();
                if (p != null)
                {
                    foreach (Mapeamento m in p.Mapeamentos)
                    {
                        if (!CasaComAlvo(m, a)) continue;
                        _mapasNaLista.Add(m);
                        _lstTeclas.Items.Add(GravadorPerfil.NomeDeTecla(m) ?? m.TeclaNome ?? "?");
                    }
                }

                _painelDoEixo.Visible = eixo;
                _btnCapturar.Enabled = true;
                _btnTeclaExtra.Enabled = true;
                _btnRemover.Enabled = _mapasNaLista.Count > 0;

                if (_lstTeclas.Items.Count > 0) _lstTeclas.SelectedIndex = 0;
            }
            finally { _carregando = false; }

            MontarChips();
            MostrarParametros();
            EscreverDica();
            EscreverResumo();
            AtualizarContadores();
        }

        /// <summary>Resumo de completude: quais dos 24 alvos ainda estao sem tecla neste perfil.</summary>
        private void EscreverResumo()
        {
            Perfil p = PerfilAtual();
            if (p == null)
            {
                _lblResumoTitulo.Text = "";
                _lblResumo.Text = "";
                _painelUnmapped.Visible = false;
                return;
            }

            List<string> sem = new List<string>();
            foreach (DesenhoControle.Alvo a in _desenho.Alvos)
            {
                bool tem = false;
                foreach (Mapeamento m in p.Mapeamentos)
                    if (CasaComAlvo(m, a)) { tem = true; break; }
                if (!tem) sem.Add(a.Rotulo);
            }

            if (sem.Count == 0)
            {
                _painelUnmapped.Visible = false;
                return;
            }

            _painelUnmapped.Visible = true;
            _lblResumoTitulo.Text = sem.Count + (sem.Count == 1 ? " alvo sem tecla" : " alvos sem tecla");
            _lblResumo.Text = string.Join(", ", sem.ToArray());
        }

        /// <summary>Poe nas barras o que esta no modelo (nao escreve nada).</summary>
        private void MostrarParametros()
        {
            DesenhoControle.Alvo a = AlvoSelecionado();
            Mapeamento m = MapaSelecionado();

            int minimo = 1, maximo = 32767;
            if (a != null && a.Sentido != 0)
            {
                Eixo e;
                Enum.TryParse(a.Nome, true, out e);
                maximo = Faixas.EhGatilho(e) ? 255 : 32767;
            }

            int valorMaximo = m != null ? m.ValorMaximo : (a != null && a.Sentido != 0 ? ValorMaximoPadraoDe(a) : maximo);
            int rampaIda = m != null ? m.RampaMs : 150;
            int rampaVolta = (m != null && m.RampaVoltaMs >= 0) ? m.RampaVoltaMs : rampaIda;
            bool mesma = m == null || m.RampaVoltaMs < 0;
            bool temMapa = m != null;

            _sincronizando = true;
            try
            {
                _barraSensibilidade.Faixa(minimo, maximo, Limitar(valorMaximo, minimo, maximo));
                _numSensibilidade.Minimum = minimo;
                _numSensibilidade.Maximum = maximo;
                _numSensibilidade.Increment = maximo <= 255 ? 1 : 64;
                _numSensibilidade.Value = Limitar(valorMaximo, minimo, maximo);

                _barraRampaIda.Faixa(0, 2000, Limitar(rampaIda, 0, 2000));
                _numRampaIda.Value = Limitar(rampaIda, 0, 2000);
                _barraRampaVolta.Faixa(0, 2000, Limitar(rampaVolta, 0, 2000));
                _numRampaVolta.Value = Limitar(rampaVolta, 0, 2000);

                _chkMesmaVolta.Checked = mesma;

                _barraSensibilidade.Enabled = temMapa;
                _numSensibilidade.Enabled = temMapa;
                _barraRampaIda.Enabled = temMapa;
                _numRampaIda.Enabled = temMapa;
                _barraRampaVolta.Enabled = temMapa && !mesma;
                _numRampaVolta.Enabled = temMapa && !mesma;
                _chkMesmaVolta.Enabled = temMapa;
                _btnAvancado.Enabled = temMapa;
            }
            finally { _sincronizando = false; }
        }

        private static int ValorMaximoPadraoDe(DesenhoControle.Alvo a)
        {
            Eixo e;
            if (a != null && Enum.TryParse(a.Nome, true, out e)) return Faixas.ValorMaximoPadrao(e);
            return 32767;
        }

        /// <summary>Escreve no modelo o que esta nas barras do painel do eixo.</summary>
        private void GravarParametros()
        {
            if (_carregando || _sincronizando) return;

            DesenhoControle.Alvo a = AlvoSelecionado();
            Mapeamento m = MapaSelecionado();
            if (a == null || m == null || m.Tipo != TipoMapeamento.Eixo) return;

            int valorNovo = (int)_numSensibilidade.Value;
            int idaNova = (int)_numRampaIda.Value;
            int voltaNova = _chkMesmaVolta.Checked ? -1 : (int)_numRampaVolta.Value;
            if (valorNovo == m.ValorMaximo && idaNova == m.RampaMs && voltaNova == m.RampaVoltaMs) return;

            m.ValorMaximo = valorNovo;
            m.RampaMs = idaNova;
            m.RampaVoltaMs = voltaNova;
            MarcarSujo();
            EscreverDica();
        }

        /// <summary>Explica em palavras simples o que o alvo escolhido faz.</summary>
        private void EscreverDica()
        {
            DesenhoControle.Alvo a = AlvoSelecionado();
            if (a == null || PerfilAtual() == null) { _lblComportamentoNota.Text = ""; return; }

            if (_mapasNaLista.Count == 0)
            {
                _lblComportamentoNota.Text = "Este alvo (" + a.NomePorExtenso + ") ainda nao tem tecla. " +
                                "Clique em 'Capturar tecla' e aperte a tecla que voce quer. Alvo sem tecla nao e gravado no arquivo.";
                return;
            }

            string lista = "";
            foreach (Mapeamento m in _mapasNaLista)
            {
                string nome = GravadorPerfil.NomeDeTecla(m) ?? m.TeclaNome ?? "?";
                lista += (lista.Length == 0 ? "" : " ou ") + nome;
            }

            string texto;
            if (a.Sentido == 0)
            {
                texto = "Segurar " + lista + " mantem o botao " + a.Nome + " apertado; ao soltar, ele solta.";
            }
            else
            {
                Mapeamento m = _mapasNaLista[0];
                string volta = m.RampaVoltaMs < 0
                    ? ("a mesma rampa de ida (" + m.RampaMs + " ms)")
                    : (m.RampaVoltaMs + " ms");
                texto = "Segurar " + lista + " empurra " + a.Rotulo + " ate " + m.ValorMaximo +
                        " em " + m.RampaMs + " ms; ao soltar, volta em " + volta + ".";
            }
            if (_mapasNaLista.Count > 1) texto += "   Todas as teclas da lista fazem a mesma coisa.";

            _lblComportamentoNota.Text = texto;
        }

        // =====================================================================================
        // desenho das listas (feito por nos por causa do tema escuro)
        // =====================================================================================

        private void DesenharPerfil(object remetente, DrawItemEventArgs e)
        {
            if (e.Index < 0 || _arquivo == null) return;

            string nome = (string)_lstPerfis.Items[e.Index];
            bool escolhido = (e.State & DrawItemState.Selected) != 0;

            Rectangle r = new Rectangle(e.Bounds.Left, e.Bounds.Top + 2, e.Bounds.Width, e.Bounds.Height - 4);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath caminho = Tema.Retangulo(r, 4))
            {
                using (SolidBrush fundo = new SolidBrush(escolhido ? Tema.RealceEscuro : Tema.FundoSidebar))
                    e.Graphics.FillPath(fundo, caminho);
                if (escolhido)
                    using (Pen caneta = new Pen(Tema.SeloBorda))
                        e.Graphics.DrawPath(caneta, caminho);
            }

            if (escolhido)
                using (SolidBrush marca = new SolidBrush(Tema.Realce))
                    e.Graphics.FillRectangle(marca, r.Left, r.Top + 12, 2, r.Height - 24);

            Perfil p;
            if (!_arquivo.Perfis.TryGetValue(nome, out p)) p = null;

            // O nome entrava quebrado (sem reticencias) quando era longo e o card tinha o selo
            // "inicial": o DrawString padrao quebra linha e a segunda linha saia cortada
            // (foto zoom-card-perfil.png, 2026-10-21). Agora e 1 linha com reticencias, e o
            // espaco do selo so e reservado quando o selo existe.
            bool seloInicial = string.Equals(_arquivo.PerfilInicial, nome, StringComparison.OrdinalIgnoreCase);
            using (SolidBrush tinta = new SolidBrush(escolhido ? Tema.Realce : Tema.Texto))
            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                e.Graphics.DrawString(nome, Tema.Fonte10N, tinta,
                                      new Rectangle(r.Left + 12, r.Top + 8, r.Width - (seloInicial ? 76 : 24), 18));

            if (seloInicial)
            {
                using (SolidBrush tinta = new SolidBrush(Color.FromArgb(0x91, 0xae, 0x9f)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Far })
                    e.Graphics.DrawString("inicial", Tema.Fonte9, tinta,
                                          new Rectangle(r.Left, r.Top + 9, r.Width - 12, 16), sf);
            }

            string detalhe = "";
            if (p != null)
            {
                int teclas = p.Mapeamentos.Count;
                detalhe = teclas + (teclas == 1 ? " tecla" : " teclas");
                if (!string.IsNullOrWhiteSpace(p.Descricao)) detalhe = p.Descricao + "  ·  " + detalhe;
            }
            using (SolidBrush tinta = new SolidBrush(Tema.Texto3))
            using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                e.Graphics.DrawString(detalhe, Tema.Fonte9, tinta, new Rectangle(r.Left + 12, r.Top + 28, r.Width - 24, 16), sf);
        }

        private void MarcarSujo()
        {
            if (_carregando) return;
            _sujo = true;
            MarcarTitulo();
        }

        private void MarcarTitulo()
        {
            string nome = _caminho != null ? Path.GetFileName(_caminho) : "(arquivo novo)";
            Text = "KeyToJoyEmulator - configurador do controle virtual" + (_sujo ? " *" : "") + "   [" + nome + "]";
            if (_lblSalvoEstado != null)
            {
                _lblSalvoEstado.Text = _sujo ? "● alteracoes nao salvas" : "✓ Sem alteracoes";
                _lblSalvoEstado.ForeColor = _sujo ? Tema.Amarelo : Tema.Texto3;
            }
        }

        // =====================================================================================
        // teclas do alvo escolhido
        // =====================================================================================

        /// <summary>
        /// Captura uma tecla e poe no alvo escolhido. <paramref name="nova"/> = true acrescenta uma
        /// tecla extra (o alvo fica com duas); false troca a tecla que esta escolhida na lista.
        /// </summary>
        private void CapturarTecla(bool nova)
        {
            DesenhoControle.Alvo a = AlvoSelecionado();
            Perfil p = PerfilAtual();
            if (a == null || p == null) return;

            string capturada;
            using (CapturaDeTecla janela = new CapturaDeTecla(false))
            {
                if (janela.ShowDialog(this) != DialogResult.OK) return;
                capturada = janela.NomeCapturado;
            }
            if (string.IsNullOrWhiteSpace(capturada)) return;

            CodigoTecla codigo;
            if (!TabelaTeclas.TentarResolver(capturada, out codigo))
            {
                _lblStatus.Text = "nao entendi a tecla '" + capturada + "'.";
                return;
            }

            Mapeamento mapa = nova ? null : MapaSelecionado();
            if (mapa == null)
            {
                mapa = new Mapeamento();
                mapa.Sentido = 1;
                mapa.RampaMs = 150;
                mapa.RampaVoltaMs = -1;
                if (a.Sentido == 0)
                {
                    Botao b;
                    if (!Enum.TryParse(a.Nome, true, out b)) { _lblStatus.Text = "alvo de botao invalido: " + a.Nome; return; }
                    mapa.Tipo = TipoMapeamento.Botao;
                    mapa.AlvoBotao = b;
                    mapa.ValorMaximo = Faixas.ValorMaximoPadrao(Eixo.LX);
                }
                else
                {
                    Eixo e;
                    if (!Enum.TryParse(a.Nome, true, out e)) { _lblStatus.Text = "alvo de eixo invalido: " + a.Nome; return; }
                    mapa.Tipo = TipoMapeamento.Eixo;
                    mapa.AlvoEixo = e;
                    mapa.Sentido = a.Sentido;
                    mapa.ValorMaximo = Faixas.ValorMaximoPadrao(e);
                }
                p.Mapeamentos.Add(mapa);
            }

            mapa.TeclaNome = TabelaTeclas.Canonico(capturada) ?? capturada;
            mapa.Tecla = codigo;

            // a mesma tecla duas vezes no mesmo alvo nao muda nada: tira a repetida
            string chave = ChaveDoMapa(mapa);
            string nome = GravadorPerfil.NomeDeTecla(mapa);
            for (int i = p.Mapeamentos.Count - 1; i >= 0; i--)
            {
                Mapeamento outro = p.Mapeamentos[i];
                if (ReferenceEquals(outro, mapa)) continue;
                if (!string.Equals(ChaveDoMapa(outro), chave, StringComparison.Ordinal)) continue;
                if (string.Equals(GravadorPerfil.NomeDeTecla(outro), nome, StringComparison.OrdinalIgnoreCase))
                    p.Mapeamentos.RemoveAt(i);
            }

            MarcarSujo();
            AtualizarDesenho();
            _lblStatus.Text = "a tecla '" + nome + "' agora vale para " + a.NomePorExtenso.ToLowerInvariant() + ".";
            MostrarAlvo();
        }

        private void RemoverTecla(object remetente, EventArgs e)
        {
            DesenhoControle.Alvo a = AlvoSelecionado();
            Perfil p = PerfilAtual();
            Mapeamento mapa = MapaSelecionado();
            if (p == null || mapa == null) return;

            string nome = GravadorPerfil.NomeDeTecla(mapa) ?? "?";
            p.Mapeamentos.Remove(mapa);
            MarcarSujo();
            AtualizarDesenho();
            _lblStatus.Text = "tirei a tecla '" + nome + "' do alvo " + (a != null ? a.NomePorExtenso.ToLowerInvariant() : "?") + ".";
            MostrarAlvo();
        }

        // =====================================================================================
        // arquivo
        // =====================================================================================

        private static string PerfilPadrao()
        {
            string lado = Path.Combine(Caminhos.PastaDoPrograma(), "profiles", "corrida.json");
            if (File.Exists(lado)) return lado;

            string pasta = Caminhos.PastaDoPrograma();
            for (int nivel = 0; nivel < 6 && !string.IsNullOrEmpty(pasta); nivel++)
            {
                string c = Path.Combine(pasta, "my-version", "profiles", "corrida.json");
                if (File.Exists(c)) return c;
                DirectoryInfo pai = Directory.GetParent(pasta);
                pasta = pai == null ? null : pai.FullName;
            }
            return lado;
        }

        private void AbrirArquivo(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho))
            {
                _arquivo = new ArquivoPerfis { Versao = 1, TiqueMs = 8, PerfilInicial = "padrao" };
                _arquivo.Perfis["padrao"] = new Perfil { Nome = "padrao" };
                _caminho = null;
                _sujo = false;   // a janela nao deve nascer marcada com "*": nada foi alterado ainda
                RecarregarLista("padrao", "arquivo novo em memoria. Escolha um perfil, clique num botao do controle e aperte a tecla dele.");
                return;
            }

            if (!File.Exists(caminho))
            {
                _arquivo = new ArquivoPerfis { Versao = 1, TiqueMs = 8, PerfilInicial = "padrao" };
                _arquivo.Perfis["padrao"] = new Perfil { Nome = "padrao" };
                _caminho = null;
                _sujo = false;
                RecarregarLista("padrao", "nao encontrei o arquivo " + caminho + " - comecei um novo em memoria (use Abrir... para escolher outro).");
                return;
            }

            List<string> avisos;
            ArquivoPerfis carregado;
            try { carregado = CarregadorPerfil.Carregar(caminho, out avisos); }
            catch (PerfilInvalidoException e)
            {
                _lblStatus.Text = "nao consegui abrir: " + e.Message;
                MessageBox.Show(this, "O arquivo foi recusado pelo carregador:\r\n\r\n" + e.Message, "abrir perfil",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception e)
            {
                _lblStatus.Text = "erro ao abrir: " + e.Message;
                return;
            }

            _arquivo = carregado;
            _caminho = caminho;
            _sujo = false;
            string status = "arquivo carregado: " + caminho;
            if (avisos.Count > 0) status += "   |   avisos: " + string.Join(" | ", avisos.ToArray());
            RecarregarLista(null, status);
        }

        private void EscolherArquivo()
        {
            if (!ConfirmarPerdaDeAlteracoes()) return;

            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Arquivo de perfis (*.json)|*.json|Todos os arquivos (*.*)|*.*";
            d.Title = "Abrir arquivo de perfis";
            d.InitialDirectory = _caminho != null ? Path.GetDirectoryName(_caminho) : Caminhos.PastaDoPrograma();
            if (d.ShowDialog(this) != DialogResult.OK) return;
            AbrirArquivo(d.FileName);
        }

        private void RecarregarDoDisco()
        {
            if (_caminho == null) { AbrirArquivo(null); return; }
            if (!ConfirmarPerdaDeAlteracoes()) return;
            AbrirArquivo(_caminho);
        }

        private bool ConfirmarPerdaDeAlteracoes()
        {
            if (!_sujo) return true;
            DialogResult r = MessageBox.Show(this, "Descartar as alteracoes nao salvas?", "KeyToJoyEmulator",
                                             MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            return r == DialogResult.Yes;
        }

        private void RecarregarLista(string selecionar, string status)
        {
            _carregando = true;
            try
            {
                _lstPerfis.Items.Clear();
                foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis) _lstPerfis.Items.Add(par.Key);

                int indice = 0;
                string alvo = selecionar ?? _arquivo.PerfilInicial;
                for (int i = 0; i < _lstPerfis.Items.Count; i++)
                    if (string.Equals((string)_lstPerfis.Items[i], alvo, StringComparison.OrdinalIgnoreCase)) { indice = i; break; }

                if (_lstPerfis.Items.Count > 0) _lstPerfis.SelectedIndex = indice;
            }
            finally { _carregando = false; }

            _perfilNaTela = null;
            _desenho.Selecionado = "A";
            MostrarPerfilNaTela();

            _lblArquivo.Text = _caminho != null
                ? Path.GetFileName(_caminho)
                : "(novo, ainda nao salvo)";
            _btnArquivo.Texto = _lblArquivo.Text;
            _dicas.SetToolTip(_btnArquivo, _caminho != null
                ? _caminho
                : "novo arquivo: clique em Salvar JSON para escolher onde gravar");
            _lblStatus.Text = status;
            AtualizarEstadoDoPad();
        }

        // =====================================================================================
        // perfis
        // =====================================================================================

        private string NomeUnico(string baseNome)
        {
            if (!_arquivo.Perfis.ContainsKey(baseNome)) return baseNome;
            for (int i = 2; i < 1000; i++)
            {
                string c = baseNome + "-" + i;
                if (!_arquivo.Perfis.ContainsKey(c)) return c;
            }
            return baseNome + "-" + DateTime.Now.Ticks;
        }

        private static Perfil CopiarPerfil(Perfil origem, string novoNome)
        {
            Perfil novo = new Perfil { Nome = novoNome, Descricao = origem.Descricao, ZonaMorta = origem.ZonaMorta };
            foreach (Mapeamento m in origem.Mapeamentos)
            {
                novo.Mapeamentos.Add(new Mapeamento
                {
                    TeclaNome = m.TeclaNome,
                    Tecla = m.Tecla,
                    Tipo = m.Tipo,
                    AlvoBotao = m.AlvoBotao,
                    AlvoEixo = m.AlvoEixo,
                    Sentido = m.Sentido,
                    RampaMs = m.RampaMs,
                    RampaVoltaMs = m.RampaVoltaMs,
                    ValorMaximo = m.ValorMaximo
                });
            }
            foreach (Atalho a in origem.Atalhos)
                novo.Atalhos.Add(new Atalho { Combinacao = a.Combinacao, PerfilDestino = a.PerfilDestino, Teclas = new List<CodigoTecla>(a.Teclas) });
            return novo;
        }

        private void CriarPerfil()
        {
            if (_arquivo == null) return;
            string nome = NomeUnico("perfil-novo");
            _arquivo.Perfis[nome] = new Perfil { Nome = nome };
            if (string.IsNullOrWhiteSpace(_arquivo.PerfilInicial)) _arquivo.PerfilInicial = nome;
            MarcarSujo();
            RecarregarLista(nome, "perfil '" + nome + "' criado. Clique num botao do controle e aperte a tecla que voce quer.");
        }

        private void DuplicarPerfil()
        {
            Perfil p = PerfilAtual();
            if (p == null) return;
            string novo = NomeUnico(p.Nome + "-copia");
            _arquivo.Perfis[novo] = CopiarPerfil(p, novo);
            MarcarSujo();
            RecarregarLista(novo, "perfil '" + novo + "' criado como copia de '" + p.Nome + "'.");
        }

        private void RenomearPerfil()
        {
            Perfil p = PerfilAtual();
            string nome = NomeDoPerfilSelecionado();
            if (p == null || nome == null) return;

            string novo = DialogoTexto.Perguntar(this, "Renomear perfil", "Novo nome do perfil:", nome);
            if (novo == null) return;
            novo = novo.Trim();
            if (novo.Length == 0) { MessageBox.Show(this, "Nome vazio nao vale.", "renomear"); return; }
            if (novo == nome) return;
            if (_arquivo.Perfis.ContainsKey(novo)) { MessageBox.Show(this, "Ja existe um perfil chamado '" + novo + "'.", "renomear"); return; }

            Dictionary<string, Perfil> novos = new Dictionary<string, Perfil>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis) novos[par.Key == nome ? novo : par.Key] = par.Value;
            _arquivo.Perfis = novos;
            p.Nome = novo;

            // quem apontava para o nome antigo passa a apontar para o novo
            if (string.Equals(_arquivo.PerfilInicial, nome, StringComparison.OrdinalIgnoreCase)) _arquivo.PerfilInicial = novo;
            foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis)
                foreach (Atalho a in par.Value.Atalhos)
                    if (string.Equals(a.PerfilDestino, nome, StringComparison.OrdinalIgnoreCase)) a.PerfilDestino = novo;

            MarcarSujo();
            RecarregarLista(novo, "perfil renomeado de '" + nome + "' para '" + novo + "'.");
        }

        private void ExcluirPerfil()
        {
            Perfil p = PerfilAtual();
            string nome = NomeDoPerfilSelecionado();
            if (p == null || nome == null) return;

            if (_arquivo.Perfis.Count <= 1) { MessageBox.Show(this, "O arquivo precisa de pelo menos um perfil.", "excluir"); return; }
            DialogResult r = MessageBox.Show(this, "Excluir o perfil '" + nome + "'?", "excluir",
                                             MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            int atalhosRemovidos = 0;
            foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis)
            {
                for (int i = par.Value.Atalhos.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(par.Value.Atalhos[i].PerfilDestino, nome, StringComparison.OrdinalIgnoreCase))
                    {
                        par.Value.Atalhos.RemoveAt(i);
                        atalhosRemovidos++;
                    }
                }
            }

            _arquivo.Perfis.Remove(nome);
            if (string.Equals(_arquivo.PerfilInicial, nome, StringComparison.OrdinalIgnoreCase))
                foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis) { _arquivo.PerfilInicial = par.Key; break; }

            MarcarSujo();
            string status = "perfil '" + nome + "' excluido";
            if (atalhosRemovidos > 0) status += " (e " + atalhosRemovidos + " atalho(s) que apontavam para ele)";
            RecarregarLista(null, status + ".");
        }

        // =====================================================================================
        // salvar
        // =====================================================================================

        /// <summary>
        /// Confere o modelo antes de gravar. Nao ha "recolher" da tela: o modelo ja esta atualizado,
        /// porque cada mexida escreve nele na hora.
        /// </summary>
        private bool Validar(out string erro)
        {
            erro = null;
            if (_arquivo == null) { erro = "nenhum arquivo aberto."; return false; }
            if (_arquivo.Perfis.Count == 0) { erro = "o arquivo precisa de pelo menos um perfil."; return false; }
            if (string.IsNullOrWhiteSpace(_arquivo.PerfilInicial) || !_arquivo.Perfis.ContainsKey(_arquivo.PerfilInicial))
            {
                erro = "escolha qual perfil o pad usa quando inicia: marque 'usar este perfil quando o pad iniciar'.";
                return false;
            }

            foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis)
            {
                foreach (Mapeamento m in par.Value.Mapeamentos)
                {
                    if (string.IsNullOrWhiteSpace(GravadorPerfil.NomeDeTecla(m)))
                    {
                        erro = "o perfil '" + par.Key + "' tem um alvo sem tecla.";
                        return false;
                    }
                }
            }
            return true;
        }

        private bool Salvar()
        {
            if (_arquivo == null || _carregando) return false;

            string erro;
            if (!Validar(out erro))
            {
                _lblStatus.Text = "nao salvei: " + erro;
                MessageBox.Show(this, erro, "nao salvei", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string destino = _caminho;
            if (string.IsNullOrWhiteSpace(destino))
            {
                SaveFileDialog d = new SaveFileDialog();
                d.Filter = "Arquivo de perfis (*.json)|*.json";
                d.FileName = "perfis.json";
                d.InitialDirectory = Caminhos.PastaDoPrograma();
                if (d.ShowDialog(this) != DialogResult.OK) return false;
                destino = d.FileName;
            }

            List<string> avisos;
            if (!GravadorPerfil.SalvarComValidacao(_arquivo, destino, out erro, out avisos))
            {
                _lblStatus.Text = "nao salvei: " + erro;
                MessageBox.Show(this, erro, "nao salvei", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _caminho = destino;
            _sujo = false;
            MarcarTitulo();
            _lblArquivo.Text = Path.GetFileName(destino);
            _btnArquivo.Texto = _lblArquivo.Text;
            _dicas.SetToolTip(_btnArquivo, destino);

            string status = "salvo em " + destino + " (a versao anterior ficou em " + Path.GetFileName(destino) + ".bak)";
            if (avisos.Count > 0) status += "   |   avisos: " + string.Join(" | ", avisos.ToArray());
            _lblStatus.Text = status;

            if (_chkReiniciar.Checked && ProcessoPad.QuantosRodando() > 0)
            {
                string erro2;
                if (!_pad.Parar(out erro2)) _lblStatus.Text = status + "   |   nao consegui parar o pad: " + erro2;
                else
                {
                    if (LigarPad(out erro2)) _lblStatus.Text = status + "   |   pad reiniciado com o arquivo novo.";
                    else _lblStatus.Text = status + "   |   o pad nao voltou: " + erro2;
                }
            }

            AtualizarEstadoDoPad();
            return true;
        }

        // =====================================================================================
        // ajustes, atalhos e ajuda
        // =====================================================================================

        private void AbrirAjustes()
        {
            Perfil p = PerfilAtual();
            if (_arquivo == null || p == null) return;
            if (JanelaAjustes.Perguntar(this, _arquivo, p))
            {
                MarcarSujo();
                _lstPerfis.Invalidate();
                MostrarPerfilNaTela();
                _lblStatus.Text = "ajustes do perfil '" + p.Nome + "' aplicados (zona morta " + p.ZonaMorta +
                                  " | intervalo do pad " + _arquivo.TiqueMs + " ms).";
            }
        }

        private void AbrirAtalhos()
        {
            Perfil p = PerfilAtual();
            if (_arquivo == null || p == null) return;
            if (JanelaAtalhos.Perguntar(this, _arquivo, p))
            {
                MarcarSujo();
                _lstPerfis.Invalidate();
                _lblStatus.Text = "atalhos do perfil '" + p.Nome + "': " + p.Atalhos.Count + " combinacao(oes).";
            }
        }

        private void MostrarComoUsar()
        {
            MessageBox.Show(this,
                "1) Escolha o perfil na lista da esquerda (ou crie um novo).\r\n" +
                "2) Clique no botao do controle que voce quer configurar.\r\n" +
                "3) Clique em 'Capturar tecla' e aperte a tecla que voce quer usar.\r\n" +
                "4) Clique em 'Salvar'.\r\n" +
                "5) Clique em 'Ligar o pad' para o jogo enxergar o controle virtual.\r\n\r\n" +
                "As teclas aparecem escritas nas caixinhas ligadas ao desenho. Alvo sem tecla fica apagado e nao vai para o arquivo.\r\n" +
                "Para analogicos e gatilhos, o painel da direita tem a sensibilidade e, em 'Avancado', as rampas.\r\n" +
                "Segurar a tecla mantem o botao apertado; soltar, solta. Teclas extras (duas teclas no mesmo alvo) valem igual.",
                "como usar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarSobre()
        {
            MessageBox.Show(this,
                "KeyToJoyEmulator - teclado vira controle de Xbox 360 para o jogo.\r\n\r\n" +
                "Este configurador escreve o arquivo de perfis; ele NAO cria o controle virtual.\r\n" +
                "O arquivo e revalidado pelo proprio programa antes de trocar o seu (a versao anterior fica em .bak).\r\n\r\n" +
                "A captura de tecla usa o mesmo gancho de teclado que o programa usa em jogo.",
                "sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================================================
        // pad: ligar, parar, olhar
        // =====================================================================================

        /// <summary>Abre o dialogo para escolher o KeyToJoyEmulator.exe. Devolve false se o usuario desistir.</summary>
        private bool EscolherProgramaDoPad()
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "KeyToJoyEmulator.exe|KeyToJoyEmulator.exe|Executaveis (*.exe)|*.exe";
            d.Title = "Onde esta o KeyToJoyEmulator.exe";
            d.FileName = "KeyToJoyEmulator.exe";
            string pasta = string.IsNullOrEmpty(_caminhoDoPadExe) ? null : Path.GetDirectoryName(_caminhoDoPadExe);
            if (!string.IsNullOrEmpty(pasta) && Directory.Exists(pasta)) d.InitialDirectory = pasta;
            if (d.ShowDialog(this) != DialogResult.OK) return false;
            _caminhoDoPadExe = d.FileName;
            AtualizarEstadoDoPad();
            return true;
        }

        /// <summary>
        /// O clique do botao unico: se ja tem pad rodando, para; se nao tem, liga.
        /// O caminho do programa e achado sozinho; so pergunta o arquivo se nao achar nenhum.
        /// </summary>
        private void AlternarPad()
        {
            if (ProcessoPad.QuantosRodando() > 0) { PararPadPeloBotao(); return; }

            if (!File.Exists(_caminhoDoPadExe))
            {
                string achado = ProcessoPad.ProcurarExecutavel();
                if (achado != null && File.Exists(achado)) _caminhoDoPadExe = achado;
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
            // 1) o que depende do usuario fica na thread da interface (avisos, salvar)
            string erro;
            if (!PodeLigarPad(out erro)) { FalhaAoLigarPad(erro); return; }

            // 2) iniciar o processo espera ate 1,5 s; na thread da interface isso CONGELA a janela
            //    (defeito relatado em 2026-10-21: "na hora que clica pra ligar ele trava"). Por isso
            //    o trabalho demorado vai para uma thread separada e a tela e atualizada no fim.
            string exe = _caminhoDoPadExe.Trim();
            string perfil = _caminho;
            string log = Path.Combine(Path.GetDirectoryName(perfil), "KeyToJoyEmulator-interface.log");
            _btnLigarPad.Enabled = false;
            _lblStatus.Text = "ligando o pad...";

            Task.Run(() =>
            {
                string falha;
                bool ok = _pad.Iniciar(exe, perfil, log, out falha);
                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        _btnLigarPad.Enabled = true;
                        if (ok)
                            _lblStatus.Text = "pad ligado. O controle virtual leva de 0,4 a 2,4 s para aparecer (veja o 'Teste ao vivo').";
                        else
                            FalhaAoLigarPad(falha);
                        AtualizarEstadoDoPad();
                    }));
                }
                catch (Exception) { }   // a janela pode ter sido fechada enquanto o pad subia
            });
        }

        private void FalhaAoLigarPad(string erro)
        {
            _lblStatus.Text = "nao liguei o pad: " + erro;
            MessageBox.Show(this, erro, "ligar o pad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Checagens que precisam da thread da interface (mensagem e salvar).</summary>
        private bool PodeLigarPad(out string erro)
        {
            erro = null;
            if (_caminho == null || !File.Exists(_caminho))
            {
                erro = "salve o arquivo de perfis antes: o pad le os perfis do disco quando inicia.";
                return false;
            }
            if (_sujo)
            {
                DialogResult r = MessageBox.Show(this, "Tem alteracoes nao salvas. Salvar antes de iniciar o pad?",
                                                 "iniciar pad", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) { erro = "ha alteracoes nao salvas (salve para o pad pegar o perfil novo)."; return false; }
                if (!Salvar()) { erro = "nao consegui salvar o arquivo de perfis."; return false; }
            }
            return true;
        }

        private bool LigarPad(out string erro)
        {
            if (!PodeLigarPad(out erro)) return false;

            string log = Path.Combine(Path.GetDirectoryName(_caminho), "KeyToJoyEmulator-interface.log");
            bool ok = _pad.Iniciar(_caminhoDoPadExe.Trim(), _caminho, log, out erro);
            if (ok) _lblStatus.Text = "log do pad: " + log;
            return ok;
        }

        private void PararPadPeloBotao()
        {
            string erro;
            bool para = _pad.LigadoPorMim;
            if (!para && ProcessoPad.QuantosRodando() > 0)
            {
                DialogResult r = MessageBox.Show(this,
                    "Existe um KeyToJoyEmulator rodando que NAO foi ligado por esta janela. Parar ele mesmo assim?",
                    "parar pad", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }

            if (_pad.Parar(out erro)) _lblStatus.Text = "pad parado.";
            else _lblStatus.Text = "nao parei o pad: " + erro;
            AtualizarEstadoDoPad();
        }

        /// <summary>
        /// Desenha o estado no botao unico: verde = da para ligar, vermelho = da para desligar.
        /// O caminho do programa aparece na dica do botao (nao ocupa mais a tela).
        /// </summary>
        private void AtualizarEstadoDoPad()
        {
            int rodando = ProcessoPad.QuantosRodando();
            bool ligado = rodando > 0;

            _pontoPad.Ligado = ligado;
            _btnLigarPad.Text = ligado ? "Desligar o pad" : "Ligar o pad";
            _btnLigarPad.BackColor = ligado ? Tema.PowerFundo : Tema.Realce;
            _btnLigarPad.ForeColor = ligado ? Tema.Realce : Tema.PowerTexto;
            _btnLigarPad.FlatAppearance.BorderColor = ligado ? Tema.PowerBorda : Tema.Realce;
            _btnLigarPad.FlatAppearance.MouseOverBackColor = ligado ? Color.FromArgb(0x30, 0x40, 0x38) : Tema.BotaoClaroHover;
            _dicas.SetToolTip(_btnLigarPad,
                (ligado ? "Para o pad: o controle virtual sai do Windows." : "Liga o pad: cria o controle virtual com o arquivo salvo.") +
                "\r\nPrograma: " + _caminhoDoPadExe);

            if (ligado)
            {
                _lblEstadoPad.Text = "pad ligado";
                _lblEstadoPad.ForeColor = Tema.Realce;
                _dicas.SetToolTip(_lblEstadoPad, rodando + " processo" + (rodando > 1 ? "s" : "") +
                    (_pad.LigadoPorMim ? " - ligado por esta janela" : " - ligado por fora desta janela"));
            }
            else
            {
                _lblEstadoPad.Text = "pad desligado";
                _lblEstadoPad.ForeColor = Tema.Texto2;
                _dicas.SetToolTip(_lblEstadoPad, File.Exists(_caminhoDoPadExe)
                    ? "clique em Ligar o pad para criar o controle virtual"
                    : "nao achei o KeyToJoyEmulator.exe; clique em Ligar o pad para escolher o programa");
            }
        }

        // =====================================================================================
        // teste ao vivo
        // =====================================================================================

        private void TicVivo(object remetente, EventArgs e)
        {
            _contadorDoRelogio++;
            if (_contadorDoRelogio % 20 == 0) AtualizarEstadoDoPad();

            LeitorControle.Estado estado;
            if (!LeitorControle.Ler(out estado))
            {
                _desenho.SemLeitura();
                _pontoVivo.Ligado = false;
                _lblVivoEstado.Text = "Aguardando conexao";
                _lblVivoMensagem.Text = "Nenhum controle XInput responde agora. Ligue o pad no botao 'Ligar o pad' e espere alguns segundos.";
                _lblVivoEvento.Text = "— nenhum comando —";
                _caixaVivo.CorBorda = Color.FromArgb(0x30, 0x3b, 0x40);
                _caixaVivo.CorFundo = Color.Empty;
                _lblVivoEvento.ForeColor = Tema.Texto3;
                _caixaVivo.Invalidate();
                return;
            }

            _desenho.AoVivo(estado.Botoes, estado.LX, estado.LY, estado.RX, estado.RY, estado.LT, estado.RT);

            bool aceso = estado.Botoes != 0 || estado.LX != 0 || estado.LY != 0 ||
                         estado.RX != 0 || estado.RY != 0 || estado.LT != 0 || estado.RT != 0;
            _pontoVivo.Ligado = true;
            _lblVivoEstado.Text = "Controle " + estado.Indice + "   pacote " + estado.Pacote;
            _lblVivoMensagem.Text = "LX " + estado.LX + "   LY " + estado.LY + "   RX " + estado.RX + "   RY " + estado.RY +
                                    "\r\nLT " + estado.LT + "   RT " + estado.RT;
            _lblVivoEvento.Text = "botoes 0x" + estado.Botoes.ToString("X4");
            _caixaVivo.CorBorda = aceso ? Color.FromArgb(0x53, 0x74, 0x5f) : Color.FromArgb(0x30, 0x3b, 0x40);
            _caixaVivo.CorFundo = aceso ? Color.FromArgb(0x15, 0x23, 0x1a) : Color.Empty;
            _lblVivoEvento.ForeColor = aceso ? Tema.Realce : Tema.Texto3;
            _caixaVivo.Invalidate();
        }

        private void AoFechar(object remetente, FormClosingEventArgs e)
        {
            _tmrVivo.Stop();

            if (_sujo)
            {
                DialogResult r = MessageBox.Show(this,
                    "Tem alteracoes nao salvas no arquivo de perfis. Salvar antes de sair?",
                    "KeyToJoyEmulator", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) { e.Cancel = true; _tmrVivo.Start(); return; }
                if (r == DialogResult.Yes && !Salvar()) { e.Cancel = true; _tmrVivo.Start(); return; }
            }

            if (_pad.LigadoPorMim || ProcessoPad.QuantosRodando() > 0)
            {
                DialogResult r = MessageBox.Show(this,
                    "O pad (KeyToJoyEmulator) esta rodando. Quer parar ele agora?\r\n\r\n" +
                    "Sim = para o pad e sai\r\nNao = deixa o pad rodando e sai",
                    "KeyToJoyEmulator", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    string erro;
                    _pad.Parar(out erro);
                }
            }
        }
    }
}
