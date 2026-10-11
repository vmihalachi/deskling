using Deskling.Companion.Rules;

namespace Deskling.Companion.Desktop;

/// <summary>
/// Whether the character stands on the desktop right now, and how long its speech bubble stays there.
/// The character steps aside while another app uses the camera or microphone or an app is full screen, and comes back
/// <see cref="ReappearDelaySeconds"/> after the last busy moment, so a microphone that blinks on and off doesn't make
/// it flicker. "Hide for a while" lives here too, in memory only: a relaunch brings the character back. Pure: the shell
/// passes the moment in. Pinned by conformance/companion/desktop.json.
/// </summary>
public sealed class DesktopPresence
{
    /// <summary>How long after the last call or full-screen moment the character comes back.</summary>
    public const double ReappearDelaySeconds = 10;

    /// <summary>"Hide for an hour".</summary>
    public const double HourHideSeconds = 3600;

    /// <summary>Hidden for a while, until this moment ("for an hour", "for the rest of today").</summary>
    public DateTimeOffset? HiddenUntil { get; private set; }

    /// <summary>The last moment a call or a full-screen app was seen.</summary>
    public DateTimeOffset? LastBusy { get; private set; }

    public void Hide(DateTimeOffset until) => HiddenUntil = until;

    /// <summary>Ends a hide for a while early.</summary>
    public void ShowNow() => HiddenUntil = null;

    /// <summary>
    /// Whether the character is on the desktop at <paramref name="now"/>, with the desktop setting <paramref name="enabled"/>.
    /// Call it on every tick and busy poll: it remembers the last busy moment and forgets a hide that has run out.
    /// </summary>
    public bool Update(bool enabled, Snapshot snapshot, DateTimeOffset now)
    {
        if (snapshot.Busy.Count > 0)
            LastBusy = now;
        if (HiddenUntil is { } until && now >= until)
            HiddenUntil = null;
        if (!enabled || HiddenUntil is not null)
            return false;
        return LastBusy is not { } busy || (now - busy).TotalSeconds >= ReappearDelaySeconds;
    }

    /// <summary>
    /// How long the desktop bubble stays once the line has been said (or shown, when nothing is spoken): half a second
    /// plus 40 ms a character, between 2 and 5 s. Characters are Unicode scalars (runes), as on the Mac.
    /// </summary>
    public static double BubbleSeconds(string text) => Math.Min(Math.Max(0.5 + 0.04 * text.EnumerateRunes().Count(), 2), 5);
}
