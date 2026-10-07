using Deskling.Windows.Shell;

namespace Deskling.Windows.System;

/// <summary>
/// Whether the session is locked (Win+L, the lock screen after a timeout), from <c>WTSRegisterSessionNotification</c>
/// on a message-only window. Create it on the UI thread: <see cref="Changed"/> is raised there with the new state.
/// Windows doesn't say whether the session is locked right now, so it starts as unlocked and follows the changes
/// from then on.
/// </summary>
public sealed class SessionLockWatcher : IDisposable
{
    private readonly MessageWindow window;
    private readonly bool registered;

    /// <param name="windowClassName">The hidden window's class name, unique within the process.</param>
    public SessionLockWatcher(string windowClassName = "Deskling.SessionLock")
    {
        window = new MessageWindow(windowClassName, messageOnly: true, OnMessage);
        registered =
            window.Handle != 0
            && NativeMethods.WTSRegisterSessionNotification(window.Handle, NativeMethods.NOTIFY_FOR_THIS_SESSION);
    }

    /// <summary>The session was locked (true) or unlocked (false).</summary>
    public event Action<bool>? Changed;

    public bool IsLocked { get; private set; }

    public void Dispose()
    {
        if (registered)
            NativeMethods.WTSUnRegisterSessionNotification(window.Handle);
        window.Dispose();
    }

    private nint? OnMessage(uint message, nint wParam, nint lParam)
    {
        if (message != NativeMethods.WM_WTSSESSION_CHANGE)
            return null;
        if (wParam == NativeMethods.WTS_SESSION_LOCK)
            Set(true);
        else if (wParam == NativeMethods.WTS_SESSION_UNLOCK)
            Set(false);
        return 0;
    }

    private void Set(bool locked)
    {
        if (IsLocked == locked)
            return;
        IsLocked = locked;
        Changed?.Invoke(locked);
    }
}
