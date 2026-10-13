using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// The bottom band of a window with buttons (Swift's <c>ActionBar</c>): a hairline in the look's line color over its
/// content, inset like the window's. A grid, so what goes in fills the band: a button alone sits at the start, a
/// horizontal <c>StackPanel</c> of buttons (12 apart, <see cref="Space.Item"/>) is the Mac's row, and a <c>Grid</c> with a
/// star column pushes buttons to the end.
/// </summary>
public sealed partial class ActionBar : Grid
{
    public ActionBar()
    {
        Padding = new Thickness(WindowInset.Side, Space.Group, WindowInset.Side, Space.Group);
        BorderThickness = new Thickness(0, 1, 0, 0);
        LookBinding.Keep(this, () => BorderBrush = LookBinding.Brush(this, p => p.Line));
    }
}
