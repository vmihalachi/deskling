using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// A row in the tray panel (Swift's <c>PanelRowStyle</c>): a full-width button of plain 13-point text in ink, no frame,
/// that fills with the look's <c>accentSoft</c> under the pointer and while pressed. A <see cref="Button"/> restyled
/// through its own light and dark theme dictionaries (WinUI's lightweight styling), so the states follow the theme with
/// no template; under a contrast theme it keeps Windows' own button colors. The look is read when the row is made.
/// </summary>
public sealed partial class PanelRow : Button
{
    public PanelRow()
    {
        FontSize = 13;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Left;
        Padding = new Thickness(Space.Snug, Space.Tight + 1, Space.Snug, Space.Tight + 1);
        CornerRadius = new CornerRadius(6);
        BorderThickness = new Thickness(0);
        var palette = CompanionLook.Current.Palette;
        Resources.ThemeDictionaries["Light"] = States(palette, ElementTheme.Light);
        Resources.ThemeDictionaries["Dark"] = States(palette, ElementTheme.Dark);
        Resources.ThemeDictionaries["Default"] = States(palette, ElementTheme.Dark);
    }

    private static ResourceDictionary States(CompanionPalette palette, ElementTheme theme)
    {
        var clear = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        var soft = new SolidColorBrush(palette.AccentSoft.For(theme));
        var ink = new SolidColorBrush(palette.Ink.For(theme));
        var states = new ResourceDictionary();
        foreach (var state in new[] { "", "PointerOver", "Pressed", "Disabled" })
        {
            states["ButtonBackground" + state] = state is "PointerOver" or "Pressed" ? soft : clear;
            states["ButtonBorderBrush" + state] = clear;
            states["ButtonForeground" + state] = ink;
        }
        return states;
    }
}
