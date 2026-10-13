using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// A short status in capitals on an <c>accentSoft</c> capsule ("Next in about 20 min", "Quiet: you're on a call"),
/// in the look's colors (Swift's <c>StatusPill</c>): 11-point semibold, a little tracked, one line. The text is the
/// app's, already localized; Narrator reads it as written, not in capitals.
/// </summary>
public sealed partial class StatusPill : UserControl
{
    private readonly Border capsule = new() { Padding = new Thickness(Space.Item, Space.Tight + 1, Space.Item, Space.Tight + 1) };
    private readonly TextBlock label = new()
    {
        FontSize = 11,
        FontWeight = new() { Weight = 600 },
        // 0.8 points at 11 points, in thousandths of an em.
        CharacterSpacing = 73,
        MaxLines = 1,
        TextTrimming = TextTrimming.CharacterEllipsis,
        TextWrapping = TextWrapping.NoWrap,
    };
    private string text = "";

    public StatusPill()
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        IsTabStop = false;
        capsule.Child = label;
        // A capsule: ends as round as it is tall (a larger radius would make an ellipse).
        capsule.SizeChanged += (_, e) => capsule.CornerRadius = new CornerRadius(e.NewSize.Height / 2);
        Content = capsule;
        LookBinding.Keep(this, Repaint);
    }

    public string Text
    {
        get => text;
        set
        {
            text = value ?? "";
            label.Text = text.ToUpper(CultureInfo.CurrentCulture);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(label, text);
        }
    }

    private void Repaint()
    {
        capsule.Background = LookBinding.Brush(this, p => p.AccentSoft);
        label.Foreground = LookBinding.Brush(this, p => p.AccentText);
    }
}
