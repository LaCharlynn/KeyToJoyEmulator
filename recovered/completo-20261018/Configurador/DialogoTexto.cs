using System.Drawing;
using System.Windows.Forms;

namespace Configurador;

public static class DialogoTexto
{
	public static string Perguntar(IWin32Window dono, string titulo, string pergunta, string valorInicial)
	{
		using Form form = new Form();
		form.Text = titulo;
		form.FormBorderStyle = FormBorderStyle.FixedDialog;
		form.StartPosition = FormStartPosition.CenterParent;
		form.MinimizeBox = false;
		form.MaximizeBox = false;
		form.ShowInTaskbar = false;
		form.ClientSize = new Size(420, 130);
		Tema.IconeDaJanela(form);
		Label label = new Label();
		label.Text = pergunta;
		label.SetBounds(12, 12, 396, 22);
		TextBox textBox = new TextBox();
		textBox.Text = valorInicial ?? "";
		textBox.SetBounds(12, 38, 396, 24);
		textBox.SelectAll();
		Button button = new Button();
		button.Text = "OK";
		button.SetBounds(232, 78, 88, 30);
		button.DialogResult = DialogResult.OK;
		Button button2 = new Button();
		button2.Text = "Cancelar";
		button2.SetBounds(324, 78, 84, 30);
		button2.DialogResult = DialogResult.Cancel;
		form.Controls.Add(label);
		form.Controls.Add(textBox);
		form.Controls.Add(button);
		form.Controls.Add(button2);
		form.AcceptButton = button;
		form.CancelButton = button2;
		if (form.ShowDialog(dono) != DialogResult.OK)
		{
			return null;
		}
		return textBox.Text;
	}
}
