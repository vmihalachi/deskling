using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Deskling.Windows.Shell;

/// <summary>
/// Finds text that doesn't fit its box in a WinUI window, the way a screenshot mode can check each window it renders
/// instead of someone spotting it in a shot (longer translations are where it happens: French has the longest
/// strings, German the longest words). Every visible <see cref="TextBlock"/> is checked: cut off with an ellipsis,
/// one line wider than its room, a word broken across lines (a label squeezed into a one-letter column), or wrapped
/// lines cut off at the bottom. Lines are measured on the real TextBlock, with a set width or minimum width lifted
/// for the measure (a label's minimum width isn't text); words go through a fresh TextBlock with the same font,
/// since a probe would round each line to whole DIPs and wrap it at its own width. Text cut off by a parent rather
/// than its own box gets past it, and a TextBlock with mixed formatting (more than one inline) is skipped. The Mac's
/// <c>LayoutCheck</c> checks a window's height instead (SwiftUI doesn't expose its text layout). Reads the visual
/// tree through <see cref="XamlCast"/>, so it works in Native AOT builds. Needs the UI thread of the window, after
/// layout; run it after taking a screenshot, not before, since it lays the tree out again when it's done.
/// </summary>
public static class LayoutCheck
{
    /// <summary>DIPs of layout rounding allowed (to physical pixels in the window, whole DIPs in a probe).</summary>
    public const double Slack = 1;

    /// <summary>
    /// One line per TextBlock under <paramref name="root"/> (a window's <c>Content</c>) whose text doesn't fit, quoting
    /// the text and saying how; empty when everything fits.
    /// </summary>
    public static IReadOnlyList<string> TextProblems(DependencyObject root)
    {
        var problems = new List<string>();
        Walk(root, problems);
        // The measures above left the boxes measured for the check; put them back the way the window lays them out.
        XamlCast.As<UIElement>(root)?.UpdateLayout();
        return problems;
    }

    private static void Walk(DependencyObject node, List<string> problems)
    {
        if (XamlCast.As<UIElement>(node) is { } element
            && (element.Visibility != Visibility.Visible || element.Opacity == 0))
            return;
        if (XamlCast.As<TextBlock>(node) is { } text && Check(text) is { } problem && !problems.Contains(problem))
            problems.Add(problem);
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            Walk(VisualTreeHelper.GetChild(node, i), problems);
    }

    private static string? Check(TextBlock text)
    {
        var content = text.Text;
        if (string.IsNullOrWhiteSpace(content) || text.ActualWidth <= 0 || text.ActualHeight <= 0)
            return null;
        var quoted = content.ReplaceLineEndings(" ");
        if (quoted.Length > 60)
            quoted = quoted[..59] + "…";
        if (text.IsTextTrimmed)
            return $"\"{quoted}\" is cut off with an ellipsis";
        // Mixed formatting (a bold run in a sentence): a plain probe would measure it differently.
        if (text.Inlines.Count > 1)
            return null;

        // A box arranged smaller than it asked for keeps its size and is clipped to its slot, so take the smaller.
        var slot = LayoutInformation.GetLayoutSlot(text);
        var width = text.ActualWidth;
        var height = text.ActualHeight;
        if (slot.Width > 0)
            width = Math.Min(width, slot.Width - text.Margin.Left - text.Margin.Right);
        if (slot.Height > 0)
            height = Math.Min(height, slot.Height - text.Margin.Top - text.Margin.Bottom);

        if (text.TextWrapping == TextWrapping.NoWrap)
        {
            var needed = MeasureBox(text, double.PositiveInfinity).Width;
            return needed > width + Slack ? $"\"{quoted}\" is cut off on the right ({needed:0} px wide in {width:0})" : null;
        }
        // Words go through a probe. Letter spacing set on a button reads back on its label's TextBlock but isn't
        // applied, so the probe takes the spacing that gives the box's own one-line width.
        var spacing = text.CharacterSpacing;
        if (spacing != 0 && Math.Abs(Measure(text, content, 0).Width - MeasureBox(text, double.PositiveInfinity).Width) <= Slack)
            spacing = 0;
        foreach (var piece in Unbreakable(content))
        {
            var needed = Measure(text, piece, spacing).Width;
            if (needed > width + Slack)
                return $"\"{quoted}\" breaks \"{piece}\" across lines ({needed:0} px wide in {width:0})";
        }
        var tall = MeasureBox(text, width + Slack).Height;
        return tall > height + Slack ? $"\"{quoted}\" is cut off at the bottom ({tall:0} px tall in {height:0})" : null;
    }

    /// <summary>
    /// What a line can't break inside: the words, also split after a hyphen, dash or slash. Chinese and Japanese break
    /// between any two characters, so text in them has nothing unbreakable to check.
    /// </summary>
    internal static List<string> Unbreakable(string content)
    {
        var pieces = new List<string>();
        var piece = new StringBuilder();
        void Flush()
        {
            var word = piece.ToString();
            if (word.Length > 0 && !word.Any(BreaksAnywhere))
                pieces.Add(word);
            piece.Clear();
        }
        foreach (var c in content)
        {
            if (char.IsWhiteSpace(c))
            {
                Flush();
                continue;
            }
            piece.Append(c);
            if (c is '-' or '/' or '–' or '—')
                Flush();
        }
        Flush();
        return pieces;
    }

    /// <summary>CJK ideographs, kana and full-width forms, which a line may break before or after.</summary>
    private static bool BreaksAnywhere(char c) =>
        c is >= '　' and <= '鿿' or >= '豈' and <= '﫿' or >= '＀' and <= '￯';

    private static readonly DependencyProperty[] WidthProperties =
        [FrameworkElement.WidthProperty, FrameworkElement.MinWidthProperty, FrameworkElement.MaxWidthProperty];

    /// <summary>
    /// The size the real box's text asks for, margins left out, when it has <paramref name="width"/> DIPs. A set
    /// width or minimum width is lifted for the measure, so it doesn't count as text. Its next layout pass
    /// measures it again with its real room.
    /// </summary>
    private static global::Windows.Foundation.Size MeasureBox(TextBlock text, double width)
    {
        var margin = text.Margin;
        var saved = WidthProperties.Select(text.ReadLocalValue).ToArray();
        text.Width = double.NaN;
        text.MinWidth = 0;
        text.MaxWidth = double.PositiveInfinity;
        text.Measure(new global::Windows.Foundation.Size(width + margin.Left + margin.Right, double.PositiveInfinity));
        var size = text.DesiredSize;
        for (var i = 0; i < WidthProperties.Length; i++)
        {
            if (saved[i] == DependencyProperty.UnsetValue)
                text.ClearValue(WidthProperties[i]);
            else
                text.SetValue(WidthProperties[i], saved[i]);
        }
        text.InvalidateMeasure();
        return new global::Windows.Foundation.Size(Math.Max(0, size.Width - margin.Left - margin.Right),
            Math.Max(0, size.Height - margin.Top - margin.Bottom));
    }

    /// <summary>The one-line size <paramref name="content"/> takes in a fresh TextBlock set like <paramref name="like"/>.</summary>
    private static global::Windows.Foundation.Size Measure(TextBlock like, string content, int characterSpacing)
    {
        var probe = new TextBlock
        {
            Text = content,
            FontFamily = like.FontFamily,
            FontSize = like.FontSize,
            FontWeight = like.FontWeight,
            FontStyle = like.FontStyle,
            FontStretch = like.FontStretch,
            CharacterSpacing = characterSpacing,
            OpticalMarginAlignment = like.OpticalMarginAlignment,
            IsTextScaleFactorEnabled = like.IsTextScaleFactorEnabled,
            Padding = like.Padding,
            TextWrapping = TextWrapping.NoWrap,
        };
        probe.Measure(new global::Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return probe.DesiredSize;
    }
}
