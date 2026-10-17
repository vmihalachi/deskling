using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Laching.Windows.Companion.Views;

/// <summary>
/// A short status in capitals on an <c>accentSoft</c> capsule ("Next in about 20 min", "Quiet: you're on a call"),
/// in the look's colors (Swift's <c>StatusPill</c>): 11-point semibold, a little tracked, one line. The text is the
/// app's, already localized; Narrator reads it as written, not in capitals. With <see cref="Announces"/> and a
/// <see cref="Kind"/>, Narrator hears the new status when it changes kind (Shh, a call, back to the countdown), never
/// the minute countdown ticking down.
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
    private string? kind;
    private bool announcing;

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

    /// <summary>The status, as the app words it (the model's <c>StatusLine</c>): shown in capitals, read as written.</summary>
    public string Text
    {
        get => text;
        set
        {
            text = value ?? "";
            label.Text = text.ToUpper(CultureInfo.CurrentCulture);
            AutomationProperties.SetName(label, text);
        }
    }

    /// <summary>
    /// What kind of status <see cref="Text"/> is, as a key the app chooses: the same for every minute of the countdown,
    /// different for Shh, a call, off hours and so on. Only a change of kind is announced; null or empty (the default)
    /// is no kind and never announces. It is never shown or read.
    /// </summary>
    public string? Kind
    {
        get => kind;
        set
        {
            if (ShouldAnnounce(Announces, kind, value))
                AnnounceSoon();
            kind = value;
        }
    }

    /// <summary>
    /// True has Narrator hear the new status (a polite notification, without moving focus) when its <see cref="Kind"/>
    /// changes while the pill is on screen; false (the default) keeps it quiet. Needs <see cref="Kind"/>: without one
    /// the pill can't tell a new status from the countdown, so it says nothing. The words are <see cref="Text"/>.
    /// </summary>
    public bool Announces { get; set; }

    /// <summary>
    /// Whether a pill announces when its kind goes from <paramref name="shown"/> to <paramref name="next"/>: only when it
    /// <paramref name="announces"/> and both are kinds that differ. The first kind it gets is the status it opened on,
    /// not a change, so it stays quiet.
    /// </summary>
    public static bool ShouldAnnounce(bool announces, string? shown, string? next) =>
        announces && !string.IsNullOrEmpty(shown) && !string.IsNullOrEmpty(next) && shown != next;

    /// <summary>Says the text once the bindings have settled: the app may set the kind before or after the text it goes with.</summary>
    private void AnnounceSoon()
    {
        if (announcing || !IsLoaded)
            return;
        announcing = DispatcherQueue.TryEnqueue(() =>
        {
            announcing = false;
            Announcer.Say(label, text, Announcer.Status);
        });
    }

    private void Repaint()
    {
        capsule.Background = LookBinding.Brush(this, p => p.AccentSoft);
        label.Foreground = LookBinding.Brush(this, p => p.AccentText);
    }
}
