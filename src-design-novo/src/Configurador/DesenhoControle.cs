// DesenhoControle.cs - o controle de Xbox One na tela, com a tecla de cada botao.
//
// A CARCACA e a IMAGEM que o usuario mandou em 2026-10-18 (recursos\controle-xbox.png), gravada
// dentro do proprio programa (recurso embutido). Foi pedido dele: "apenas coloque a imagem
// diretamente, preencha centro dela e os botoes das suas respectivas cores".
// A imagem original e desenho de LINHA: o fundo e o miolo da carcaca sao transparentes; um script
// preencheu o miolo de branco (recortado pelo contorno) e essa versao pronta e a que vai embutida
// (Entradas 87 e 88 do REA-FINDINGS.md).
//
// Por que ainda desenhamos por cima da imagem:
//   - os circulos A/B/X/Y recebem a cor de cada botao (verde, vermelho, azul, amarelo);
//   - o desenho responde ao pad: e ele o "teste ao vivo";
//   - cada botao fica clicavel, e a caixinha com a tecla fica ligada ao botao por uma linha.
// O desenho do usuario nao tem gatilho (LT/RT) nem peca de ombro separada: o ombro e a FAIXA entre a
// linha de fora da carcaca e a segunda linha interna (Entrada 90). Por isso LB/RB apontam para essa
// faixa e os gatilhos sao abas que desenhamos ATRAS da imagem.
//
// Toda posicao deste arquivo esta em PIXELS DA IMAGEM (680 x 678) e foi MEDIDA nela, componente por
// componente (Entrada 89), nao estimada - e o que faz a linha de chamada terminar na borda do botao
// certo (a reclamacao do usuario em 2026-10-18 foi exatamente "as linhas nao seguem para os botoes
// corretamente").
//
// Lista de alvos: a MESMA do modelo e do pad (secao 6 do SPEC.md):
//   14 botoes (A, B, X, Y, LB, RB, BACK, START, LS, RS, UP, DOWN, LEFT, RIGHT),
//   4 direcoes de cada analogico (LX/LY/RX/RY, cada uma com sentido +1 e -1) e os 2 gatilhos (LT/RT).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Configurador
{
    public sealed class DesenhoControle : Control
    {
        /// <summary>Um alvo do controle (botao, direcao de analogico ou gatilho) e o que a tela mostra dele.</summary>
        public sealed class Alvo
        {
            public string Nome;         // A, LB, UP, LX, LT...
            public int Sentido;         // 0 = botao; +1 / -1 = direcao do analogico
            public int Lado;            // 0 = caixinha em cima, 1 = esquerda, 2 = direita, 3 = embaixo
            public string Tecla;        // texto da tecla mapeada (null = sem tecla)
            public bool Aceso;          // o pad esta mandando este botao agora
            public float Valor;         // 0..1 (gatilho)
            public float DeslocX;       // -1..1 (analogico)
            public float DeslocY;
            public Rectangle Caixa;     // calculado no tamanho atual

            /// <summary>Chave unica do alvo: "A", "LX+", "LX-", "LT".</summary>
            public string Chave { get { return Sentido == 0 ? Nome : Nome + (Sentido > 0 ? "+" : "-"); } }

            /// <summary>Como o alvo aparece escrito.</summary>
            public string Rotulo
            {
                get
                {
                    switch (Nome)
                    {
                        // 2026-10-18: eram "L.analog. ->" / "L.analog. ^". O usuario apontou que o
                        // rotulo saia ilegivel ("Lanalog." parecia uma palavra so, o ponto sumia). Agora
                        // usa a mesma forma do D-pad, que ja existia ("D-pad esq."), e o nome inteiro do
                        // analogico. As larguras foram MEDIDAS na fonte do desenho (Segoe UI 7,5):
                        // "L. analogico dir." = 74,6 px e "L. analogico cima" = 76,8 px, dentro dos
                        // ~104 px que a caixinha lateral da (largura lateral de 136 menos a tecla).
                        case "LX": return Sentido > 0 ? "L. analogico dir." : "L. analogico esq.";
                        case "LY": return Sentido > 0 ? "L. analogico cima" : "L. analogico baixo";
                        case "RX": return Sentido > 0 ? "R. analogico dir." : "R. analogico esq.";
                        case "RY": return Sentido > 0 ? "R. analogico cima" : "R. analogico baixo";
                        case "LT": return "LT";
                        case "RT": return "RT";
                        case "LB": return "LB";
                        case "RB": return "RB";
                        case "LS": return "LS (clique)";
                        case "RS": return "RS (clique)";
                        case "BACK": return "Voltar";
                        case "START": return "Iniciar";
                        case "UP": return "D-pad cima";
                        case "DOWN": return "D-pad baixo";
                        case "LEFT": return "D-pad esq.";
                        case "RIGHT": return "D-pad dir.";
                        default: return Nome;
                    }
                }
            }

            /// <summary>Nome em portugues, para o painel da direita.</summary>
            public string NomePorExtenso
            {
                get
                {
                    switch (Nome)
                    {
                        case "A": return "Botao A";
                        case "B": return "Botao B";
                        case "X": return "Botao X";
                        case "Y": return "Botao Y";
                        case "LB": return "Botao LB (ombro esquerdo)";
                        case "RB": return "Botao RB (ombro direito)";
                        case "BACK": return "Botao VOLTAR";
                        case "START": return "Botao INICIAR";
                        case "LS": return "Clique do analogico esquerdo (LS)";
                        case "RS": return "Clique do analogico direito (RS)";
                        case "UP": return "Direcional - cima";
                        case "DOWN": return "Direcional - baixo";
                        case "LEFT": return "Direcional - esquerda";
                        case "RIGHT": return "Direcional - direita";
                        case "LT": return "Gatilho esquerdo (LT)";
                        case "RT": return "Gatilho direito (RT)";
                        // o painel da direita tem espaco: aqui o nome sai inteiro, sem abreviar
                        case "LX": return Sentido > 0 ? "Analogico esquerdo - direita" : "Analogico esquerdo - esquerda";
                        case "LY": return Sentido > 0 ? "Analogico esquerdo - cima" : "Analogico esquerdo - baixo";
                        case "RX": return Sentido > 0 ? "Analogico direito - direita" : "Analogico direito - esquerda";
                        case "RY": return Sentido > 0 ? "Analogico direito - cima" : "Analogico direito - baixo";
                        default: return Rotulo;
                    }
                }
            }
        }

        private readonly List<Alvo> _alvos = AlvosPadrao();
        private string _selecionado = "A";
        private string _passando;
        private bool _temLeitura;
        private ushort _botoes;
        private short _lx, _ly, _rx, _ry;
        private byte _lt, _rt;

        // ---- a imagem e a escala ----
        private static Image _imagem;          // carregada uma vez, compartilhada por todas as janelas
        private float _escala = 1f;            // quantos pixels de tela vale 1 pixel da imagem
        private PointF _canto;                 // onde cai o pixel (0,0) da imagem
        private RectangleF _area;              // retangulo onde a imagem inteira foi desenhada

        private Font _fonteLetra;              // cache: criar fonte a cada quadro vazaria recurso
        private float _tamFonteLetra;

        public event EventHandler Selecionou;

        public DesenhoControle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fundo;
            DoubleBuffered = true;
        }

        /// <summary>Os 24 alvos, na ordem em que as caixinhas aparecem na tela.</summary>
        public static List<Alvo> AlvosPadrao()
        {
            List<Alvo> a = new List<Alvo>();

            // em cima, da esquerda para a direita
            a.Add(new Alvo { Nome = "LT", Sentido = 1, Lado = 0 });
            a.Add(new Alvo { Nome = "LB", Sentido = 0, Lado = 0 });
            a.Add(new Alvo { Nome = "BACK", Sentido = 0, Lado = 0 });
            a.Add(new Alvo { Nome = "START", Sentido = 0, Lado = 0 });
            a.Add(new Alvo { Nome = "RB", Sentido = 0, Lado = 0 });
            a.Add(new Alvo { Nome = "RT", Sentido = 1, Lado = 0 });

            // coluna da esquerda: analogico esquerdo e direcional
            a.Add(new Alvo { Nome = "LX", Sentido = 1, Lado = 1 });
            a.Add(new Alvo { Nome = "LX", Sentido = -1, Lado = 1 });
            a.Add(new Alvo { Nome = "LY", Sentido = 1, Lado = 1 });
            a.Add(new Alvo { Nome = "LY", Sentido = -1, Lado = 1 });
            a.Add(new Alvo { Nome = "UP", Sentido = 0, Lado = 1 });
            a.Add(new Alvo { Nome = "DOWN", Sentido = 0, Lado = 1 });
            a.Add(new Alvo { Nome = "LEFT", Sentido = 0, Lado = 1 });
            a.Add(new Alvo { Nome = "RIGHT", Sentido = 0, Lado = 1 });

            // coluna da direita: botoes e analogico direito
            a.Add(new Alvo { Nome = "A", Sentido = 0, Lado = 2 });
            a.Add(new Alvo { Nome = "B", Sentido = 0, Lado = 2 });
            a.Add(new Alvo { Nome = "X", Sentido = 0, Lado = 2 });
            a.Add(new Alvo { Nome = "Y", Sentido = 0, Lado = 2 });
            a.Add(new Alvo { Nome = "RX", Sentido = 1, Lado = 2 });
            a.Add(new Alvo { Nome = "RX", Sentido = -1, Lado = 2 });
            a.Add(new Alvo { Nome = "RY", Sentido = 1, Lado = 2 });
            a.Add(new Alvo { Nome = "RY", Sentido = -1, Lado = 2 });

            // embaixo: os cliques dos analogicos
            a.Add(new Alvo { Nome = "LS", Sentido = 0, Lado = 3 });
            a.Add(new Alvo { Nome = "RS", Sentido = 0, Lado = 3 });

            return a;
        }

        /// <summary>Alvos desenhados, na ordem da tela.</summary>
        public List<Alvo> Alvos { get { return _alvos; } }

        public string Selecionado
        {
            get { return _selecionado; }
            set
            {
                if (string.Equals(_selecionado, value, StringComparison.Ordinal)) return;
                _selecionado = value;
                Invalidate();
            }
        }

        public string Passando { get { return _passando; } }

        /// <summary>Poe nas caixinhas a tecla de cada alvo (chave -> nome da tecla).</summary>
        public void DefinirTeclas(Dictionary<string, string> teclas)
        {
            foreach (Alvo a in _alvos)
            {
                string t;
                a.Tecla = (teclas != null && teclas.TryGetValue(a.Chave, out t)) ? t : null;
            }
            Invalidate();
        }

        /// <summary>O que o pad esta mandando agora (leitura do XInput, feita por quem chama).</summary>
        public void AoVivo(ushort botoes, short lx, short ly, short rx, short ry, byte lt, byte rt)
        {
            _temLeitura = true;
            _botoes = botoes;
            _lx = lx; _ly = ly; _rx = rx; _ry = ry; _lt = lt; _rt = rt;

            foreach (Alvo a in _alvos)
            {
                bool aceso = false;
                float valor = 0f;
                float dx = 0f, dy = 0f;

                switch (a.Nome)
                {
                    case "A": aceso = (botoes & LeitorControle.BT_A) != 0; break;
                    case "B": aceso = (botoes & LeitorControle.BT_B) != 0; break;
                    case "X": aceso = (botoes & LeitorControle.BT_X) != 0; break;
                    case "Y": aceso = (botoes & LeitorControle.BT_Y) != 0; break;
                    case "LB": aceso = (botoes & LeitorControle.BT_LB) != 0; break;
                    case "RB": aceso = (botoes & LeitorControle.BT_RB) != 0; break;
                    case "BACK": aceso = (botoes & LeitorControle.BT_BACK) != 0; break;
                    case "START": aceso = (botoes & LeitorControle.BT_START) != 0; break;
                    case "LS": aceso = (botoes & LeitorControle.BT_LS) != 0; break;
                    case "RS": aceso = (botoes & LeitorControle.BT_RS) != 0; break;
                    case "UP": aceso = (botoes & LeitorControle.BT_UP) != 0; break;
                    case "DOWN": aceso = (botoes & LeitorControle.BT_DOWN) != 0; break;
                    case "LEFT": aceso = (botoes & LeitorControle.BT_LEFT) != 0; break;
                    case "RIGHT": aceso = (botoes & LeitorControle.BT_RIGHT) != 0; break;
                    case "LT": valor = lt / 255f; aceso = lt > 0; break;
                    case "RT": valor = rt / 255f; aceso = rt > 0; break;
                    case "LX": dx = lx / 32767f; if (a.Sentido > 0) aceso = lx > 4000; else aceso = lx < -4000; break;
                    case "LY": dy = ly / 32767f; if (a.Sentido > 0) aceso = ly > 4000; else aceso = ly < -4000; break;
                    case "RX": dx = rx / 32767f; if (a.Sentido > 0) aceso = rx > 4000; else aceso = rx < -4000; break;
                    case "RY": dy = ry / 32767f; if (a.Sentido > 0) aceso = ry > 4000; else aceso = ry < -4000; break;
                }

                if (a.Nome == "LX") { a.DeslocX = dx; a.DeslocY = 0; }
                else if (a.Nome == "LY") { a.DeslocX = 0; a.DeslocY = dy; }
                else if (a.Nome == "RX") { a.DeslocX = dx; a.DeslocY = 0; }
                else if (a.Nome == "RY") { a.DeslocX = 0; a.DeslocY = dy; }

                a.Aceso = aceso;
                if (a.Nome == "LT" || a.Nome == "RT") a.Valor = valor;
                if (a.Nome == "LS") { a.DeslocX = lx / 32767f; a.DeslocY = -ly / 32767f; }
                if (a.Nome == "RS") { a.DeslocX = rx / 32767f; a.DeslocY = -ry / 32767f; }
            }

            Invalidate();
        }

        public void SemLeitura()
        {
            _temLeitura = false;
            _lx = _ly = _rx = _ry = 0;
            _lt = _rt = 0;
            _botoes = 0;
            foreach (Alvo a in _alvos)
            {
                a.Aceso = false;
                a.Valor = 0f;
                a.DeslocX = 0f;
                a.DeslocY = 0f;
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _fonteLetra != null) { _fonteLetra.Dispose(); _fonteLetra = null; }
            base.Dispose(disposing);
        }

        // =====================================================================================
        // a imagem do controle e a conversao de coordenadas
        // =====================================================================================

        /// <summary>Tamanho da imagem embutida, em pixels (o mesmo da imagem do usuario).</summary>
        private const int IMG_LARGURA = 680, IMG_ALTURA = 678;

        /// <summary>Caixa do DESENHO dentro da imagem (o resto da imagem e so margem transparente).</summary>
        private const double DES_X0 = 3, DES_Y0 = 99, DES_LARGURA = 674, DES_ALTURA = 481;

        /// <summary>Le a imagem embutida do proprio programa (uma vez so).</summary>
        private static Image Imagem()
        {
            if (_imagem != null) return _imagem;
            try
            {
                Assembly este = typeof(DesenhoControle).Assembly;
                string nome = null;
                foreach (string n in este.GetManifestResourceNames())
                    if (n.EndsWith("controle-xbox.png", StringComparison.OrdinalIgnoreCase)) { nome = n; break; }
                if (nome == null) return null;
                using (Stream recurso = este.GetManifestResourceStream(nome))
                {
                    if (recurso == null) return null;
                    // copia para a memoria: a imagem precisa da fonte aberta enquanto for usada
                    using (MemoryStream copia = new MemoryStream())
                    {
                        recurso.CopyTo(copia);
                        copia.Position = 0;
                        _imagem = PintarNoTema(Image.FromStream(copia));
                    }
                }
            }
            catch (Exception) { return null; }
            return _imagem;
        }

        /// <summary>
        /// Pinta a arte de linha no tema do desenho novo: a carcaca (que era branca) vira o degrade
        /// escuro do prototipo (#22262a em cima, #131619 embaixo) e as linhas (que eram pretas) viram
        /// o cinza metalico do contorno (#5a6064 / #30363b / #464c51). O logo, que era azul, vira o
        /// verde do realce. A arte original continua embutida; so o resultado e guardado.
        /// </summary>
        private static Image PintarNoTema(Image original)
        {
            try
            {
                using (Bitmap origem = new Bitmap(original))
                {
                    int largura = origem.Width, altura = origem.Height;
                    Bitmap destino = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);
                    Rectangle area = new Rectangle(0, 0, largura, altura);
                    BitmapData leitura = origem.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    BitmapData escrita = destino.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    int total = largura * altura;
                    int[] pixels = new int[total];
                    try
                    {
                        Marshal.Copy(leitura.Scan0, pixels, 0, total);

                        Color fundoTopo = Color.FromArgb(0x22, 0x26, 0x2a);
                        Color fundoBase = Color.FromArgb(0x13, 0x16, 0x19);
                        Color linhaTopo = Color.FromArgb(0x5a, 0x60, 0x64);
                        Color linhaMeio = Color.FromArgb(0x30, 0x36, 0x3b);
                        Color linhaBase = Color.FromArgb(0x46, 0x4c, 0x51);
                        Color corDoLogo = Color.FromArgb(0xa9, 0xc8, 0xb8);

                        for (int y = 0; y < altura; y++)
                        {
                            float t = (float)((y - DES_Y0) / DES_ALTURA);
                            if (t < 0f) t = 0f;
                            if (t > 1f) t = 1f;

                            Color fundo = Misturar(fundoTopo, fundoBase, t);
                            Color linha = t < 0.5f
                                ? Misturar(linhaTopo, linhaMeio, t * 2f)
                                : Misturar(linhaMeio, linhaBase, (t - 0.5f) * 2f);

                            int inicio = y * largura;
                            for (int x = 0; x < largura; x++)
                            {
                                int argb = pixels[inicio + x];
                                int alfa = (argb >> 24) & 0xFF;
                                if (alfa == 0) continue;

                                int r = (argb >> 16) & 0xFF;
                                int g = (argb >> 8) & 0xFF;
                                int b = argb & 0xFF;

                                // o logo do meio era azul; o prototipo usa o verde do realce
                                if (b > r + 40 && b > g + 20)
                                {
                                    pixels[inicio + x] = Color.FromArgb(alfa, corDoLogo).ToArgb();
                                    continue;
                                }

                                int luma = (r * 299 + g * 587 + b * 114) / 1000;
                                float cobertura = 1f - luma / 255f;   // 0 = branco, 1 = preto
                                Color cor = Misturar(fundo, linha, cobertura);
                                pixels[inicio + x] = Color.FromArgb(alfa, cor).ToArgb();
                            }
                        }

                        Marshal.Copy(pixels, 0, escrita.Scan0, total);
                    }
                    finally
                    {
                        origem.UnlockBits(leitura);
                        destino.UnlockBits(escrita);
                    }
                    return destino;
                }
            }
            catch (Exception) { return original; }
        }

        /// <summary>Mistura duas cores (0 = a, 1 = b).</summary>
        private static Color Misturar(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        /// <summary>Ponto da imagem (x,y) em pixels da tela.</summary>
        private PointF P(double x, double y)
        {
            return new PointF((float)(_canto.X + x * _escala), (float)(_canto.Y + y * _escala));
        }

        private float PX(double x) { return (float)(_canto.X + x * _escala); }
        private float PY(double y) { return (float)(_canto.Y + y * _escala); }

        /// <summary>Medida da imagem (comprimento, raio) em pixels da tela.</summary>
        private float RD(double v) { return (float)(v * _escala); }

        /// <summary>
        /// Espessura de traco das pecas que NOS desenhamos (as abas dos gatilhos e os aneis do teste ao
        /// vivo). Precisa acompanhar a escala: a imagem ampliada engrossa sozinha, e com uma espessura
        /// fixa as nossas pecas ficariam finas ao lado das linhas dela.
        /// </summary>
        private float LN(float basePixels)
        {
            float v = basePixels * _escala;
            if (v < 1.2f) v = 1.2f;
            if (v > 6f) v = 6f;
            return v;
        }

        // =====================================================================================
        // lugares medidos na imagem (Entrada 89 do REA-FINDINGS.md)
        // =====================================================================================

        private const double LOGO_X = 340, LOGO_Y = 166.5, LOGO_R = 31;     // botao guia (o logotipo)
        private const double BACK_X = 291, BACK_Y = 233.5, BACK_R = 22.5;   // "Voltar" (a esquerda do guia)
        private const double INI_X = 388, INI_Y = 233.5, INI_R = 22.5;      // "Iniciar" (a direita do guia)
        private const double AE_X = 175, AE_Y = 222, ARO_R = 42.5, TAMPA_R = 26.5;    // analogico esquerdo
        private const double AD_X = 433, AD_Y = 337.5;                                // analogico direito
        private const double DP_X = 241, DP_Y = 352, DP_R = 58.5;                     // direcional
        private const double ABXY_X = 513, ABXY_Y = 237.7, ABXY_PASSO = 46.7, ABXY_R = 26.5;
        private const double BORDA = 4;      // o quanto a ponta da linha fica FORA do botao

        // Linha de FORA do ombro esquerdo, medida coluna por coluna na imagem (Entrada 90). e por ela
        // que a aba do gatilho se guia, para ficar paralela ao ombro do desenho.
        private static readonly double[,] OMBRO_ESQ = new double[,] {
            { 110, 152 }, { 118, 145 }, { 130, 131 }, { 142, 122 }, { 154, 116 },
            { 166, 111 }, { 178, 107 }, { 190, 104 }, { 202, 101 }, { 214, 99 }
        };

        /// <summary>
        /// Onde a linha de chamada de cada alvo encosta no desenho. O ponto e posto na BORDA de cada
        /// botao (nao no meio dele): a linha e desenhada por cima do controle, entao ela precisa parar
        /// exatamente onde o botao comeca, senao parece que aponta para o lugar errado.
        /// </summary>
        private PointF Ancora(Alvo a)
        {
            switch (a.Nome)
            {
                case "LT": return P(180, 88);
                case "RT": return P(IMG_LARGURA - 180, 88);
                case "LB": return P(176, 132);
                case "RB": return P(IMG_LARGURA - 176, 132);
                case "BACK": return P(BACK_X, BACK_Y - BACK_R - BORDA);
                case "START": return P(INI_X, INI_Y - INI_R - BORDA);
                case "Y": return P(ABXY_X, ABXY_Y - ABXY_PASSO - ABXY_R - BORDA);
                case "B": return P(ABXY_X + ABXY_PASSO + ABXY_R + BORDA, ABXY_Y);
                // o X e o botao da ESQUERDA do losango, mas a caixinha dele fica na coluna da direita:
                // encostando a linha na borda esquerda, ela atravessava por cima do B e parecia ligada a
                // ele (defeito visto na foto imagem-2.png, 2026-10-18). Pela borda de baixo a linha passa
                // por baixo do B e nao cruza mais nada.
                case "X": return P(ABXY_X - ABXY_PASSO, ABXY_Y + ABXY_R + BORDA);
                case "A": return P(ABXY_X, ABXY_Y + ABXY_PASSO + ABXY_R + BORDA);
                case "UP": return P(DP_X, DP_Y - DP_R - BORDA);
                case "DOWN": return P(DP_X, DP_Y + DP_R + BORDA);
                case "LEFT": return P(DP_X - DP_R - BORDA, DP_Y);
                case "RIGHT": return P(DP_X + DP_R + BORDA, DP_Y);
                case "LX": return a.Sentido > 0 ? P(AE_X + ARO_R + BORDA, AE_Y) : P(AE_X - ARO_R - BORDA, AE_Y);
                case "LY": return a.Sentido > 0 ? P(AE_X, AE_Y - ARO_R - BORDA) : P(AE_X, AE_Y + ARO_R + BORDA);
                case "RX": return a.Sentido > 0 ? P(AD_X + ARO_R + BORDA, AD_Y) : P(AD_X - ARO_R - BORDA, AD_Y);
                case "RY": return a.Sentido > 0 ? P(AD_X, AD_Y - ARO_R - BORDA) : P(AD_X, AD_Y + ARO_R + BORDA);
                // os cliques entram na diagonal de baixo: se ficassem embaixo do analogico, ficariam
                // na mesma direcao de "L.analog. v" e as duas linhas pareceriam uma so
                case "LS": return P(AE_X + 22, AE_Y + 22);
                case "RS": return P(AD_X + 22, AD_Y + 22);
                default: return P(ABXY_X, ABXY_Y);
            }
        }

        /// <summary>Reorganiza caixinhas e o lugar do desenho no tamanho atual.</summary>
        private void Recalcular()
        {
            int W = ClientSize.Width, H = ClientSize.Height;
            if (W < 60 || H < 60) return;

            int folga = 6;
            int altura = 30;
            // as colunas laterais so precisam caber "L.analog. ->" e a tecla: 136 e suficiente, e quanto
            // mais estreitas, MAIOR fica o controle no meio (a foto redesenho-7.png mostrou o controle
            // pequeno justamente por causa da largura reservada aqui)
            int larguraLateral = Math.Max(112, Math.Min(136, (W - 240) / 2));

            // primeiro o lugar do desenho: a posicao das caixinhas depende dele (a ordem das caixinhas
            // na coluna e a ordem em que as linhas encontram o desenho; ver abaixo)
            float livreX = folga + larguraLateral + 8;
            float livreL = W - 2 * livreX;
            float livreY = folga + altura + 12;
            float livreA = H - livreY - altura - folga - 10;
            if (livreL < 80) livreL = 80;
            if (livreA < 80) livreA = 80;

            // o desenho do controle e escalado mantendo a proporcao da imagem (674 x 481 no desenho)
            float escala = Math.Min((float)(livreL / DES_LARGURA), (float)(livreA / DES_ALTURA));
            if (escala < 0.05f) escala = 0.05f;
            _escala = escala;

            // canto do pixel (0,0) da imagem: o DESENHO fica centrado na area livre, e a margem
            // transparente da imagem (o resto) sai para fora dela
            float esq = livreX + (livreL - (float)DES_LARGURA * escala) / 2f;
            float topo = livreY + (livreA - (float)DES_ALTURA * escala) / 2f;
            _canto = new PointF((float)(esq - DES_X0 * escala), (float)(topo - DES_Y0 * escala));
            _area = new RectangleF(PX(0), PY(0), RD(IMG_LARGURA), RD(IMG_ALTURA));

            int i;

            // fileira de cima: larguras diferentes de proposito (Voltar/Iniciar precisam de mais espaco
            // porque "Backspace" e "Enter" sao nomes compridos)
            double[] partes = { 0.145, 0.135, 0.220, 0.220, 0.135, 0.145 };
            int util = W - 2 * folga - 5 * folga;
            int xT = folga;
            for (i = 0; i < 6; i++)
            {
                int largCaixa = (int)Math.Round(util * partes[i]);
                if (largCaixa < 56) largCaixa = 56;
                _alvos[i].Caixa = new Rectangle(xT, folga, largCaixa, altura);
                xT += largCaixa + folga;
            }

            // fileira de baixo (LS e RS)
            int larguraBaixo = Math.Max(110, larguraLateral);
            int xB = W / 2 - (larguraBaixo * 2 + folga) / 2;
            for (i = 22; i < 24; i++)
            {
                _alvos[i].Caixa = new Rectangle(xB, H - folga - altura, larguraBaixo, altura);
                xB += larguraBaixo + folga;
            }

            // colunas da esquerda e da direita: a ORDEM e por onde a linha encontra o desenho (de cima
            // para baixo). Assim as linhas nao se cruzam - foi o defeito apontado pelo usuario
            // ("as linhas nao seguem para os botoes corretamente", 2026-10-18).
            // EXCECAO (2026-10-18, pedido do usuario): na coluna da direita a ordem de desenho do "A" e
            // a do "R. analogico cima" sao trocadas entre si - ver AlturaParaOrdenar().
            List<Alvo> esquerda = new List<Alvo>();
            List<Alvo> direita = new List<Alvo>();
            foreach (Alvo a in _alvos)
            {
                if (a.Lado == 1) esquerda.Add(a);
                else if (a.Lado == 2) direita.Add(a);
            }
            esquerda.Sort(PorAlturaNoDesenho);
            direita.Sort(PorAlturaNoDesenho);

            int passoY = altura + 10;
            int blocoY = 8 * passoY - 10;
            int topoY = folga + altura + 14;
            int sobraY = (H - topoY - (folga + altura + 10)) - blocoY;
            if (sobraY > 0) topoY += sobraY / 2;

            int y = topoY;
            foreach (Alvo a in esquerda)
            {
                a.Caixa = new Rectangle(folga, y, larguraLateral, altura);
                y += passoY;
            }

            y = topoY;
            foreach (Alvo a in direita)
            {
                a.Caixa = new Rectangle(W - folga - larguraLateral, y, larguraLateral, altura);
                y += passoY;
            }
        }

        private int PorAlturaNoDesenho(Alvo a, Alvo b)
        {
            int r = AlturaParaOrdenar(a).CompareTo(AlturaParaOrdenar(b));
            if (r != 0) return r;
            return _alvos.IndexOf(a).CompareTo(_alvos.IndexOf(b));
        }

        /// <summary>
        /// A altura que decide a ORDEM da caixinha na coluna. Nao e sempre a ancora: a linha continua
        /// encostando no lugar medido da imagem (ver <see cref="Ancora"/>), mas a caixinha pode ser
        /// desenhada em outra posicao da coluna.
        ///
        /// 2026-10-18 - pedido do usuario: "vamos apenas inverter o R.analogico cima pelo A, pois esta
        /// inconsistente os comandos do analogico ficarem separados". Ordenando so pela ancora, a coluna
        /// da direita saia  Y, B, X, "R. analogico cima", A, "R. dir.", "R. esq.", "R. baixo": o botao A
        /// caia no MEIO dos quatro comandos do analogico direito (ancora do A em 314,9 e a do
        /// "R. analogico cima" em 291,0 - 24 px de diferenca) e os partia em dois grupos. Trocando a
        /// altura de ordenacao dessas duas caixinhas, os botoes ficam  Y, B, X, A  e os quatro comandos
        /// do analogico ficam juntos, sem que nada mude de dono nem de lugar no desenho. Consequencia
        /// conhecida e aceita: as linhas dessas duas caixinhas se cruzam uma vez, de leve.
        /// </summary>
        private float AlturaParaOrdenar(Alvo a)
        {
            if (string.Equals(a.Nome, "A", StringComparison.Ordinal))
            {
                Alvo cima = Achar("RY", 1);
                if (cima != null) return Ancora(cima).Y;
            }
            else if (string.Equals(a.Nome, "RY", StringComparison.Ordinal) && a.Sentido == 1)
            {
                Alvo botaoA = Achar("A");
                if (botaoA != null) return Ancora(botaoA).Y;
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

        // =====================================================================================
        // clique e mouse em cima
        // =====================================================================================

        private Alvo AlvoNoPonto(Point p)
        {
            foreach (Alvo a in _alvos)
                if (a.Caixa.Contains(p)) return a;

            if (_area.IsEmpty || _escala <= 0f) return null;

            // botoes redondos A/B/X/Y (um pouco maiores que o circulo, para ser facil de acertar)
            if (Cerca(p, ABXY_X, ABXY_Y + ABXY_PASSO, ABXY_R + 6)) return Achar("A");
            if (Cerca(p, ABXY_X + ABXY_PASSO, ABXY_Y, ABXY_R + 6)) return Achar("B");
            if (Cerca(p, ABXY_X - ABXY_PASSO, ABXY_Y, ABXY_R + 6)) return Achar("X");
            if (Cerca(p, ABXY_X, ABXY_Y - ABXY_PASSO, ABXY_R + 6)) return Achar("Y");

            // gatilhos: a aba desenhada por nos, acima do ombro (o desenho do usuario nao tem gatilho)
            using (GraphicsPath abaE = AbaGatilho(true))
                if (abaE.IsVisible(p)) return Achar("LT");
            using (GraphicsPath abaD = AbaGatilho(false))
                if (abaD.IsVisible(p)) return Achar("RT");

            // ombros: a faixa do desenho entre a linha de fora e a segunda linha interna
            if (FaixaOmbro(true).Contains(p)) return Achar("LB");
            if (FaixaOmbro(false).Contains(p)) return Achar("RB");
            // voltar / iniciar
            if (Cerca(p, BACK_X, BACK_Y, BACK_R + 3)) return Achar("BACK");
            if (Cerca(p, INI_X, INI_Y, INI_R + 3)) return Achar("START");

            // direcional
            if (Cerca(p, DP_X, DP_Y, DP_R))
            {
                float dx = p.X - PX(DP_X), dy = p.Y - PY(DP_Y);
                if (Math.Abs(dx) > Math.Abs(dy)) return Achar(dx > 0 ? "RIGHT" : "LEFT");
                return Achar(dy > 0 ? "DOWN" : "UP");
            }

            // analogicos: a tampa central e o clique (LS/RS); o resto do aro, as direcoes
            if (Cerca(p, AE_X, AE_Y, ARO_R))
            {
                if (Cerca(p, AE_X, AE_Y, TAMPA_R * 0.85)) return Achar("LS");
                float dx = p.X - PX(AE_X), dy = p.Y - PY(AE_Y);
                if (Math.Abs(dx) > Math.Abs(dy)) return Achar("LX", dx > 0 ? 1 : -1);
                return Achar("LY", dy > 0 ? -1 : 1);
            }
            if (Cerca(p, AD_X, AD_Y, ARO_R))
            {
                if (Cerca(p, AD_X, AD_Y, TAMPA_R * 0.85)) return Achar("RS");
                float dx = p.X - PX(AD_X), dy = p.Y - PY(AD_Y);
                if (Math.Abs(dx) > Math.Abs(dy)) return Achar("RX", dx > 0 ? 1 : -1);
                return Achar("RY", dy > 0 ? 1 : -1);
            }

            return null;
        }

        private bool Cerca(Point p, double nx, double ny, double raio)
        {
            float dx = p.X - PX(nx), dy = p.Y - PY(ny);
            float r = RD(raio);
            return dx * dx + dy * dy <= r * r;
        }

        private Alvo Achar(string nome)
        {
            return Achar(nome, 0);
        }

        private Alvo Achar(string nome, int sentido)
        {
            foreach (Alvo a in _alvos)
                if (string.Equals(a.Nome, nome, StringComparison.Ordinal) && a.Sentido == sentido) return a;
            return null;
        }

        private bool Aceso(string nome)
        {
            Alvo a = Achar(nome);
            return a != null && a.Aceso;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Alvo a = AlvoNoPonto(e.Location);
            string chave = a == null ? null : a.Chave;
            if (chave != _passando)
            {
                _passando = chave;
                Cursor = chave == null ? Cursors.Default : Cursors.Hand;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_passando != null) { _passando = null; Cursor = Cursors.Default; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Alvo a = AlvoNoPonto(e.Location);
            if (a != null && e.Button == MouseButtons.Left)
            {
                Selecionado = a.Chave;
                if (Selecionou != null) Selecionou(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }

        // =====================================================================================
        // desenho
        // =====================================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_area.IsEmpty) Recalcular();

            using (SolidBrush fundo = new SolidBrush(Tema.Fundo)) g.FillRectangle(fundo, ClientRectangle);

            // 1) o controle (a imagem do usuario) e o que acender nele
            if (!_area.IsEmpty) DesenharControle(g);

            // 2) as linhas de chamada POR CIMA do controle: se fossem por baixo, sumiriam debaixo do
            //    corpo branco e pareceria que apontam para o lugar errado (reclamacao do usuario,
            //    2026-10-18). Cada linha termina na borda do botao (ver Ancora).
            if (!_area.IsEmpty)
                foreach (Alvo a in _alvos) DesenharLinha(g, a);

            // 3) dica de estado: escrita ABAIXO do controle e limitada a largura dele - antes ela ficava
            //    no pe do desenho, atravessava as caixinhas laterais e ainda era coberta pela caixinha de
            //    LS/RS (defeito visto nas fotos redesenho-6-zoom.png e redesenho-7.png, 2026-10-18).
            if (!_temLeitura)
            {
                float y = _area.Bottom + 6;
                if (y + 16 <= ClientSize.Height - 42)
                {
                    using (SolidBrush tinta = new SolidBrush(Tema.Texto3))
                    using (StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    })
                        g.DrawString("o pad esta desligado: ligue no botao 'Ligar o pad'",
                                     Tema.Fonte8, tinta, new RectangleF(_area.Left, y, _area.Width, 16), sf);
                }
            }

            // 4) as caixinhas (por ultimo, para nada cobrir o texto delas)
            foreach (Alvo a in _alvos) DesenharCaixa(g, a);
        }

        private void DesenharLinha(Graphics g, Alvo a)
        {
            if (a.Caixa.Width == 0) return;
            PointF fim = Ancora(a);
            PointF inicio;
            switch (a.Lado)
            {
                case 0: inicio = new PointF(a.Caixa.Left + a.Caixa.Width / 2f, a.Caixa.Bottom); break;
                case 1: inicio = new PointF(a.Caixa.Right, a.Caixa.Top + a.Caixa.Height / 2f); break;
                case 2: inicio = new PointF(a.Caixa.Left, a.Caixa.Top + a.Caixa.Height / 2f); break;
                default: inicio = new PointF(a.Caixa.Left + a.Caixa.Width / 2f, a.Caixa.Top); break;
            }

            // a linha passa POR CIMA do corpo branco do controle: com transparencia baixa ela sumia
            // (por isso os valores de alfa aqui sao altos, ao contrario dos tons do resto do tema)
            bool forte = string.Equals(a.Chave, _selecionado, StringComparison.Ordinal);
            Color cor = forte ? Tema.Realce
                       : (a.Tecla != null ? Color.FromArgb(200, Tema.Realce) : Color.FromArgb(130, Tema.Texto2));

            float meioX = (inicio.X + fim.X) / 2f;
            using (Pen caneta = new Pen(cor, forte ? 1.8f : 1.2f))
            {
                if (a.Lado == 0 || a.Lado == 3)
                    g.DrawBezier(caneta, inicio, new PointF(inicio.X, inicio.Y + (fim.Y - inicio.Y) / 2f),
                                 new PointF(fim.X, inicio.Y + (fim.Y - inicio.Y) / 2f), fim);
                else
                    g.DrawBezier(caneta, inicio, new PointF(meioX, inicio.Y), new PointF(meioX, fim.Y), fim);
            }

            using (SolidBrush ponto = new SolidBrush(cor))
                g.FillEllipse(ponto, fim.X - 2.5f, fim.Y - 2.5f, 5f, 5f);
        }

        private void DesenharCaixa(Graphics g, Alvo a)
        {
            if (a.Caixa.Width == 0) return;
            bool sel = string.Equals(a.Chave, _selecionado, StringComparison.Ordinal);
            bool pass = string.Equals(a.Chave, _passando, StringComparison.Ordinal);

            Color fundo = sel ? Color.FromArgb(52, 62, 60) : (pass ? Tema.Painel2 : Tema.Painel2);
            using (SolidBrush b = new SolidBrush(fundo))
            using (GraphicsPath caminho = Arredondado(a.Caixa, 6))
                g.FillPath(b, caminho);

            using (Pen p = new Pen(sel ? Tema.Realce : (a.Aceso ? Tema.Aceso : Tema.Borda), sel ? 1.8f : 1f))
            using (GraphicsPath caminho = Arredondado(a.Caixa, 6))
                g.DrawPath(p, caminho);

            Rectangle texto = new Rectangle(a.Caixa.Left + 6, a.Caixa.Top, a.Caixa.Width - 12, a.Caixa.Height);

            // Marcador de "sem tecla": o texto comprido ("(sem tecla)") empurrava o rotulo do analogico,
            // que precisa de ~77 px dos 114 px da caixinha, e o rotulo saia cortado ("L. analogic...").
            // "vazio" e curto e diz a mesma coisa; a caixinha apagada e a lista do painel da direita ja
            // avisam que o alvo nao tem tecla (medido em 2026-10-18, Entrada 104).
            string direita = a.Tecla ?? "vazio";
            Font fonteDireita = a.Tecla != null ? Tema.Fonte8N : Tema.FonteMicro;
            Font fonteRotulo = Tema.FonteMicro;

            // A largura de cada pedaco e MEDIDA, nao fixada em porcentagem: a caixinha de baixo
            // ("L.analog. ->") tem so 136 pixels e com a porcentagem fixa o rotulo saia cortado; ja a
            // caixinha de cima ("Iniciar" + "Enter") tem rotulo curto e tecla comprida. Medindo, cada
            // caso fica do tamanho que precisa (defeito visto na foto redesenho-8.png, 2026-10-18).
            //
            // 2026-10-21 (desenho novo): o rotulo passou a usar a fonte mono de 8 px (~9 px na tela,
            // por causa da escala do desenho). A 9 px, "L. analogico cima" + "vazio" nao cabia nos
            // 114 px da caixinha e saia "L. analogico...". Agora, quando os dois cabem, o rotulo fica
            // inteiro e a tecla recebe a sobra; quando nao cabem, vale a regra antiga (a tecla encolhe).
            float precisaDoRotulo = g.MeasureString(a.Rotulo, fonteRotulo).Width;
            float precisaDaTecla = g.MeasureString(direita, fonteDireita).Width + 2f;
            float larguraRotulo, larguraTecla;
            if (precisaDoRotulo + precisaDaTecla + 2f <= texto.Width)
            {
                larguraRotulo = precisaDoRotulo;
                larguraTecla = texto.Width - larguraRotulo - 2f;
            }
            else
            {
                larguraTecla = Math.Min(texto.Width * 0.55f, precisaDaTecla);
                larguraRotulo = texto.Width - larguraTecla - 2f;
                if (larguraRotulo < texto.Width * 0.30f) { larguraRotulo = texto.Width * 0.30f; larguraTecla = texto.Width - larguraRotulo - 2f; }
            }

            using (SolidBrush tinta = new SolidBrush(a.Tecla != null ? Tema.Texto2 : Tema.Texto3))
            using (StringFormat sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(a.Rotulo, fonteRotulo, tinta, new RectangleF(texto.Left, texto.Top, larguraRotulo, texto.Height), sf);

            using (SolidBrush tinta = new SolidBrush(a.Tecla != null ? (a.Aceso ? Tema.Aceso : Tema.Texto) : Tema.Texto3))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
                g.DrawString(direita, fonteDireita, tinta,
                             new RectangleF(texto.Left + larguraRotulo + 2f, texto.Top, larguraTecla, texto.Height), sf);
        }

        private void DesenharControle(Graphics g)
        {
            // 1) as ABAS dos gatilhos, atras da imagem: o desenho do usuario nao tem gatilho nenhum
            //    (Entrada 90), entao LT/RT precisam de uma peca para a linha ter onde encostar
            DesenharAbaGatilho(g, true, Achar("LT"));
            DesenharAbaGatilho(g, false, Achar("RT"));

            // 2) a imagem do controle
            DesenharImagem(g);

            // 3) o que a imagem nao pinta: a cor dos botoes A/B/X/Y e o que o pad esta mandando
            DesenharBotao(g, Achar("A"), ABXY_X, ABXY_Y + ABXY_PASSO, "A", Color.FromArgb(0xb9, 0xdc, 0xc6));
            DesenharBotao(g, Achar("B"), ABXY_X + ABXY_PASSO, ABXY_Y, "B", Color.FromArgb(0xc8, 0x93, 0x8d));
            DesenharBotao(g, Achar("X"), ABXY_X - ABXY_PASSO, ABXY_Y, "X", Color.FromArgb(0x8e, 0xab, 0xc6));
            DesenharBotao(g, Achar("Y"), ABXY_X, ABXY_Y - ABXY_PASSO, "Y", Color.FromArgb(0xd8, 0xbf, 0x7f));

            DesenharBolinha(g, Achar("BACK"), BACK_X, BACK_Y, BACK_R);
            DesenharBolinha(g, Achar("START"), INI_X, INI_Y, INI_R);

            DesenharDirecional(g);
            DesenharAnalogico(g, true, Achar("LX", 1), Achar("LX", -1), Achar("LY", 1), Achar("LY", -1), Achar("LS"));
            DesenharAnalogico(g, false, Achar("RX", 1), Achar("RX", -1), Achar("RY", 1), Achar("RY", -1), Achar("RS"));

            // 4) anel do alvo escolhido: um anel PEQUENO em volta da ponta da linha. Com 26 pixels ele
            //    cobria metade do botao e confundia (foto imagem-2.png); a ponta (o pontinho) e que marca
            //    o lugar exato no botao.
            Alvo sel = null;
            foreach (Alvo a in _alvos) if (string.Equals(a.Chave, _selecionado, StringComparison.Ordinal)) sel = a;
            if (sel != null)
            {
                PointF c = Ancora(sel);
                float r = RD(11);
                using (Pen p = new Pen(Tema.Realce, 2f))
                    g.DrawEllipse(p, c.X - r, c.Y - r, 2 * r, 2 * r);
            }
        }

        /// <summary>A imagem do usuario, escalada e centrada (com as margens transparentes fora da area).</summary>
        private void DesenharImagem(Graphics g)
        {
            Image img = Imagem();
            if (img == null) return;

            InterpolationMode modoAntes = g.InterpolationMode;
            PixelOffsetMode deslocAntes = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Rectangle destino = new Rectangle(
                (int)Math.Round(PX(0)), (int)Math.Round(PY(0)),
                (int)Math.Round(RD(IMG_LARGURA)), (int)Math.Round(RD(IMG_ALTURA)));

            // WrapMode.TileFlipXY evita a faixa clara que aparece na borda quando se amplia uma imagem
            using (ImageAttributes atributos = new ImageAttributes())
            {
                atributos.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(img, destino, 0, 0, IMG_LARGURA, IMG_ALTURA, GraphicsUnit.Pixel, atributos);
            }

            g.InterpolationMode = modoAntes;
            g.PixelOffsetMode = deslocAntes;
        }

        /// <summary>
        /// A aba do gatilho (LT/RT): uma tira PARALELA a linha de fora do ombro, desenhada atras da
        /// imagem - so a parte de cima dela aparece, que e o gatilho do controle de verdade.
        /// </summary>
        private GraphicsPath AbaGatilho(bool esquerdo)
        {
            // sobe: quanto a aba fica acima da linha de fora do ombro (e o pedaco que aparece).
            // desce: quanto a aba entra ATRAS do corpo. Tem que ser negativo (para dentro do corpo):
            // com a base parada antes da linha de fora a aba ficava flutuando, com um vao escuro entre
            // ela e o ombro (defeito visto na foto imagem-1.png, 2026-10-18).
            const double sobe = 30, desce = -16;
            int n = OMBRO_ESQ.GetLength(0);
            PointF[] pontos = new PointF[2 * n];
            for (int i = 0; i < n; i++)
            {
                double x = OMBRO_ESQ[i, 0], y = OMBRO_ESQ[i, 1];
                if (!esquerdo) x = IMG_LARGURA - x;
                pontos[i] = P(x, y - sobe);              // borda de cima, da esquerda para a direita
                pontos[2 * n - 1 - i] = P(x, y - desce); // borda de baixo, de volta (dentro do corpo)
            }

            GraphicsPath caminho = new GraphicsPath();
            caminho.AddPolygon(pontos);
            return caminho;
        }

        /// <summary>A faixa do ombro no desenho (onde a linha de LB/RB encosta).</summary>
        private RectangleF FaixaOmbro(bool esquerdo)
        {
            return esquerdo
                ? RectangleF.FromLTRB(PX(112), PY(100), PX(228), PY(148))
                : RectangleF.FromLTRB(PX(IMG_LARGURA - 228), PY(100), PX(IMG_LARGURA - 112), PY(148));
        }

        private void DesenharAbaGatilho(Graphics g, bool esquerdo, Alvo a)
        {
            using (GraphicsPath caminho = AbaGatilho(esquerdo))
            {
                using (SolidBrush fundo = new SolidBrush(Color.FromArgb(0x1b, 0x20, 0x23)))
                    g.FillPath(fundo, caminho);

                // quanto do gatilho esta puxado: enche de baixo para cima, como no controle de verdade
                if (a != null && a.Valor > 0.01f)
                {
                    RectangleF c = caminho.GetBounds();
                    Region antigo = g.Clip;
                    g.SetClip(caminho);
                    float altura = c.Height * a.Valor;
                    using (SolidBrush cheio = new SolidBrush(Tema.Aceso))
                        g.FillRectangle(cheio, c.Left, c.Bottom - altura, c.Width, altura);
                    g.Clip = antigo;
                }

                using (Pen contorno = new Pen(Color.FromArgb(0x48, 0x51, 0x57), LN(2.4f)))
                    g.DrawPath(contorno, caminho);
            }
        }

        /// <summary>Circulos de Voltar e Iniciar: a imagem ja os desenha; aqui so acende quando o pad manda.</summary>
        private void DesenharBolinha(Graphics g, Alvo a, double x, double y, double r)
        {
            if (a == null || !a.Aceso) return;
            float cx = PX(x), cy = PY(y), raio = RD(r) + 3;
            using (Pen p = new Pen(Tema.Aceso, LN(2.5f)))
                g.DrawEllipse(p, cx - raio, cy - raio, 2 * raio, 2 * raio);
        }

        /// <summary>
        /// Os botoes A/B/X/Y: no desenho novo o circulo e escuro (#141b1e) e a LETRA leva a cor do
        /// botao (A #b9dcc6, B #c8938d, X #8eabc6, Y #d8bf7f), como no prototipo. Antes o circulo
        /// era cheio de cor e a letra branca. Quando o pad manda o botao, ele ganha um anel aceso.
        /// </summary>
        private void DesenharBotao(Graphics g, Alvo a, double x, double y, string letra, Color cor)
        {
            float cx = PX(x), cy = PY(y);
            float r = RD(ABXY_R - 1.5f);   // um pouco menor: nao pode apagar o contorno da imagem

            using (SolidBrush fundo = new SolidBrush(Color.FromArgb(0x14, 0x1b, 0x1e)))
                g.FillEllipse(fundo, cx - r, cy - r, 2 * r, 2 * r);

            // o anel do botao: no prototipo o circulo tem contorno proprio (#485157, 1.3 px).
            using (Pen borda = new Pen(Color.FromArgb(0x48, 0x51, 0x57), LN(1.3f)))
                g.DrawEllipse(borda, cx - r, cy - r, 2 * r, 2 * r);

            if (a != null && a.Aceso)
                using (Pen p = new Pen(Tema.Aceso, LN(2.4f)))
                    g.DrawEllipse(p, cx - r - 3.5f, cy - r - 3.5f, 2 * (r + 3.5f), 2 * (r + 3.5f));

            using (SolidBrush tinta = new SolidBrush(cor))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(letra, FonteLetra(2 * r * 0.78f), tinta,
                             new RectangleF(cx - r, cy - r, 2 * r, 2 * r), sf);
        }

        /// <summary>
        /// O direcional: a imagem desenha o circulo com a grade 3x3; aqui a direcao que o pad esta
        /// mandando fica preenchida com a cor acesa.
        /// </summary>
        private void DesenharDirecional(Graphics g)
        {
            float cx = PX(DP_X), cy = PY(DP_Y), r = RD(DP_R - 4);
            if (Aceso("UP")) Fatia(g, cx, cy, r, -135, -45);
            if (Aceso("RIGHT")) Fatia(g, cx, cy, r, -45, 45);
            if (Aceso("DOWN")) Fatia(g, cx, cy, r, 45, 135);
            if (Aceso("LEFT")) Fatia(g, cx, cy, r, 135, 225);
        }

        private void Fatia(Graphics g, float cx, float cy, float r, float de, float ate)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(200, Tema.Aceso)))
                g.FillPie(b, cx - r, cy - r, 2 * r, 2 * r, de, ate - de);
        }

        /// <summary>
        /// O analogico: a imagem ja desenha o aro de fora e a tampa. Aqui a TAMPA anda conforme o pad
        /// esta empurrando, o clique (LS/RS) acende a tampa e o aro fica aceso quando ha alguma direcao.
        /// </summary>
        private void DesenharAnalogico(Graphics g, bool esquerdo, Alvo eixoX, Alvo eixoXmenos, Alvo eixoY, Alvo eixoYmenos, Alvo clique)
        {
            double nx = esquerdo ? AE_X : AD_X;
            double ny = esquerdo ? AE_Y : AD_Y;
            float cx = PX(nx), cy = PY(ny);
            float aro = RD(ARO_R), tampa = RD(TAMPA_R);

            float dx = 0f, dy = 0f;
            if (_temLeitura)
            {
                dx = (esquerdo ? _lx : _rx) / 32767f;
                dy = -(esquerdo ? _ly : _ry) / 32767f;   // tela: y cresce para baixo
            }
            if (dx > 1f) dx = 1f; if (dx < -1f) dx = -1f;
            if (dy > 1f) dy = 1f; if (dy < -1f) dy = -1f;

            bool aceso = (clique != null && clique.Aceso) ||
                         (eixoX != null && eixoX.Aceso) || (eixoXmenos != null && eixoXmenos.Aceso) ||
                         (eixoY != null && eixoY.Aceso) || (eixoYmenos != null && eixoYmenos.Aceso);

            // a tampa andando: um circulo claro na posicao para onde o pad esta empurrando
            if (dx * dx + dy * dy > 0.02f)
            {
                float tx = cx + dx * (aro - tampa) * 0.85f;
                float ty = cy + dy * (aro - tampa) * 0.85f;
                using (SolidBrush andando = new SolidBrush(Color.FromArgb(120, Tema.Aceso)))
                    g.FillEllipse(andando, tx - tampa, ty - tampa, 2 * tampa, 2 * tampa);
            }

            // o clique do analogico: enche a tampa de cor
            if (clique != null && clique.Aceso)
                using (SolidBrush cheio = new SolidBrush(Color.FromArgb(150, Tema.Aceso)))
                    g.FillEllipse(cheio, cx - tampa, cy - tampa, 2 * tampa, 2 * tampa);

            // o aro aceso quando alguma direcao esta sendo mandada
            if (aceso)
                using (Pen p = new Pen(Tema.Aceso, LN(2.4f)))
                    g.DrawEllipse(p, cx - aro - 1, cy - aro - 1, 2 * (aro + 1), 2 * (aro + 1));
        }

        /// <summary>Fonte da letra do botao, do tamanho pedido em PIXELS (recriada so quando muda).</summary>
        private Font FonteLetra(float pixels)
        {
            if (pixels < 5f) pixels = 5f;
            if (_fonteLetra == null || Math.Abs(_tamFonteLetra - pixels) > 0.25f)
            {
                if (_fonteLetra != null) _fonteLetra.Dispose();
                _fonteLetra = new Font("Segoe UI", pixels, FontStyle.Bold, GraphicsUnit.Pixel);
                _tamFonteLetra = pixels;
            }
            return _fonteLetra;
        }

        private static GraphicsPath Arredondado(RectangleF r, float raio)
        {
            GraphicsPath p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return p;
            float d = raio * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
