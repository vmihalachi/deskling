using Laching.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// The root of every window <see cref="WindowManager{TWindow}"/> opens (and of the screenshot windows built the same
/// way): the look's paper behind everything, as the Mac's window background, a <see cref="WindowInset.TitleBar"/> band
/// at the top that holds nothing but the caption buttons and WinUI's default drag region, and the app's view under it.
/// The band sits outside the view, so a view that scrolls clips its content below the band instead of sliding it under
/// the caption buttons, and the view's <see cref="WindowInset"/> starts under the band. The band takes the window's
/// paper in every theme and contrast theme, since it is the root's own background. UI thread only.
/// </summary>
internal static class WindowFrame
{
    public static Grid Make(UIElement view)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WindowInset.TitleBar) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        LookBinding.Keep(root, () => root.Background = LookBinding.Brush(root, p => p.Paper));
        view.SetValue(Grid.RowProperty, 1);
        root.Children.Add(view);
        return root;
    }
}
