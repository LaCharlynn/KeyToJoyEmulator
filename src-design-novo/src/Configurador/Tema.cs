// Tema.cs - paleta, fontes e controles prontos da interface.
//
// Pedido do usuario em 2026-10-19: aplicar o DESENHO NOVO (o prototipo HTML aprovado) ao
// configurador, mantendo o visual antigo do controle desenhado (DesenhoControle.cs). O desenho novo
// usa: fundo quase preto #0b0d0f, paineis #101315, texto #e7e9e8, texto fraco #92999d, texto
// apagado #667075, linha #252a2e, realce verde-acinzentado #b4d4c2 e a fonte JetBrains Mono.
//
// REGRA DA FONTE: a JetBrains Mono vai EMBUTIDA no .exe (recursos\fontes\*.ttf) e carregada por
// PrivateFontCollection. Ela NAO e instalada no Windows: assim o programa continua sendo um arquivo
// unico e nao mexe na maquina do usuario. A licenca (OFL 1.1) vai embutida junto (OFL.txt).
//
// REGRA DO DESENHO: o que aparece na tela e desenhado por nos (nao pelo sistema), para o tema ficar
// coerente. E o caso das barras deslizantes (BarraDeslizante.cs), do desenho do controle
// (DesenhoControle.cs) e do menu (MenuEscuro, no fim deste arquivo).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Configurador
{
    internal static class Tema
    {
        // ---- paleta (os valores sao os do prototipo aprovado) ----
        public static readonly Color Fundo = Color.FromArgb(0x0b, 0x0d, 0x0f);          // fundo da janela
        public static readonly Color Painel = Color.FromArgb(0x10, 0x13, 0x15);        // cartoes
        public static readonly Color Painel2 = Color.FromArgb(0x16, 0x1b, 0x1e);       // caixas e botoes
        public static readonly Color Painel3 = Color.FromArgb(0x1c, 0x22, 0x25);       // realce do mouse
        public static readonly Color Borda = Color.FromArgb(0x25, 0x2a, 0x2e);
        public static readonly Color Texto = Color.FromArgb(0xe7, 0xe9, 0xe8);
        public static readonly Color Texto2 = Color.FromArgb(0x92, 0x99, 0x9d);        // texto fraco
        public static readonly Color Texto3 = Color.FromArgb(0x66, 0x70, 0x75);        // texto bem apagado
        public static readonly Color Realce = Color.FromArgb(0xb4, 0xd4, 0xc2);        // verde-acinzentado
        public static readonly Color RealceEscuro = Color.FromArgb(0x1c, 0x2a, 0x24);
        public static readonly Color Aceso = Color.FromArgb(0xb4, 0xd4, 0xc2);         // o pad esta mandando
        public static readonly Color Verde = Color.FromArgb(0x2e, 0x7d, 0x5b);
        public static readonly Color Vermelho = Color.FromArgb(0xbe, 0x42, 0x3a);
        public static readonly Color Amarelo = Color.FromArgb(0xd8, 0xb5, 0x80);       // "aviso" (warm)

        // ---- cores proprias das partes do desenho novo ----
        // 2026-10-21 (pedido do usuario): as colunas laterais ficavam quase iguais ao meio
        // (#0b0d0f) e a separacao sumia. Subimos um pouco o tom das duas, mantendo a familia
        // do design (antes eram #0e1113 e #0f1214).
        public static readonly Color FundoSidebar = Color.FromArgb(0x11, 0x15, 0x18);
        public static readonly Color FundoInspetor = Color.FromArgb(0x12, 0x16, 0x19);
        public static readonly Color FundoRodape = Color.FromArgb(0x10, 0x14, 0x16);
        public static readonly Color LinhaStatus = Color.FromArgb(0x1d, 0x24, 0x28);
        public static readonly Color BotaoFundo = Color.FromArgb(0x14, 0x18, 0x1b);
        public static readonly Color Hover = Color.FromArgb(0x22, 0x28, 0x2b);
        public static readonly Color HoverBorda = Color.FromArgb(0x47, 0x51, 0x58);
        public static readonly Color ChipFundo = Color.FromArgb(0x1c, 0x26, 0x21);
        public static readonly Color ChipBorda = Color.FromArgb(0x3b, 0x50, 0x44);
        public static readonly Color ChipAtivo = Color.FromArgb(0xa4, 0xc7, 0xb3);
        public static readonly Color ChipTexto = Color.FromArgb(0xe3, 0xeb, 0xe6);
        public static readonly Color ChipXis = Color.FromArgb(0x75, 0x93, 0x81);
        public static readonly Color TokenFundo = Color.FromArgb(0x1d, 0x30, 0x25);
        public static readonly Color TokenBorda = Color.FromArgb(0x3e, 0x59, 0x4b);
        public static readonly Color AvisoFundo = Color.FromArgb(0x1b, 0x18, 0x13);
        public static readonly Color AvisoBorda = Color.FromArgb(0x3c, 0x33, 0x28);
        public static readonly Color AvisoTexto = Color.FromArgb(0x9b, 0x8d, 0x78);
        public static readonly Color SeloBorda = Color.FromArgb(0x33, 0x48, 0x3d);
        public static readonly Color SeloFundo = Color.FromArgb(0x16, 0x21, 0x1b);
        public static readonly Color SalvarFundo = Color.FromArgb(0x20, 0x2a, 0x25);
        public static readonly Color SalvarBorda = Color.FromArgb(0x42, 0x59, 0x4b);
        public static readonly Color PowerFundo = Color.FromArgb(0x25, 0x32, 0x2c);
        public static readonly Color PowerBorda = Color.FromArgb(0x61, 0x7f, 0x6d);
        public static readonly Color PowerTexto = Color.FromArgb(0x17, 0x28, 0x1f);
        public static readonly Color BotaoClaro = Color.FromArgb(0x15, 0x1b, 0x18);   // texto sobre a tecla clara
        public static readonly Color BotaoClaroHover = Color.FromArgb(0xc9, 0xdf, 0xd2);
        public static readonly Color BioTexto = Color.FromArgb(0xcd, 0xd1, 0xd2);

        // =====================================================================================
        // fontes (criadas uma vez; criar a cada desenho vazaria handle)
        //
        // A JetBrains Mono e carregada de dentro do proprio .exe. Cada arquivo .ttf vira uma colecao
        // privada so dele: assim nao importa se o Windows junta as variantes numa familia so ou nao -
        // a familia usada e sempre a do proprio arquivo (Families[0]).
        // =====================================================================================

        private enum Peso { Regular, Media, Semi, Negrito }

        private static readonly List<GCHandle> _memorias = new List<GCHandle>();
        private static readonly List<PrivateFontCollection> _colecoes = new List<PrivateFontCollection>();
        private static FontFamily _famRegular, _famMedia, _famSemi, _famNegrito;

        // tamanhos em PIXELS, iguais aos do prototipo (12px e o corpo)
        public static readonly Font Fonte;        // 12px
        public static readonly Font Fonte8;       // 10px
        public static readonly Font Fonte8N;      // 10px semi-negrito
        public static readonly Font Fonte75;      // 9px
        public static readonly Font FonteMicro;    // 8px - rotulo dentro dos baloes do desenho do controle
        public static readonly Font Fonte10N;     // 11px semi-negrito
        public static readonly Font Fonte12N;     // 16px medio
        public static readonly Font FonteMono;    // 10px

        public static readonly Font Fonte9;
        public static readonly Font Fonte10;
        public static readonly Font Fonte10M;
        public static readonly Font Fonte11;
        public static readonly Font Fonte11M;
        public static readonly Font Fonte16;
        public static readonly Font Fonte16N;
        public static readonly Font Fonte18N;
        public static readonly Font Fonte22;
        public static readonly Font Fonte22M;

        // simbolos (marca, setas, teclado...): a JetBrains Mono nao tem esses desenhos
        public static readonly Font Simbolo26;
        public static readonly Font Simbolo18;
        public static readonly Font Simbolo15;
        public static readonly Font Simbolo11;
        public static readonly Font Simbolo9;
        public static readonly Font Simbolo8;

        static Tema()
        {
            _famRegular = CarregarFamilia("JetBrainsMono-Regular.ttf");
            _famMedia = CarregarFamilia("JetBrainsMono-Medium.ttf");
            _famSemi = CarregarFamilia("JetBrainsMono-SemiBold.ttf");
            _famNegrito = CarregarFamilia("JetBrainsMono-Bold.ttf");

            Fonte = Criar(12f, Peso.Regular);
            Fonte8 = Criar(10f, Peso.Regular);
            Fonte8N = Criar(10f, Peso.Semi);
            Fonte75 = Criar(9f, Peso.Regular);
            FonteMicro = Criar(8f, Peso.Regular);
            Fonte10N = Criar(11f, Peso.Semi);
            Fonte12N = Criar(16f, Peso.Media);
            FonteMono = Criar(10f, Peso.Regular);

            Fonte9 = Criar(9f, Peso.Regular);
            Fonte10 = Criar(10f, Peso.Regular);
            Fonte10M = Criar(10f, Peso.Media);
            Fonte11 = Criar(11f, Peso.Regular);
            Fonte11M = Criar(11f, Peso.Media);
            Fonte16 = Criar(16f, Peso.Media);
            Fonte16N = Criar(16f, Peso.Semi);
            Fonte18N = Criar(18f, Peso.Semi);
            Fonte22 = Criar(22f, Peso.Regular);
            Fonte22M = Criar(22f, Peso.Media);

            Simbolo26 = Simbolo(26f);
            Simbolo18 = Simbolo(18f);
            Simbolo15 = Simbolo(15f);
            Simbolo11 = Simbolo(11f);
            Simbolo9 = Simbolo(9f);
            Simbolo8 = Simbolo(8f);
        }

        private static Font Simbolo(float px)
        {
            try { return new Font("Segoe UI Symbol", px, FontStyle.Regular, GraphicsUnit.Pixel); }
            catch { return new Font("Segoe UI", px, FontStyle.Regular, GraphicsUnit.Pixel); }
        }

        private static Font Criar(float px, Peso peso)
        {
            FontFamily familia = peso == Peso.Media ? _famMedia
                                : peso == Peso.Semi ? _famSemi
                                : peso == Peso.Negrito ? _famNegrito
                                : _famRegular;

            if (familia != null)
            {
                try { return new Font(familia, px, FontStyle.Regular, GraphicsUnit.Pixel); }
                catch { }
            }

            // sem a fonte embutida, o programa continua igual: cai na fonte do sistema
            FontStyle estilo = peso == Peso.Regular ? FontStyle.Regular : FontStyle.Bold;
            return new Font("Segoe UI", px, estilo, GraphicsUnit.Pixel);
        }

        /// <summary>Le um .ttf embutido no proprio .exe e devolve a familia dele (null se nao houver).</summary>
        private static FontFamily CarregarFamilia(string arquivo)
        {
            try
            {
                Assembly eu = Assembly.GetExecutingAssembly();
                string nome = null;
                foreach (string candidato in eu.GetManifestResourceNames())
                    if (candidato.EndsWith(arquivo, StringComparison.OrdinalIgnoreCase)) { nome = candidato; break; }
                if (nome == null) return null;

                byte[] bytes;
                using (Stream s = eu.GetManifestResourceStream(nome))
                {
                    if (s == null) return null;
                    bytes = new byte[s.Length];
                    int lidos = 0;
                    while (lidos < bytes.Length)
                    {
                        int n = s.Read(bytes, lidos, bytes.Length - lidos);
                        if (n <= 0) break;
                        lidos += n;
                    }
                }

                // a colecao precisa que a memoria continue valida: guardamos o ponteiro preso
                GCHandle prendedor = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                _memorias.Add(prendedor);

                PrivateFontCollection colecao = new PrivateFontCollection();
                colecao.AddMemoryFont(prendedor.AddrOfPinnedObject(), bytes.Length);
                _colecoes.Add(colecao);

                return colecao.Families.Length > 0 ? colecao.Families[0] : null;
            }
            catch { return null; }
        }

        // =====================================================================================
        // controles prontos (o resto do programa monta a tela com isto)
        // =====================================================================================

        /// <summary>Rotulo simples. fraco = texto apagado (dicas).</summary>
        public static Label Rotulo(string texto, bool fraco)
        {
            Label l = new Label();
            l.Text = texto;
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = fraco ? Texto2 : Texto;
            l.AutoSize = false;
            l.BackColor = Color.Transparent;
            return l;
        }

        /// <summary>Rotulo de secao do desenho novo: texto bem apagado em caixa alta, com um valor a direita.</summary>
        public static Panel RotuloSecao(string texto, string direita)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Color.Transparent;
            p.Margin = new Padding(0);

            Label esq = new Label();
            esq.Text = texto.ToUpperInvariant();
            esq.AutoSize = true;
            esq.ForeColor = Texto3;
            esq.Font = Fonte9;
            esq.BackColor = Color.Transparent;
            esq.Dock = DockStyle.Left;
            esq.TextAlign = ContentAlignment.MiddleLeft;
            p.Controls.Add(esq);

            if (!string.IsNullOrEmpty(direita))
            {
                Label dir = new Label();
                dir.Text = direita;
                dir.AutoSize = true;
                dir.ForeColor = Texto3;
                dir.Font = Fonte9;
                dir.BackColor = Color.Transparent;
                dir.Dock = DockStyle.Right;
                dir.TextAlign = ContentAlignment.MiddleRight;
                p.Controls.Add(dir);
            }
            return p;
        }

        public static Label TituloDoCartao(string texto)
        {
            Label l = new Label();
            l.Text = texto.ToUpperInvariant();
            l.Dock = DockStyle.Fill;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = Texto2;
            l.Font = Fonte8N;
            l.Margin = new Padding(2, 0, 0, 2);
            return l;
        }

        /// <summary>Botao escuro (usado nos dialogos).</summary>
        public static Button Botao(string texto, bool principal)
        {
            Button b = new Button();
            b.Text = texto;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = principal ? RealceEscuro : Borda;
            b.BackColor = principal ? RealceEscuro : Painel2;
            b.ForeColor = Texto;
            b.FlatAppearance.MouseOverBackColor = principal ? Hover : Painel3;
            b.FlatAppearance.MouseDownBackColor = principal ? RealceEscuro : Borda;
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(0, 0, 5, 0);
            b.Font = Fonte;
            b.TabStop = false;
            return b;
        }

        /// <summary>Botao escuro sem borda, para a fileira de acoes do inspetor.</summary>
        public static Button BotaoContorno(string texto)
        {
            Button b = new Button();
            b.Text = texto;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Borda;
            b.BackColor = FundoInspetor;
            b.ForeColor = Texto2;
            b.FlatAppearance.MouseOverBackColor = Hover;
            b.FlatAppearance.MouseDownBackColor = Painel3;
            b.Font = Fonte9;
            b.TabStop = false;
            b.Cursor = Cursors.Hand;
            return b;
        }

        /// <summary>Botao pequeno, para fileira horizontal.</summary>
        public static Button BotaoPequeno(string texto, int largura)
        {
            Button b = Botao(texto, false);
            b.Dock = DockStyle.None;
            b.Width = largura;
            b.Height = 26;
            b.Margin = new Padding(0, 0, 5, 0);
            return b;
        }

        public static TextBox Caixa(bool somenteLeitura)
        {
            TextBox t = new TextBox();
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = somenteLeitura ? Painel : Painel2;
            t.ForeColor = somenteLeitura ? Texto2 : Texto;
            t.ReadOnly = somenteLeitura;
            t.Dock = DockStyle.Fill;
            t.Margin = new Padding(0);
            return t;
        }

        public static NumericUpDown Numero(int minimo, int maximo, int valor)
        {
            NumericUpDown n = new NumericUpDown();
            n.Minimum = minimo;
            n.Maximum = maximo;
            n.Value = valor;
            n.ThousandsSeparator = true;
            n.BorderStyle = BorderStyle.FixedSingle;
            n.BackColor = Painel2;
            n.ForeColor = Texto;
            n.Dock = DockStyle.Fill;
            n.Margin = new Padding(0);
            // Os botoezinhos de subir/descer sao pintados pelo Windows no tema CLARO e viravam um
            // quadradinho claro no meio do tema escuro. Duas coisas foram testadas em 2026-10-18:
            //   (1) SetWindowTheme(hwnd, "DarkMode_Explorer", null) NAO escurece esses botoes (conferido
            //       em foto, Entrada 104) - por isso nao esta aqui;
            //   (2) ESCONDER o controle dos botoes funciona. A sonda `sonda-numero` (cria o controle sem
            //       mostrar janela) mostrou dois filhos: "UpDownBase+UpDownButtons" (as setinhas) e
            //       "UpDownBase+UpDownEdit" (o numero). Escondendo o primeiro, o numero continua visivel,
            //       ligado e digitavel. Decisao do usuario em 2026-10-18: esconder.
            foreach (Control filho in n.Controls)
                if (string.Equals(filho.GetType().Name, "UpDownButtons", StringComparison.Ordinal))
                    filho.Visible = false;

            // Esconder NAO bastou: a faixa que as setinhas ocupavam nao e pintada por ninguem e ficava
            // BRANCA no meio do campo escuro. O jeito que funcionou foi pintar essa faixa com a cor do
            // campo, no desenho do proprio campo, sem tocar no resto.
            n.Paint += delegate(object remetente, PaintEventArgs e)
            {
                foreach (Control filho in n.Controls)
                {
                    if (!string.Equals(filho.GetType().Name, "UpDownEdit", StringComparison.Ordinal)) continue;
                    int x = filho.Right;
                    int largura = n.ClientSize.Width - x;
                    if (largura > 0)
                        using (SolidBrush tinta = new SolidBrush(Painel2))
                            e.Graphics.FillRectangle(tinta, new Rectangle(x, 0, largura, n.ClientSize.Height));
                    break;
                }
            };
            return n;
        }

        // =====================================================================================
        // barra de titulo escura e logo nas janelas
        // =====================================================================================

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr janela, int atributo, ref int valor, int tamanho);

        /// <summary>Poe a logo (a mesma do .exe) na janela: vale para a barra de titulo e a barra de tarefas.</summary>
        public static void IconeDaJanela(Form janela)
        {
            if (janela == null) return;
            try
            {
                string caminho = Environment.ProcessPath;
                if (string.IsNullOrEmpty(caminho)) return;
                janela.Icon = Icon.ExtractAssociatedIcon(caminho);
            }
            catch (ArgumentException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>Pede ao Windows a barra de titulo escura. So enfeite: se o Windows nao aceitar, nada muda.</summary>
        public static void BarraDeTituloEscura(IntPtr janela)
        {
            int ligado = 1;
            try
            {
                if (DwmSetWindowAttribute(janela, 20, ref ligado, sizeof(int)) != 0)
                    DwmSetWindowAttribute(janela, 19, ref ligado, sizeof(int));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        /// <summary>
        /// Cartao: painel com titulo e uma area vazia (devolvida em <paramref name="conteudo"/>) para
        /// quem chama preencher. Usado pelos dialogos.
        /// </summary>
        public static Panel Cartao(string titulo, Padding margem, out Panel conteudo)
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Painel;
            p.Margin = margem;
            p.Padding = new Padding(10, 6, 10, 8);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Painel;
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            int linha = 0;
            if (titulo != null)
            {
                t.RowCount = 2;
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
                t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                t.Controls.Add(TituloDoCartao(titulo), 0, 0);
                linha = 1;
            }
            else
            {
                t.RowCount = 1;
                t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }

            conteudo = new Panel();
            conteudo.Dock = DockStyle.Fill;
            conteudo.BackColor = Painel;
            conteudo.Margin = new Padding(0);
            t.Controls.Add(conteudo, 0, linha);

            p.Controls.Add(t);
            return p;
        }

        /// <summary>Lista escura desenhada por nos.</summary>
        public static ListBox Lista(int alturaDaLinha)
        {
            ListBox l = new ListBox();
            l.BorderStyle = BorderStyle.None;
            l.BackColor = FundoSidebar;
            l.ForeColor = Texto;
            l.DrawMode = DrawMode.OwnerDrawFixed;
            l.ItemHeight = alturaDaLinha;
            l.IntegralHeight = false;
            l.Dock = DockStyle.Fill;
            l.HandleCreated += delegate { EscurecerBarraDeRolagem(l.Handle); };
            return l;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr janela, string subApp, string subId);

        /// <summary>Pede ao Windows o desenho escuro da barra de rolagem desta janela.</summary>
        private static void EscurecerBarraDeRolagem(IntPtr janela)
        {
            if (janela == IntPtr.Zero) return;
            try { SetWindowTheme(janela, "DarkMode_Explorer", null); }
            catch { }
        }

        /// <summary>Mesma coisa, para um controle que rola (painel com AutoScroll, lista, caixa de texto).</summary>
        public static void EscurecerBarraDeRolagem(Control controle)
        {
            if (controle == null) return;
            if (controle.IsHandleCreated) EscurecerBarraDeRolagem(controle.Handle);
            else controle.HandleCreated += delegate { EscurecerBarraDeRolagem(controle.Handle); };
        }

        /// <summary>Linha separadora de 1 px.</summary>
        public static Panel Separador()
        {
            Panel p = new Panel();
            p.Height = 1;
            p.Dock = DockStyle.Top;
            p.BackColor = Borda;
            p.Margin = new Padding(0, 6, 0, 6);
            return p;
        }

        /// <summary>CheckBox do tema.</summary>
        public static CheckBox CaixaDeMarcar(string texto)
        {
            // Desenhada por nos (Controles.cs): a FlatStyle.Flat do Windows pintava o quadradinho
            // com a cor do sistema (branco no tema escuro).
            CheckBox c = new Configurador.CaixaDeMarcar();
            c.Text = texto;
            c.ForeColor = Texto2;
            c.BackColor = Fundo;
            c.Font = Fonte9;
            c.Cursor = Cursors.Hand;
            c.AutoSize = true;
            return c;
        }

        /// <summary>Desenha um retangulo de cantos arredondados (usado pelos controles deste arquivo).</summary>
        public static GraphicsPath Retangulo(Rectangle r, int raio)
        {
            GraphicsPath caminho = new GraphicsPath();
            int d = raio * 2;
            if (d <= 0 || d > r.Width || d > r.Height)
            {
                caminho.AddRectangle(r);
                return caminho;
            }
            caminho.AddArc(r.X, r.Y, d, d, 180, 90);
            caminho.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            caminho.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            caminho.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            caminho.CloseFigure();
            return caminho;
        }
    }

    /// <summary>Cores do menu suspenso (o padrao do Windows e claro e destoa do resto).</summary>
    internal sealed class CoresDoMenu : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Tema.Hover; } }
        public override Color MenuItemBorder { get { return Tema.Realce; } }
        public override Color MenuBorder { get { return Tema.Borda; } }
        public override Color MenuItemSelectedGradientBegin { get { return Tema.Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return Tema.Hover; } }
        public override Color MenuItemPressedGradientBegin { get { return Tema.Painel2; } }
        public override Color MenuItemPressedGradientEnd { get { return Tema.Painel2; } }
        public override Color ToolStripDropDownBackground { get { return Tema.Painel2; } }
        public override Color ImageMarginGradientBegin { get { return Tema.Painel2; } }
        public override Color ImageMarginGradientMiddle { get { return Tema.Painel2; } }
        public override Color ImageMarginGradientEnd { get { return Tema.Painel2; } }
        public override Color SeparatorDark { get { return Tema.Borda; } }
        public override Color SeparatorLight { get { return Tema.Borda; } }
    }

    internal sealed class MenuEscuro : ToolStripProfessionalRenderer
    {
        public MenuEscuro() : base(new CoresDoMenu()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Tema.Texto : Tema.Texto2;
            base.OnRenderItemText(e);
        }
    }
}
