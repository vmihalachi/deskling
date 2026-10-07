using Microsoft.UI.Xaml.Markup;

namespace Deskling.Windows.Shell.Localization;

/// <summary>
/// XAML access to <see cref="Loc"/>: <c>Text="{l:Loc Key='Start now'}"</c>. <c>Upper=True</c> uppercases it, for
/// labels and headings. Markup extensions can't escape a quote, so write an apostrophe in a key as a backtick:
/// <c>Key='If you`ve stepped away…'</c>.
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed partial class LocExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public bool Upper { get; set; }

    protected override object ProvideValue()
    {
        var text = Loc.Get(Key.Replace('`', '\''));
        return Upper ? Loc.Upper(text) : text;
    }
}
