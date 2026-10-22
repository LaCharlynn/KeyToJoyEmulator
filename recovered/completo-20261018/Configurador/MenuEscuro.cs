using System.Windows.Forms;

namespace Configurador;

internal sealed class MenuEscuro : ToolStripProfessionalRenderer
{
	public MenuEscuro()
		: base(new CoresDoMenu())
	{
	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
	{
		e.TextColor = (e.Item.Selected ? Tema.Texto : Tema.Texto2);
		base.OnRenderItemText(e);
	}
}
