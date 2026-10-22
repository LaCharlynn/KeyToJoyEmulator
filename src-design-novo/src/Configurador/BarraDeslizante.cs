// BarraDeslizante.cs - a barra deslizante do tema escuro (a do Windows e clara e destoa).
//
// Desenhada por nos: trilho escuro, parte preenchida em verde-agua e um botao redondo. O valor
// exato aparece na caixa (NumericUpDown) que fica ao lado, montada por quem usa esta classe.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Configurador
{
    public sealed class BarraDeslizante : Control
    {
        private int _minimo;
        private int _maximo = 100;
        private int _valor;
        private bool _arrastando;

        /// <summary>Avisa que o usuario mexeu (nao avisa quando o valor muda por codigo).</summary>
        public event EventHandler ValorMudou;

        public BarraDeslizante()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Height = 26;
            BackColor = Tema.Painel;
        }

        public int Minimo
        {
            get { return _minimo; }
            set { _minimo = value; if (_valor < _minimo) _valor = _minimo; Invalidate(); }
        }

        public int Maximo
        {
            get { return _maximo; }
            set { _maximo = value < _minimo + 1 ? _minimo + 1 : value; if (_valor > _maximo) _valor = _maximo; Invalidate(); }
        }

        public int Valor
        {
            get { return _valor; }
            set
            {
                int v = value;
                if (v < _minimo) v = _minimo;
                if (v > _maximo) v = _maximo;
                if (v == _valor) return;
                _valor = v;
                Invalidate();
            }
        }

        /// <summary>Texto que aparece em cima da barra (opcional).</summary>
        public string Etiqueta { get; set; }

        private int XDoValor(int valor)
        {
            int largura = ClientSize.Width - 16;
            if (_maximo <= _minimo) return 8;
            return 8 + (int)Math.Round((double)(valor - _minimo) * largura / (_maximo - _minimo));
        }

        private int ValorDoX(int x)
        {
            int largura = ClientSize.Width - 16;
            if (largura <= 0) return _minimo;
            double fracao = (double)(x - 8) / largura;
            if (fracao < 0) fracao = 0;
            if (fracao > 1) fracao = 1;
            return _minimo + (int)Math.Round(fracao * (_maximo - _minimo));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int meio = ClientSize.Height / 2;
            int xValor = XDoValor(_valor);
            bool ligado = Enabled;

            // trilho
            using (SolidBrush fundo = new SolidBrush(Tema.Painel2))
            using (GraphicsPath caminho = Trilho(8, meio - 3, ClientSize.Width - 16, 6))
                g.FillPath(fundo, caminho);

            // parte preenchida
            if (xValor > 8)
            {
                using (SolidBrush cheio = new SolidBrush(ligado ? Tema.Realce : Tema.Borda))
                using (GraphicsPath caminho = Trilho(8, meio - 3, xValor - 8, 6))
                    g.FillPath(cheio, caminho);
            }

            // botao redondo
            using (SolidBrush bola = new SolidBrush(ligado ? Tema.Texto : Tema.Texto3))
                g.FillEllipse(bola, xValor - 7, meio - 7, 14, 14);

            // etiqueta (se houver)
            if (!string.IsNullOrEmpty(Etiqueta))
            {
                using (SolidBrush tinta = new SolidBrush(Tema.Texto2))
                    g.DrawString(Etiqueta, Tema.Fonte75, tinta, new PointF(10, meio - 22));
            }
        }

        private static GraphicsPath Trilho(int x, int y, int largura, int altura)
        {
            GraphicsPath p = new GraphicsPath();
            int raio = altura;
            if (largura < 2 * raio) largura = 2 * raio;
            p.AddArc(x, y, raio, altura, 90, 180);
            p.AddArc(x + largura - raio, y, raio, altura, 270, 180);
            p.CloseFigure();
            return p;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!Enabled) return;
            _arrastando = true;
            Capture = true;
            Mexer(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_arrastando) Mexer(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _arrastando = false;
            Capture = false;
        }

        private void Mexer(int x)
        {
            int novo = ValorDoX(x);
            if (novo == _valor) return;
            _valor = novo;
            Invalidate();
            if (ValorMudou != null) ValorMudou(this, EventArgs.Empty);
        }

        /// <summary>Poe o valor sem avisar ninguem (usado ao carregar a tela).</summary>
        public void PorValor(int valor)
        {
            int v = valor;
            if (v < _minimo) v = _minimo;
            if (v > _maximo) v = _maximo;
            _valor = v;
            Invalidate();
        }

        /// <summary>Ajusta a faixa e o valor de uma vez (sem avisar).</summary>
        public void Faixa(int minimo, int maximo, int valor)
        {
            _minimo = minimo;
            _maximo = maximo < minimo + 1 ? minimo + 1 : maximo;
            PorValor(valor);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!Enabled) return;
            int passo = Math.Max(1, (_maximo - _minimo) / 100);
            if (e.KeyCode == Keys.Left) { Mexer(XDoValor(_valor - passo)); OnKeyUp(e); }
            if (e.KeyCode == Keys.Right) { Mexer(XDoValor(_valor + passo)); OnKeyUp(e); }
            base.OnKeyDown(e);
        }
    }
}
