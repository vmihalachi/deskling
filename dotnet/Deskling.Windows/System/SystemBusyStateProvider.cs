using System.Security;
using Deskling.Core.Scheduling;
using Deskling.Windows.Shell;
using Microsoft.Win32;

namespace Deskling.Windows.System;

/// <summary>
/// Detects calls (another app using the camera or the microphone) and full-screen apps. Only reads the privacy
/// settings' "recent activity" records (the capability access consent store) and the shell's notification state:
/// it never opens a device, so it can't trigger a permission prompt and needs no capability in the manifest.
/// Unreadable records read as not busy.
/// </summary>
public sealed class SystemBusyStateProvider : IBusyStateProvider
{
    private const string ConsentStore =
        @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

    public IReadOnlySet<BusyReason> CurrentBusyReasons()
    {
        var reasons = new HashSet<BusyReason>();
        if (IsInUse("webcam") || IsInUse("microphone"))
            reasons.Add(BusyReason.Call);
        if (IsFullScreen())
            reasons.Add(BusyReason.FullScreen);
        return reasons;
    }

    /// <summary>A full-screen app, game or presentation (Windows holds its own banners then too).</summary>
    private static bool IsFullScreen() =>
        NativeMethods.SHQueryUserNotificationState(out var state) == 0
        && state
            is NativeMethods.UserNotificationState.Busy
                or NativeMethods.UserNotificationState.RunningDirect3DFullScreen
                or NativeMethods.UserNotificationState.PresentationMode
                or NativeMethods.UserNotificationState.App;

    /// <summary>
    /// Windows records when each app starts and stops using a device; a start without a stop means it's in use
    /// now. Packaged apps are subkeys of the capability, desktop apps are under <c>NonPackaged</c>.
    /// </summary>
    private static bool IsInUse(string capability)
    {
        try
        {
            using var root = Registry.CurrentUser.OpenSubKey($@"{ConsentStore}\{capability}");
            if (root is null)
                return false;
            foreach (var name in root.GetSubKeyNames())
            {
                using var app = root.OpenSubKey(name);
                if (app is null)
                    continue;
                if (name == "NonPackaged")
                {
                    foreach (var exe in app.GetSubKeyNames())
                    {
                        using var entry = app.OpenSubKey(exe);
                        if (entry is not null && IsActive(entry))
                            return true;
                    }
                }
                else if (IsActive(app))
                    return true;
            }
        }
        catch (Exception e) when (e is SecurityException or UnauthorizedAccessException or IOException)
        {
            // Unreadable records just mean "not busy".
        }
        return false;
    }

    private static bool IsActive(RegistryKey entry) =>
        entry.GetValue("LastUsedTimeStart") is long start
        && start > 0
        && entry.GetValue("LastUsedTimeStop") is long stop
        && stop == 0;
}
