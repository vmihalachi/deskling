using Windows.Foundation;

namespace Laching.Windows.Companion.Shell;

/// <summary>Where a new window goes.</summary>
public enum WindowPlacement
{
    /// <summary>The middle of the work area on the display with the pointer.</summary>
    Center,

    /// <summary>The corner by the notification area, where the app's icon lives (the Mac's under-the-menu-bar spot).</summary>
    NearTray,
}

/// <summary>
/// One of the app's windows (Swift's <c>WindowID</c>), which the app returns per id: its title (already localized; the
/// taskbar and Alt+Tab show it), its content size in DIPs, and a few traits.
/// </summary>
/// <param name="IsExclusive">Exclusive windows share one slot: opening one closes whichever other exclusive window is open.</param>
/// <param name="MinSize">The content's own minimum in DIPs, so a resize can't clip it; null lets it shrink.</param>
/// <param name="IsAlwaysOnTop">Above other apps' windows (the Mac's floating level).</param>
public sealed record WindowSpec(
    string Title,
    Size Size,
    WindowPlacement Placement = WindowPlacement.Center,
    bool IsExclusive = false,
    Size? MinSize = null,
    bool IsAlwaysOnTop = false
);
