// Controles.cs - controles desenhados por nos, usados pelo desenho novo da janela principal.
//
// Por que nao usar os controles do Windows: no tema escuro eles saem no tema CLARO (fundo branco,
// borda cinza), como ja tinha acontecido com a barra deslizante e a lista. Aqui ficam so os pedacos
// do desenho novo: a caixa com borda arredondada, o botao com icone dos dois lados, a aba, o chip da
// tecla e o pontinho de estado. O resto da janela e montado por TableLayoutPanel no arquivo da janela.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Configurador
{
    /// <summary>Painel com borda (e fundo) desenhados por nos, de cantos arredondados.</summary>
    internal class PainelComBorda : Panel
    {
        public Color CorBorda = Tema.Borda;
        public Color CorFundo = Color.Empty;   // vazio = deixa o fundo do painel
        public int Raio = 4;
        public bool Tracejada = false;
        public bool MostrarBorda = true;

        public PainelComBorda()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Tema.Fundo;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush baseFundo = new SolidBrush(BackColor))
                g.FillRectangle(baseFundo, ClientRectangle);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath caminho = Tema.Retangulo(r, Raio))
            {
                if (CorFundo.A > 0)
                    using (SolidBrush tinta = new SolidBrush(CorFundo))
                        g.FillPath(tinta, caminho);

                if (MostrarBorda)
                    using (Pen p = new Pen(CorBorda))
                    {
                        if (Tracejada) p.DashStyle = DashStyle.Dash;
                        g.DrawPath(p, caminho);
                    }
            }
        }
    }

    /// <summary>Pontinho de estado (aceso = realce; apagado = cinza). Usado no topo e no teste ao vivo.</summary>
    internal class PontoDeStatus : Control
    {
        private bool _ligado;
        public bool ComBrilho = true;
        public Color CorLigado = Tema.Realce;
        public Color CorApagado = Color.FromArgb(0x60, 0x69, 0x6e);

        public bool Ligado
        {
            get { return _ligado; }
            set { if (_ligado != value) { _ligado = value; Invalidate(); } }
        }

        public PontoDeStatus()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int d = Math.Max(2, Math.Min(Width, Height));
            Rectangle r = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d);
            Color cor = _ligado ? CorLigado : CorApagado;

            if (_ligado && ComBrilho && d >= 4)
                using (SolidBrush brilho = new SolidBrush(Color.FromArgb(0x33, CorLigado)))
                    g.FillEllipse(brilho, Rectangle.Inflate(r, 2, 2));

            using (SolidBrush tinta = new SolidBrush(cor))
                g.FillEllipse(tinta, r);
        }
    }

    /// <summary>
    /// Botao desenhado por nos com um icone a esquerda, um texto no meio e (se quiser) um icone a
    /// direita. E o botao das partes do desenho novo: arquivo atual, capturar tecla, ajustes, atalhos,
    /// recarregar e salvar.
    /// </summary>
    internal class BotaoDeIcone : Panel
    {
        private readonly Label _esq = new Label();
        private readonly Label _meio = new Label();
        private readonly Label _dir = new Label();
        private bool _hover;

        public Color Fundo = Color.Empty;
        public Color FundoHover = Tema.Hover;
        public Color CorBorda = Color.Empty;
        public Color TextoCor = Tema.Texto2;
        public Color TextoCorHover = Tema.Texto;
        public Color CorEsq = Tema.Texto2;
        public Color CorDir = Tema.Texto3;
        public int Raio = 4;
        public bool Tracejada = false;
        public ContentAlignment Alinhamento = ContentAlignment.MiddleLeft;
        public event EventHandler Clicar;

        public string Texto
        {
            get { return _meio.Text; }
            set { _meio.Text = value; }
        }

        public string IconeEsquerda
        {
            get { return _esq.Text; }
            set { _esq.Text = value; }
        }

        public string IconeDireita
        {
            get { return _dir.Text; }
            set { _dir.Text = value; }
        }

        public Font FonteTexto { get { return _meio.Font; } set { _meio.Font = value; } }
        public Font FonteEsq { get { return _esq.Font; } set { _esq.Font = value; } }
        public Font FonteDir { get { return _dir.Font; } set { _dir.Font = value; } }

        public BotaoDeIcone()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Tema.Fundo;
            Padding = new Padding(11, 0, 11, 0);
            Cursor = Cursors.Hand;
            TabStop = false;

            _esq.AutoSize = true;
            _esq.Dock = DockStyle.Left;
            _esq.TextAlign = ContentAlignment.MiddleLeft;
            _esq.BackColor = Tema.Fundo;
            _esq.Font = Tema.Simbolo15;

            _dir.AutoSize = true;
            _dir.Dock = DockStyle.Right;
            _dir.TextAlign = ContentAlignment.MiddleRight;
            _dir.BackColor = Tema.Fundo;
            _dir.Font = Tema.Simbolo11;

            _meio.AutoSize = false;
            _meio.Dock = DockStyle.Fill;
            _meio.TextAlign = ContentAlignment.MiddleLeft;
            _meio.BackColor = Tema.Fundo;
            _meio.Font = Tema.Fonte10;
            _meio.AutoEllipsis = true;

            // a ordem importa: o ultimo acrescentado e encaixado primeiro
            Controls.Add(_meio);
            Controls.Add(_dir);
            Controls.Add(_esq);

            Ligar(_esq);
            Ligar(_meio);
            Ligar(_dir);
            AplicarCores();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            AplicarCores();
        }

        private void Ligar(Control c)
        {
            c.Click += delegate { if (Enabled && Clicar != null) Clicar(this, EventArgs.Empty); };
            c.MouseEnter += delegate { _hover = true; AplicarCores(); };
            c.MouseLeave += delegate { _hover = false; AplicarCores(); };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            AplicarCores();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            AplicarCores();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Enabled && Clicar != null) Clicar(this, EventArgs.Empty);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _hover = false;
            AplicarCores();
        }

        public override string Text
        {
            get { return _meio.Text; }
            set { _meio.Text = value; }
        }

        /// <summary>Aplica a cor de texto (esquerda/meio/direita) sem mexer no resto do leiaute.</summary>
        private void AplicarCores()
        {
            Color fundo = _hover && FundoHover.A > 0 ? FundoHover : (Fundo.A > 0 ? Fundo : BackColor);
            Color texto = Enabled ? (_hover ? TextoCorHover : TextoCor) : Tema.Texto3;
            Color esq = Enabled ? CorEsq : Tema.Texto3;
            Color dir = Enabled ? CorDir : Tema.Texto3;

            _meio.ForeColor = texto;
            _esq.ForeColor = esq;
            _dir.ForeColor = dir;

            _meio.BackColor = fundo;
            _esq.BackColor = fundo;
            _dir.BackColor = fundo;

            _meio.TextAlign = Alinhamento;
            _esq.Visible = _esq.Text.Length > 0;
            _dir.Visible = _dir.Text.Length > 0;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush baseFundo = new SolidBrush(BackColor))
                g.FillRectangle(baseFundo, ClientRectangle);

            Color fundo = _hover && FundoHover.A > 0 ? FundoHover : Fundo;
            Color borda = Enabled ? CorBorda : Tema.Borda;

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath caminho = Tema.Retangulo(r, Raio))
            {
                if (fundo.A > 0)
                    using (SolidBrush tinta = new SolidBrush(fundo))
                        g.FillPath(tinta, caminho);

                if (borda.A > 0)
                    using (Pen p = new Pen(borda))
                    {
                        if (Tracejada) p.DashStyle = DashStyle.Dash;
                        g.DrawPath(p, caminho);
                    }
            }
        }
    }

    /// <summary>Aba do centro ("Controle" / "Lista de teclas"), com a listra de realce quando ativa.</summary>
    internal class BotaoAba : Panel
    {
        private readonly Label _texto = new Label();
        private bool _ativo;

        public event EventHandler Clicar;

        public bool Ativo
        {
            get { return _ativo; }
            set { if (_ativo != value) { _ativo = value; Aplicar(); } }
        }

        public string Texto
        {
            get { return _texto.Text; }
            set { _texto.Text = value; }
        }

        public BotaoAba(string texto)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Tema.Fundo;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 0, 24, 0);
            Padding = new Padding(0, 0, 0, 2);

            _texto.Text = texto;
            _texto.AutoSize = true;
            _texto.Dock = DockStyle.Left;
            _texto.TextAlign = ContentAlignment.MiddleLeft;
            _texto.BackColor = Tema.Fundo;
            _texto.Font = Tema.Fonte10;
            _texto.ForeColor = Tema.Texto3;
            Controls.Add(_texto);

            _texto.Click += delegate { if (Clicar != null) Clicar(this, EventArgs.Empty); };
            Aplicar();
        }

        private void Aplicar()
        {
            _texto.ForeColor = _ativo ? Tema.Texto : Tema.Texto3;
            _texto.Font = _ativo ? Tema.Fonte10 : Tema.Fonte10;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (Clicar != null) Clicar(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush baseFundo = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(baseFundo, ClientRectangle);

            if (_ativo)
                using (SolidBrush tinta = new SolidBrush(Tema.Realce))
                    e.Graphics.FillRectangle(tinta, 0, Height - 2, Math.Max(_texto.Width, 18), 2);
        }
    }

    /// <summary>Chip de uma tecla do alvo escolhido: a tecla e um "x" para tirar.</summary>
    internal class ChipDeTecla : Panel
    {
        private readonly Label _chave = new Label();
        private readonly Label _xis = new Label();
        private bool _ativo;

        public event EventHandler Clicou;
        public event EventHandler Removeu;

        public bool Ativo
        {
            get { return _ativo; }
            set { if (_ativo != value) { _ativo = value; Invalidate(); } }
        }

        public ChipDeTecla(string texto)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Tema.FundoInspetor;
            Height = 40;
            Margin = new Padding(0, 0, 6, 6);

            int largura = TextRenderer.MeasureText(texto, Tema.Fonte).Width + 52;
            Width = Math.Max(70, Math.Min(230, largura));

            _xis.Text = "×";
            _xis.AutoSize = true;
            _xis.Dock = DockStyle.Right;
            _xis.TextAlign = ContentAlignment.MiddleRight;
            _xis.BackColor = Tema.ChipFundo;
            _xis.ForeColor = Tema.ChipXis;
            _xis.Font = Tema.Fonte11;
            _xis.Padding = new Padding(0, 0, 12, 0);
            _xis.Cursor = Cursors.Hand;

            _chave.Text = texto;
            _chave.AutoSize = false;
            _chave.Dock = DockStyle.Fill;
            _chave.TextAlign = ContentAlignment.MiddleLeft;
            _chave.BackColor = Tema.ChipFundo;
            _chave.ForeColor = Tema.ChipTexto;
            _chave.Font = Tema.Fonte;
            _chave.Padding = new Padding(12, 0, 0, 0);
            _chave.AutoEllipsis = true;

            Controls.Add(_chave);
            Controls.Add(_xis);

            _chave.Click += delegate { if (Clicou != null) Clicou(this, EventArgs.Empty); };
            this.Click += delegate { if (Clicou != null) Clicou(this, EventArgs.Empty); };
            _xis.Click += delegate { if (Removeu != null) Removeu(this, EventArgs.Empty); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush baseFundo = new SolidBrush(BackColor))
                g.FillRectangle(baseFundo, ClientRectangle);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath caminho = Tema.Retangulo(r, 4))
            {
                using (SolidBrush tinta = new SolidBrush(Tema.ChipFundo))
                    g.FillPath(tinta, caminho);
                using (Pen p = new Pen(_ativo ? Tema.ChipAtivo : Tema.ChipBorda))
                    g.DrawPath(p, caminho);
            }
        }
    }

    /// <summary>
    /// Caixa de marcar do tema, desenhada por nos. O CheckBox do Windows com FlatStyle.Flat pinta o
    /// quadradinho com a cor do sistema (branco no tema escuro) - visto na foto de 2026-10-21 no
    /// rodape ("Reiniciar ao salvar") e na lateral ("Usar ao iniciar o pad"). Aqui o quadrado e o
    /// visto sao desenhados, e o texto continua sendo o Text do CheckBox (entao Checked/CheckedChanged
    /// e o clique no texto seguem funcionando como antes).
    /// </summary>
    internal class CaixaDeMarcar : CheckBox
    {
        private bool _emCima = false;

        public CaixaDeMarcar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = true;
            Cursor = Cursors.Hand;
            BackColor = Tema.Fundo;
            Font = Tema.Fonte9;
            ForeColor = Tema.Texto2;
            MouseEnter += delegate { _emCima = true; Invalidate(); };
            MouseLeave += delegate { _emCima = false; Invalidate(); };
        }

        private const int Lado = 12;
        private const int Espaco = 8;

        public override Size GetPreferredSize(Size proposta)
        {
            // +10 de folga: a medida do TextRenderer (GDI) sai um pouco menor que o desenho do texto
            // (GDI+, porque o programa usa SetCompatibleTextRenderingDefault(true)); sem a folga o
            // "Reiniciar ao salvar" saia cortado no rodape (visto em 2026-10-21).
            Size texto = TextRenderer.MeasureText(Text, Font);
            return new Size(Lado + Espaco + texto.Width + 10, Math.Max(Lado, texto.Height));
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            base.OnCheckedChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // o fundo vem do pai: a caixa pode ficar no rodape, na lateral ou no painel do eixo
            Color fundo = (Parent != null && BackColor == Tema.Fundo && Parent.BackColor != Color.Transparent)
                ? Parent.BackColor
                : Tema.Fundo;
            using (SolidBrush tinta = new SolidBrush(fundo))
                g.FillRectangle(tinta, ClientRectangle);

            int topo = (Height - Lado) / 2;
            Rectangle caixa = new Rectangle(0, topo, Lado, Lado);

            using (GraphicsPath caminho = Tema.Retangulo(caixa, 3))
            {
                using (SolidBrush tinta = new SolidBrush(Checked ? Tema.RealceEscuro : Tema.Painel2))
                    g.FillPath(tinta, caminho);
                using (Pen p = new Pen(Checked ? Tema.Realce : (_emCima ? Tema.Texto3 : Tema.Borda)))
                    g.DrawPath(p, caminho);
            }

            if (Checked)
            {
                using (Pen p = new Pen(Tema.Realce, 1.6f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    g.DrawLines(p, new Point[]
                    {
                        new Point(3, topo + 6),
                        new Point(5, topo + 9),
                        new Point(9, topo + 3)
                    });
                }
            }

            // o texto vai pelo TextRenderer (GDI), que e o MESMO componente usado no GetPreferredSize.
            // Com DrawString (GDI+) as larguras nao batiam e "Reiniciar ao salvar" saia cortado no
            // rodape, mesmo com a folga do tamanho (visto em 2026-10-21).
            Color corTexto = Enabled ? (_emCima ? Tema.Texto : Tema.Texto2) : Tema.Texto3;
            Rectangle areaTexto = new Rectangle(Lado + Espaco, 0, Width - Lado - Espaco, Height);
            TextRenderer.DrawText(g, Text, Font, areaTexto, corTexto,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }
}
