// JanelaAtalhos.cs - as combinacoes de teclas que trocam de perfil durante o jogo.
//
// Cada atalho e "uma combinacao de teclas -> nome de um perfil que existe no arquivo". Quem aperta a
// combinacao no jogo faz o pad trocar para aquele perfil. A combinacao e montada com o MESMO gancho de
// teclado que o programa usa (CapturaDeTecla), entao o que se ve aqui e o que o pad vai comparar.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyVersion;

namespace Configurador
{
    internal sealed class JanelaAtalhos : Form
    {
        private readonly ArquivoPerfis _arquivo;
        private readonly Perfil _perfil;
        private readonly List<Atalho> _atalhos = new List<Atalho>();

        private readonly ListBox _lstAtalhos = Tema.Lista(26);
        private readonly ListBox _lstPerfis = Tema.Lista(22);
        private readonly Label _lblCombinacao = new Label();
        private readonly Button _btnCapturar = Tema.Botao("Capturar combinacao...", true);
        private readonly Button _btnAdicionar = Tema.Botao("Adicionar", false);
        private readonly Button _btnRemover = Tema.Botao("Remover o atalho escolhido", false);

        private string _combinacao;

        public static bool Perguntar(IWin32Window dono, ArquivoPerfis arquivo, Perfil perfil)
        {
            using (JanelaAtalhos j = new JanelaAtalhos(arquivo, perfil))
                return j.ShowDialog(dono) == DialogResult.OK;
        }

        private JanelaAtalhos(ArquivoPerfis arquivo, Perfil perfil)
        {
            _arquivo = arquivo;
            _perfil = perfil;

            Text = "Atalhos do perfil '" + perfil.Nome + "'";
            BackColor = Tema.Fundo;
            ForeColor = Tema.Texto;
            Font = Tema.Fonte;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(600, 520);
            MinimumSize = new Size(560, 480);

            foreach (Atalho a in perfil.Atalhos) _atalhos.Add(a);

            Construir();
            RecarregarListaDaTela(false);
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
            t.ColumnCount = 1;
            t.RowCount = 8;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));   // titulo
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // lista
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));   // remover
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));   // linha
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));   // combinacao capturada
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));   // capturar
            // A caixinha de escolher o perfil tem de caber os perfis (ate 5) para NAO precisar de barra
            // de rolagem: a barra do ListBox e pintada pelo Windows e, no tema escuro, ficou BRANCA
            // (defeito visto na foto de 2026-10-18); pedir a versao escura a deixa CINZA, mas ela
            // continua aparecendo. A altura certa nao sai de conta de dedo: e a que o proprio ListBox
            // pede (PreferredHeight), calculada LA EMBAIXO, depois de encher a lista. Medido em
            // 2026-10-18, com 4 perfis: a caixa ficou com 84 px para 88 px de itens e apareceu uma
            // barra de 2 px em rgb(149,149,149); a conta antiga (6 + 22 * n) errava por 6 px porque o
            // painel interno levava as margens padrao do TableLayoutPanel (3 em cima + 3 embaixo).
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 94f));   // destino + adicionar (corrigido ao encher a lista)
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // aplicar/cancelar

            Label titulo = Tema.Rotulo("Atalhos do perfil '" + _perfil.Nome + "'  (a combinacao troca o perfil do pad na hora)", true);
            titulo.Font = Tema.Fonte8;
            t.Controls.Add(titulo, 0, 0);

            t.Controls.Add(_lstAtalhos, 0, 1);
            _lstAtalhos.DrawItem += DesenharAtalho;

            _btnRemover.Margin = new Padding(0, 3, 0, 3);
            _btnRemover.Click += delegate { Remover(); };
            t.Controls.Add(_btnRemover, 0, 2);

            Panel linha = new Panel();
            linha.Dock = DockStyle.Fill;
            linha.BackColor = Tema.Borda;
            linha.Margin = new Padding(0, 4, 0, 4);
            t.Controls.Add(linha, 0, 3);

            _lblCombinacao.Dock = DockStyle.Fill;
            _lblCombinacao.Font = Tema.Fonte8N;
            _lblCombinacao.ForeColor = Tema.Texto2;
            _lblCombinacao.TextAlign = ContentAlignment.MiddleLeft;
            _lblCombinacao.Text = "Combinacao: (aperte o botao abaixo e depois a combinacao)";
            t.Controls.Add(_lblCombinacao, 0, 4);

            _btnCapturar.Margin = new Padding(0, 3, 0, 3);
            _btnCapturar.Click += delegate { Capturar(); };
            t.Controls.Add(_btnCapturar, 0, 5);

            TableLayoutPanel destino = new TableLayoutPanel();
            destino.Dock = DockStyle.Fill;
            destino.BackColor = Tema.Fundo;
            destino.Margin = new Padding(0);   // sem isto o painel come 6 px de altura e a caixa fica curta
            destino.ColumnCount = 3;
            destino.RowCount = 1;
            destino.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            destino.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            destino.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            destino.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label rotulo = Tema.Rotulo("Trocar para o perfil:", false);
            rotulo.Font = Tema.Fonte8;
            destino.Controls.Add(rotulo, 0, 0);

            _lstPerfis.Margin = new Padding(0, 2, 8, 2);
            _lstPerfis.DrawItem += DesenharPerfil;
            foreach (KeyValuePair<string, Perfil> par in _arquivo.Perfis) _lstPerfis.Items.Add(par.Key);
            if (_lstPerfis.Items.Count > 0) _lstPerfis.SelectedIndex = 0;
            destino.Controls.Add(_lstPerfis, 1, 0);

            // Agora que a lista esta cheia, o PROPRIO ListBox diz de quanto precisa. Acima de 5 perfis a
            // caixinha nao cresce: ai a barra de rolagem e util e fica de proposito.
            int visiveis = _lstPerfis.Items.Count > 5 ? 5 : _lstPerfis.Items.Count;
            int precisa = _lstPerfis.PreferredHeight -
                          Math.Max(0, _lstPerfis.Items.Count - visiveis) * _lstPerfis.ItemHeight;
            t.RowStyles[6].Height = Math.Max(34f, precisa + _lstPerfis.Margin.Vertical + 4f);

            _btnAdicionar.Margin = new Padding(0, 2, 0, 2);
            _btnAdicionar.Click += delegate { Adicionar(); };
            destino.Controls.Add(_btnAdicionar, 2, 0);
            t.Controls.Add(destino, 0, 6);

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
            aplicar.Margin = new Padding(0, 6, 6, 0);
            cancelar.Margin = new Padding(0, 6, 0, 0);
            aplicar.Click += delegate { Aplicar(); };
            cancelar.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            botoes.Controls.Add(new Panel { BackColor = Tema.Fundo, Dock = DockStyle.Fill }, 0, 0);
            botoes.Controls.Add(aplicar, 1, 0);
            botoes.Controls.Add(cancelar, 2, 0);
            t.Controls.Add(botoes, 0, 7);

            Controls.Add(t);
        }

        private void RecarregarListaDaTela(bool manterEscolha)
        {
            int escolhido = _lstAtalhos.SelectedIndex;
            _lstAtalhos.Items.Clear();
            foreach (Atalho a in _atalhos)
                _lstAtalhos.Items.Add(a.Combinacao + "   ->   " + a.PerfilDestino);
            if (_lstAtalhos.Items.Count > 0)
                _lstAtalhos.SelectedIndex = manterEscolha && escolhido >= 0 && escolhido < _lstAtalhos.Items.Count ? escolhido : 0;
        }

        private void DesenharAtalho(object remetente, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstAtalhos.Items.Count) return;
            bool escolhido = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush fundo = new SolidBrush(escolhido ? Tema.RealceEscuro : Tema.Painel2))
                e.Graphics.FillRectangle(fundo, e.Bounds);
            using (SolidBrush tinta = new SolidBrush(Tema.Texto))
            using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center })
                e.Graphics.DrawString((string)_lstAtalhos.Items[e.Index], Tema.Fonte8N, tinta,
                                      new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height), sf);
        }

        private void DesenharPerfil(object remetente, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _lstPerfis.Items.Count) return;
            bool escolhido = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush fundo = new SolidBrush(escolhido ? Tema.RealceEscuro : Tema.Painel2))
                e.Graphics.FillRectangle(fundo, e.Bounds);
            using (SolidBrush tinta = new SolidBrush(Tema.Texto))
            using (StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center })
                e.Graphics.DrawString((string)_lstPerfis.Items[e.Index], Tema.Fonte8, tinta,
                                      new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, e.Bounds.Width - 10, e.Bounds.Height), sf);
        }

        /// <summary>Pede a combinacao no MESMO gancho de teclado do programa e guarda o nome canonico.</summary>
        private void Capturar()
        {
            string bruta;
            using (CapturaDeTecla janela = new CapturaDeTecla(true))
            {
                if (janela.ShowDialog(this) != DialogResult.OK) return;
                bruta = janela.CombinacaoCapturada;
            }
            if (string.IsNullOrWhiteSpace(bruta)) return;

            List<string> nomes = new List<string>();
            foreach (string parte in bruta.Split('+'))
            {
                string pedaco = parte.Trim();
                if (pedaco.Length == 0) continue;
                CodigoTecla codigo;
                if (!TabelaTeclas.TentarResolver(pedaco, out codigo))
                {
                    _lblCombinacao.Text = "Nao entendi a tecla '" + pedaco + "'. Tente de novo.";
                    _combinacao = null;
                    return;
                }
                string canonico = TabelaTeclas.NomeDe(codigo);
                if (!nomes.Contains(canonico)) nomes.Add(canonico);
            }
            if (nomes.Count == 0)
            {
                _lblCombinacao.Text = "A combinacao ficou vazia. Tente de novo.";
                _combinacao = null;
                return;
            }

            _combinacao = string.Join("+", nomes.ToArray());
            _lblCombinacao.Text = "Combinacao: " + _combinacao + "   (escolha o perfil e clique em Adicionar)";
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

            string destino = (string)_lstPerfis.Items[_lstPerfis.SelectedIndex];

            // a mesma combinacao nao pode apontar para dois perfis diferentes: tira a antiga
            for (int i = _atalhos.Count - 1; i >= 0; i--)
                if (string.Equals(_atalhos[i].Combinacao, _combinacao, StringComparison.OrdinalIgnoreCase))
                    _atalhos.RemoveAt(i);

            Atalho at = new Atalho { Combinacao = _combinacao, PerfilDestino = destino };
            foreach (string parte in _combinacao.Split('+'))
            {
                CodigoTecla codigo;
                if (TabelaTeclas.TentarResolver(parte.Trim(), out codigo) && !at.Teclas.Contains(codigo)) at.Teclas.Add(codigo);
            }
            _atalhos.Add(at);

            _lblCombinacao.Text = "Atalho " + _combinacao + " -> " + destino + " acrescentado.";
            _combinacao = null;
            RecarregarListaDaTela(false);
        }

        private void Remover()
        {
            int i = _lstAtalhos.SelectedIndex;
            if (i < 0 || i >= _atalhos.Count) return;
            string texto = _atalhos[i].Combinacao;
            _atalhos.RemoveAt(i);
            RecarregarListaDaTela(false);
            _lblCombinacao.Text = "Atalho " + texto + " removido.";
        }

        private void Aplicar()
        {
            _perfil.Atalhos = _atalhos;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
