using Deskling.Core.Scheduling;
using Deskling.Windows.Shell;

namespace Deskling.Windows.System;

/// <summary>
/// Seconds since the last keyboard or mouse input in this session, from <c>GetLastInputInfo</c>. Reads a tick
/// count the OS already keeps: no input hook, no permission. Reports 0 when the call fails.
/// </summary>
public sealed class SystemIdleTimeProvider : IIdleTimeProvider
{
    public double SecondsSinceLastInput()
    {
        var info = new NativeMethods.LASTINPUTINFO { cbSize = 8 };
        if (!NativeMethods.GetLastInputInfo(ref info))
            return 0;
        // Both are 32-bit tick counts, so the difference survives the 49-day wrap.
        var idle = unchecked((uint)Environment.TickCount - info.dwTime);
        return idle / 1000.0;
    }
}
