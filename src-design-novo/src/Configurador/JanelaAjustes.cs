// JanelaAjustes.cs - os ajustes que NAO sao "configurar botao": descricao do perfil, zona morta,
// qual perfil o pad usa ao iniciar e o intervalo do pad (tique_ms).
//
// Eles ficam num dialogo de proposito: o pedido do usuario (2026-10-18) foi uma tela que sirva para
// "apenas a configuracao dos botoes"; o resto fica guardado aqui, a um clique de distancia.

using System;
using System.Drawing;
using System.Windows.Forms;
using MyVersion;

namespace Configurador
{
    internal sealed class JanelaAjustes : Form
    {
        private readonly ArquivoPerfis _arquivo;
        private readonly Perfil _perfil;

        private readonly TextBox _txtDescricao = Tema.Caixa(false);
        private readonly BarraDeslizante _barraZona = new BarraDeslizante();
        private readonly NumericUpDown _numZona = Tema.Numero(0, 32767, 0);
        private readonly CheckBox _chkInicial = Tema.CaixaDeMarcar("usar este perfil quando o pad iniciar");
        private readonly BarraDeslizante _barraTique = new BarraDeslizante();
        private readonly NumericUpDown _numTique = Tema.Numero(1, 200, 8);

        private bool _sincronizando;

        public static bool Perguntar(IWin32Window dono, ArquivoPerfis arquivo, Perfil perfil)
        {
            using (JanelaAjustes j = new JanelaAjustes(arquivo, perfil))
                return j.ShowDialog(dono) == DialogResult.OK;
        }

        private JanelaAjustes(ArquivoPerfis arquivo, Perfil perfil)
        {
            _arquivo = arquivo;
            _perfil = perfil;

            Text = "Ajustes do perfil e do arquivo";
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            Font = Tema.Fonte;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 400);
            MinimumSize = new Size(560, 380);

            Construir();
            Carregar();

            LigarBarra(_barraZona, _numZona);
            LigarBarra(_barraTique, _numTique);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Tema.BarraDeTituloEscura(Handle);   // a mesma barra de titulo escura da janela principal
            Tema.IconeDaJanela(this);           // a mesma logo da janela principal
        }

        private void Construir()
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.Fundo;
            t.Padding = new Padding(14, 10, 14, 10);
            t.ColumnCount = 2;
            t.RowCount = 9;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));   // titulo
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));   // descricao
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // zona morta
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // inicial
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 12f));   // linha
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // tique
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // explicacao
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // botoes
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 0f));

            Label titulo = Tema.Rotulo("Perfil: " + _perfil.Nome, false);
            titulo.Font = Tema.Fonte10N;
            titulo.ForeColor = Tema.Realce;
            t.Controls.Add(titulo, 0, 0);
            t.SetColumnSpan(titulo, 2);

            t.Controls.Add(Rotulo("Descricao do perfil (aparece na lista do pad)"), 0, 1);
            _txtDescricao.Margin = new Padding(0, 3, 0, 3);
            t.Controls.Add(_txtDescricao, 1, 1);

            t.Controls.Add(Rotulo("Zona morta dos analogicos (0 = nenhuma)"), 0, 2);
            t.Controls.Add(BarraComNumero(_barraZona, _numZona), 1, 2);

            t.Controls.Add(_chkInicial, 0, 3);
            t.SetColumnSpan(_chkInicial, 2);

            Panel linha = new Panel();
            linha.Dock = DockStyle.Fill;
            linha.BackColor = Tema.Borda;
            linha.Margin = new Padding(0, 5, 0, 5);
            t.Controls.Add(linha, 0, 4);
            t.SetColumnSpan(linha, 2);

            t.Controls.Add(Rotulo("Intervalo do pad - tique_ms (o pad le o teclado de tantos em tantos ms)"), 0, 5);
            t.Controls.Add(BarraComNumero(_barraTique, _numTique), 1, 5);

            Label explicacao = Tema.Rotulo(
                "Zona morta: quanto o analogico precisa sair do centro para o pad considerar movimento.\r\n" +
                "Intervalo: mexer aqui nao liga nem desliga o pad; o valor vale quando o pad iniciar.", true);
            explicacao.Font = Tema.Fonte8;
            explicacao.TextAlign = ContentAlignment.TopLeft;
            t.Controls.Add(explicacao, 0, 6);
            t.SetColumnSpan(explicacao, 2);

            TableLayoutPanel botoes = new TableLayoutPanel();
            botoes.Dock = DockStyle.Fill;
            botoes.BackColor = Tema.Fundo;
            botoes.ColumnCount = 3;
            botoes.RowCount = 1;
            botoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            botoes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            botoes.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            botoes.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Button aplicar = Tema.Botao("Aplicar", true);
            Button cancelar = Tema.Botao("Cancelar", false);
            aplicar.Margin = new Padding(0, 3, 6, 3);
            cancelar.Margin = new Padding(0, 3, 0, 3);
            aplicar.Click += delegate { Aplicar(); };
            cancelar.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            botoes.Controls.Add(new Panel { BackColor = Tema.Fundo, Dock = DockStyle.Fill }, 0, 0);
            botoes.Controls.Add(aplicar, 1, 0);
            botoes.Controls.Add(cancelar, 2, 0);
            t.Controls.Add(botoes, 0, 7);
            t.SetColumnSpan(botoes, 2);

            Controls.Add(t);
        }

        private static Label Rotulo(string texto)
        {
            Label l = Tema.Rotulo(texto, false);
            l.Font = Tema.Fonte8;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        private static Control BarraComNumero(BarraDeslizante barra, NumericUpDown numero)
        {
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Tema.Fundo;
            t.ColumnCount = 2;
            t.RowCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            t.Margin = new Padding(0, 3, 0, 3);
            barra.Margin = new Padding(0, 4, 8, 4);
            numero.Margin = new Padding(0, 4, 0, 4);
            t.Controls.Add(barra, 0, 0);
            t.Controls.Add(numero, 1, 0);
            return t;
        }

        private void Carregar()
        {
            _txtDescricao.Text = _perfil.Descricao ?? "";

            _sincronizando = true;
            try
            {
                _barraZona.Faixa(0, 32767, Limitar(_perfil.ZonaMorta, 0, 32767));
                _numZona.Increment = 100;
                _numZona.Value = Limitar(_perfil.ZonaMorta, 0, 32767);

                _barraTique.Faixa(1, 200, Limitar(_arquivo.TiqueMs, 1, 200));
                _numTique.Value = Limitar(_arquivo.TiqueMs, 1, 200);

                _chkInicial.Checked = string.Equals(_arquivo.PerfilInicial, _perfil.Nome, System.StringComparison.OrdinalIgnoreCase);
            }
            finally { _sincronizando = false; }
        }

        private void LigarBarra(BarraDeslizante barra, NumericUpDown numero)
        {
            barra.ValorMudou += delegate
            {
                if (_sincronizando) return;
                _sincronizando = true;
                try { numero.Value = Limitar(barra.Valor, (int)numero.Minimum, (int)numero.Maximum); }
                finally { _sincronizando = false; }
            };
            numero.ValueChanged += delegate
            {
                if (_sincronizando) return;
                _sincronizando = true;
                try { barra.Valor = (int)numero.Value; }
                finally { _sincronizando = false; }
            };
        }

        private void Aplicar()
        {
            _perfil.Descricao = (_txtDescricao.Text ?? "").Trim();
            _perfil.ZonaMorta = Limitar((int)_numZona.Value, 0, 32767);
            _arquivo.TiqueMs = Limitar((int)_numTique.Value, 1, 200);
            if (_chkInicial.Checked) _arquivo.PerfilInicial = _perfil.Nome;

            DialogResult = DialogResult.OK;
            Close();
        }

        private static int Limitar(int v, int minimo, int maximo)
        {
            if (v < minimo) return minimo;
            if (v > maximo) return maximo;
            return v;
        }
    }
}
