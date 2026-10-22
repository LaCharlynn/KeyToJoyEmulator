// DialogoTexto.cs - pergunta uma linha de texto (usado para renomear perfil).
// Escrito aqui em vez de usar Microsoft.VisualBasic.Interaction.InputBox para nao depender de nada.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Configurador
{
    public static class DialogoTexto
    {
        /// <summary>Devolve o texto digitado, ou null se o usuario cancelar.</summary>
        public static string Perguntar(IWin32Window dono, string titulo, string pergunta, string valorInicial)
        {
            using (Form janela = new Form())
            {
                janela.Text = titulo;
                janela.FormBorderStyle = FormBorderStyle.FixedDialog;
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.MinimizeBox = false;
                janela.MaximizeBox = false;
                janela.ShowInTaskbar = false;
                janela.ClientSize = new Size(420, 130);
                Tema.IconeDaJanela(janela);   // a mesma logo das outras janelas

                Label rotulo = new Label();
                rotulo.Text = pergunta;
                rotulo.SetBounds(12, 12, 396, 22);

                TextBox caixa = new TextBox();
                caixa.Text = valorInicial ?? "";
                caixa.SetBounds(12, 38, 396, 24);
                caixa.SelectAll();

                Button ok = new Button();
                ok.Text = "OK";
                ok.SetBounds(232, 78, 88, 30);
                ok.DialogResult = DialogResult.OK;

                Button cancelar = new Button();
                cancelar.Text = "Cancelar";
                cancelar.SetBounds(324, 78, 84, 30);
                cancelar.DialogResult = DialogResult.Cancel;

                janela.Controls.Add(rotulo);
                janela.Controls.Add(caixa);
                janela.Controls.Add(ok);
                janela.Controls.Add(cancelar);
                janela.AcceptButton = ok;
                janela.CancelButton = cancelar;

                if (janela.ShowDialog(dono) != DialogResult.OK) return null;
                return caixa.Text;
            }
        }
    }
}
