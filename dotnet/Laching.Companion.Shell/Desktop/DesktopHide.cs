namespace Laching.Companion.Shell.Desktop;

/// <summary>How long a hide for a while lasts, from the desktop character's menu or the tray's.</summary>
public enum DesktopHide
{
    Hour,
    RestOfToday,

    /// <summary>Turns the desktop character off: the setting, until the user turns it back on.</summary>
    UntilShown,
}
