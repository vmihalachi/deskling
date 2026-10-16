using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// Polite UI Automation notifications, for words that appear on their own (a line, a thanks, a note): Narrator reads
/// them without moving focus. Each kind of announcement has one fixed activity id and only its most recent is read, so
/// several views showing the same words, or words replaced before Narrator got to them, don't stack. The words are
/// always the app's; this adds none.
/// </summary>
internal static class Announcer
{
    /// <summary>The bubble's words.</summary>
    public const string Line = "deskling.line";

    /// <summary>The status pill's new kind of status.</summary>
    public const string Status = "deskling.status";

    /// <summary>The Support window's thanks.</summary>
    public const string Thanks = "deskling.thanks";

    /// <summary>A note under a setting (the restart note, the notification note, the startup line).</summary>
    public const string Note = "deskling.note";

    /// <summary>
    /// Raises <paramref name="words"/> as a polite notification on <paramref name="element"/>'s automation peer, when
    /// it is loaded and the words aren't empty; otherwise nothing. The element needs a peer of its own (a text or a
    /// button, not a panel).
    /// </summary>
    public static void Say(FrameworkElement element, string? words, string activityId)
    {
        if (string.IsNullOrEmpty(words) || !element.IsLoaded)
            return;
        var peer = FrameworkElementAutomationPeer.FromElement(element) ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
        peer?.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.MostRecent, words, activityId);
    }

    /// <summary>True when something hidden becomes visible: the moment a note announces itself.</summary>
    public static bool Appears(Visibility before, Visibility after) => before == Visibility.Collapsed && after == Visibility.Visible;
}
