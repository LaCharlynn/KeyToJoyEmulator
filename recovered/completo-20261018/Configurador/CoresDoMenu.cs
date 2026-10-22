using System.Drawing;
using System.Windows.Forms;

namespace Configurador;

internal sealed class CoresDoMenu : ProfessionalColorTable
{
	public override Color MenuItemSelected => Tema.Painel3;

	public override Color MenuItemBorder => Tema.Realce;

	public override Color MenuBorder => Tema.Borda;

	public override Color MenuItemSelectedGradientBegin => Tema.Painel3;

	public override Color MenuItemSelectedGradientEnd => Tema.Painel3;

	public override Color MenuItemPressedGradientBegin => Tema.Painel2;

	public override Color MenuItemPressedGradientEnd => Tema.Painel2;

	public override Color ToolStripDropDownBackground => Tema.Painel2;

	public override Color ImageMarginGradientBegin => Tema.Painel2;

	public override Color ImageMarginGradientMiddle => Tema.Painel2;

	public override Color ImageMarginGradientEnd => Tema.Painel2;

	public override Color SeparatorDark => Tema.Borda;

	public override Color SeparatorLight => Tema.Borda;
}
