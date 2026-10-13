using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Settings;

/// <summary>A Settings section's footer (Swift's <c>SettingsFooter</c>): 12-point text in the look's soft ink, wrapping.</summary>
public sealed partial class SettingsFooter : UserControl
{
    private readonly TextBlock text = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };

    public SettingsFooter()
    {
        IsTabStop = false;
        Content = text;
        LookBinding.Keep(this, () => text.Foreground = LookBinding.Brush(this, p => p.InkSoft));
    }

    public string Text
    {
        get => text.Text;
        set => text.Text = value;
    }
}
