using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// The bottom band of a window with buttons (Swift's <c>ActionBar</c>): a hairline in the look's line color over a row
/// of its children, 12 apart, inset like the window's content. Put a <c>Grid</c> in it to push buttons to the end.
/// </summary>
public sealed partial class ActionBar : StackPanel
{
    public ActionBar()
    {
        Orientation = Orientation.Horizontal;
        Spacing = Space.Item;
        Padding = new Thickness(WindowInset.Side, Space.Group, WindowInset.Side, Space.Group);
        BorderThickness = new Thickness(0, 1, 0, 0);
        LookBinding.Keep(this, () => BorderBrush = LookBinding.Brush(this, p => p.Line));
    }
}
