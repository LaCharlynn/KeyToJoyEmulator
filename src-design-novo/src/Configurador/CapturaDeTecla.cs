// CapturaDeTecla.cs - janela que pede uma tecla (ou uma combinacao) ao usuario.
//
// Ela usa o MESMO gancho de teclado que o programa usa em tempo de jogo (MyVersion.GanchoTeclado) e
// mostra na tela a identidade crua que o gancho viu: "hook viu: vk=... scan=0x.. flags=0x.. estendida=..".
// Isso e de proposito: a licao do defeito no. 5 foi que traduzir a tecla por outro caminho (MapVirtualKey)
// da errado; aqui o que o usuario ve e exatamente o que o programa vai comparar.
//
// A janela NAO fecha por teclado (nem Esc, nem Enter): so pelos botoes, com o mouse. Assim qualquer
// tecla pode ser mapeada, inclusive Esc e Enter.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MyVersion;

namespace Configurador
{
    public sealed class CapturaDeTecla : Form
    {
        // so para garantir que a thread do gancho ja tenha uma fila de mensagens antes de instalar o hook
        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int x;
            public int y;
        }

        [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
        private static extern bool PeekMessageW(out MSG lpMsg, IntPtr hWnd, uint min, uint max, uint remover);

        private readonly bool _combinacao;
        private readonly List<CodigoTecla> _teclas = new List<CodigoTecla>();
        private readonly ManualResetEvent _pronto = new ManualResetEvent(false);

        private Thread _thread;
        private GanchoTeclado _gancho;
        private string _erroDoGancho;
        private volatile string _ultimaLinhaCrua;

        private readonly Label _lblInstrucao = new Label();
        private readonly Label _lblCru = new Label();
        private readonly Label _lblArquivo = new Label();
        private readonly Label _lblCombinacao = new Label();
        private readonly Panel _quadro = new Panel();
        private readonly Panel _painelCru = new Panel();
        private readonly Button _btnLimpar = new Button();
        private readonly Button _btnUsar = new Button();
        private readonly Button _btnCancelar = new Button();

        private string _textoDoQuadro = "esperando a tecla";
        private bool _temTecla;
        private Font _fonteDoQuadro = Tema.Fonte18N;

        /// <summary>Preenchido quando o usuario confirma uma tecla so.</summary>
        public string NomeCapturado { get; private set; }

        /// <summary>Preenchido quando o usuario confirma uma combinacao (ex.: "Control+Alt+1").</summary>
        public string CombinacaoCapturada { get; private set; }

        public CapturaDeTecla(bool combinacao)
        {
            _combinacao = combinacao;

            Text = combinacao ? "Capturar combinacao" : "Capturar tecla";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(580, 290);
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            Font = Tema.Fonte;
            Tema.IconeDaJanela(this);

            Panel titulo = Tema.RotuloSecao("Captura", combinacao ? "COMBINACAO" : "TECLA");
            titulo.Dock = DockStyle.None;
            titulo.Bounds = new Rectangle(20, 10, 540, 16);

            _lblInstrucao.SetBounds(20, 34, 540, 34);
            _lblInstrucao.ForeColor = Tema.Texto2;
            _lblInstrucao.Font = Tema.Fonte9;
            _lblInstrucao.Text = combinacao
                ? "Aperte as teclas da combinacao (todas juntas, como voce vai usar no jogo).\r\nA janela so fecha nos botoes, com o mouse - qualquer tecla pode ser capturada."
                : "Aperte a tecla que voce quer mapear.\r\nA janela so fecha nos botoes, com o mouse - qualquer tecla pode ser capturada.";

            _quadro.SetBounds(20, 76, 540, 56);
            _quadro.BackColor = Tema.Fundo;
            _quadro.Paint += DesenharQuadro;

            _lblArquivo.SetBounds(20, 140, 540, 18);
            _lblArquivo.ForeColor = Tema.Texto3;
            _lblArquivo.Font = Tema.Fonte9;
            _lblArquivo.AutoEllipsis = true;
            _lblArquivo.Text = "nome no arquivo de perfis: (nada ainda)";

            _lblCombinacao.SetBounds(20, 160, 540, 18);
            _lblCombinacao.ForeColor = Tema.Texto2;
            _lblCombinacao.Font = Tema.Fonte9;
            _lblCombinacao.AutoEllipsis = true;
            _lblCombinacao.Text = "(nenhuma tecla ainda)";
            _lblCombinacao.Visible = combinacao;

            _painelCru.SetBounds(20, 190, 540, 26);
            _painelCru.BackColor = Tema.Borda;
            _painelCru.Padding = new Padding(1);
            _lblCru.Dock = DockStyle.Fill;
            _lblCru.BackColor = Tema.Fundo;
            _lblCru.ForeColor = Tema.Texto3;
            _lblCru.Font = Tema.Fonte75;
            _lblCru.Padding = new Padding(6, 0, 6, 0);
            _lblCru.TextAlign = ContentAlignment.MiddleLeft;
            _lblCru.AutoEllipsis = true;
            _lblCru.Text = "(esperando voce apertar uma tecla...)";
            _painelCru.Controls.Add(_lblCru);

            _btnLimpar.SetBounds(20, 240, 100, 30);
            _btnLimpar.Text = "Limpar";
            Estilo(_btnLimpar, false);
            _btnLimpar.Click += (s, e) => { _teclas.Clear(); _ultimaLinhaCrua = null; Mostrar(); };

            _btnUsar.SetBounds(326, 240, 130, 30);
            _btnUsar.Text = combinacao ? "Usar combinacao" : "Usar esta tecla";
            Estilo(_btnUsar, true);
            _btnUsar.Enabled = false;
            _btnUsar.Click += BotaoUsar;

            _btnCancelar.SetBounds(466, 240, 94, 30);
            _btnCancelar.Text = "Cancelar";
            Estilo(_btnCancelar, false);
            _btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(titulo);
            Controls.Add(_lblInstrucao);
            Controls.Add(_quadro);
            Controls.Add(_lblArquivo);
            Controls.Add(_lblCombinacao);
            Controls.Add(_painelCru);
            Controls.Add(_btnLimpar);
            Controls.Add(_btnUsar);
            Controls.Add(_btnCancelar);
        }

        /// <summary>Botao do tema (o mesmo desenho dos botoes da janela principal).</summary>
        private static void Estilo(Button b, bool principal)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = principal ? Tema.PowerBorda : Tema.Borda;
            b.BackColor = principal ? Tema.Realce : Tema.Painel2;
            b.ForeColor = principal ? Tema.Fundo : Tema.Texto2;
            b.FlatAppearance.MouseOverBackColor = principal ? Tema.BotaoClaroHover : Tema.Hover;
            b.FlatAppearance.MouseDownBackColor = principal ? Tema.Realce : Tema.Painel3;
            b.Font = Tema.Fonte9;
            b.TabStop = false;
            b.Cursor = Cursors.Hand;
        }

        /// <summary>O quadro da tecla: canto arredondado, texto grande no meio.</summary>
        private void DesenharQuadro(object remetente, PaintEventArgs e)
        {
            Rectangle area = new Rectangle(0, 0, _quadro.Width - 1, _quadro.Height - 1);
            using (GraphicsPath caminho = Tema.Retangulo(area, 8))
            {
                using (SolidBrush fundo = new SolidBrush(Tema.ChipFundo))
                    e.Graphics.FillPath(fundo, caminho);
                using (Pen borda = new Pen(Tema.ChipBorda))
                    e.Graphics.DrawPath(borda, caminho);
            }

            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            using (SolidBrush tinta = new SolidBrush(_temTecla ? Tema.Realce : Tema.Texto3))
                e.Graphics.DrawString(_textoDoQuadro, _fonteDoQuadro, tinta,
                                      new RectangleF(8, 0, _quadro.Width - 16, _quadro.Height), sf);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Tema.BarraDeTituloEscura(Handle);

            // nenhum controle com foco: sem isso a barra de espaco "clicaria" o botao focado
            ActiveControl = null;

            _thread = new Thread(RodarGancho);
            _thread.IsBackground = true;
            _thread.Name = "captura-de-tecla";
            _thread.Start();

            if (!_pronto.WaitOne(3000))
            {
                _lblCru.Text = "o gancho de teclado nao respondeu em 3 s";
                return;
            }
            if (_erroDoGancho != null)
            {
                _lblCru.Text = "nao consegui instalar o gancho de teclado: " + _erroDoGancho;
                MessageBox.Show(this, "Nao consegui instalar o gancho de teclado:\r\n" + _erroDoGancho,
                                "captura de tecla", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RodarGancho()
        {
            // forca a criacao da fila de mensagens desta thread antes de instalar o hook de baixo nivel
            MSG lixo;
            PeekMessageW(out lixo, IntPtr.Zero, 0, 0, 0);

            GanchoTeclado g = new GanchoTeclado(AoEvento, AoTexto);
            string erro;
            if (!g.Instalar(out erro))
            {
                _erroDoGancho = erro;
                _pronto.Set();
                return;
            }
            _gancho = g;
            _pronto.Set();
            g.BombearMensagens();
        }

        private void AoTexto(string texto)
        {
            if (texto != null && texto.Contains("apertou")) _ultimaLinhaCrua = texto;
        }

        private void AoEvento(CodigoTecla tecla, bool pressionada)
        {
            if (!pressionada) return;
            try { BeginInvoke(new Action<CodigoTecla>(TeclaApertada), tecla); }
            catch { /* janela fechando */ }
        }

        private void TeclaApertada(CodigoTecla tecla)
        {
            if (_combinacao)
            {
                if (!_teclas.Contains(tecla)) _teclas.Add(tecla);
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
            List<string> nomes = new List<string>();
            bool todosComNome = true;
            foreach (CodigoTecla t in _teclas)
            {
                string n = TabelaTeclas.NomeDe(t);
                if (n == null) { todosComNome = false; n = "SEM NOME (" + t + ")"; }
                nomes.Add(n);
            }

            _lblCru.Text = _ultimaLinhaCrua ?? "(esperando voce apertar uma tecla...)";

            string juntos = string.Join("+", nomes.ToArray());
            _temTecla = nomes.Count > 0;
            _textoDoQuadro = _temTecla
                ? (_combinacao ? string.Join(" + ", nomes.ToArray()) : nomes[0])
                : "esperando a tecla";
            _fonteDoQuadro = TextRenderer.MeasureText(_textoDoQuadro, Tema.Fonte18N).Width <= _quadro.Width - 20
                ? Tema.Fonte18N : Tema.Fonte12N;
            _quadro.Invalidate();

            if (_combinacao)
            {
                _lblCombinacao.Text = nomes.Count == 0 ? "(nenhuma tecla ainda)" : "combinacao: " + string.Join(" + ", nomes.ToArray());
                _lblArquivo.Text = todosComNome
                    ? "vai para o arquivo como: " + juntos
                    : "nome no arquivo de perfis: uma das teclas nao tem nome hoje";
            }
            else
            {
                _lblCombinacao.Text = "";
                _lblArquivo.Text = nomes.Count == 0
                    ? "nome no arquivo de perfis: (nada ainda)"
                    : (todosComNome ? "nome no arquivo de perfis: " + nomes[0] : "nome no arquivo de perfis: esta tecla nao tem nome hoje");
            }

            _btnUsar.Enabled = nomes.Count > 0 && todosComNome;
        }

        private void BotaoUsar(object remetente, EventArgs e)
        {
            List<string> nomes = new List<string>();
            foreach (CodigoTecla t in _teclas)
            {
                string n = TabelaTeclas.NomeDe(t);
                if (n == null) return;      // o botao esta desabilitado nesse caso
                nomes.Add(n);
            }
            if (nomes.Count == 0) return;

            if (_combinacao) CombinacaoCapturada = string.Join("+", nomes.ToArray());
            else NomeCapturado = nomes[0];

            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>Enquanto captura, nenhuma tecla mexe na janela (nem Enter, nem Esc, nem Tab).</summary>
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
            GanchoTeclado g = _gancho;
            if (g == null) return;
            _gancho = null;
            try { g.PedirParada(); } catch { }
            if (_thread != null) { try { _thread.Join(1500); } catch { } }
            try { g.Dispose(); } catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DesligarGancho();
            base.Dispose(disposing);
        }
    }
}
