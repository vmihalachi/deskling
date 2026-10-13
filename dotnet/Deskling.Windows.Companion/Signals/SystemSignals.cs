using Deskling.Companion.Shell.Signals;
using Deskling.Windows.System;

namespace Deskling.Windows.Companion.Signals;

/// <summary>The output device's mute flag (<see cref="IAudioOutputState"/>) over <see cref="OutputMute"/>; null when it can't be read.</summary>
public sealed class SystemAudioOutput : IAudioOutputState
{
    private readonly OutputMute mute = new();

    public bool? IsOutputMuted() => mute.TryRead(out var muted) ? muted : null;
}

/// <summary>
/// Whether the session is locked (<see cref="IScreenLock"/>) over <see cref="SessionLockWatcher"/>. Create it on the UI
/// thread. It starts as unlocked, as the app can't be running at sign-in with the session locked.
/// </summary>
public sealed class SystemScreenLock : IScreenLock, IDisposable
{
    private readonly SessionLockWatcher watcher;

    /// <param name="windowClassName">The hidden window's class name, unique within the process.</param>
    public SystemScreenLock(string windowClassName = "Deskling.Companion.SessionLock")
    {
        watcher = new SessionLockWatcher(windowClassName);
    }

    public bool IsLocked => watcher.IsLocked;

    public void Dispose() => watcher.Dispose();
}

/// <summary>
/// The battery (<see cref="IPowerSource"/>) over <see cref="PowerSource"/>: no state on a PC without a battery.
/// <see cref="Changed"/> comes back on the UI thread (the signal hub's).
/// </summary>
public sealed class SystemPowerSource : IPowerSource, IDisposable
{
    private readonly PowerSource source = new();
    private readonly SynchronizationContext ui;

    /// <param name="ui">Where <see cref="Changed"/> is raised; the current thread's context by default.</param>
    public SystemPowerSource(SynchronizationContext? ui = null)
    {
        this.ui = UiContext.Resolve(ui, "power source");
        source.Changed += () => this.ui.Post(_ => Changed?.Invoke(), null);
    }

    public event Action? Changed;

    public PowerState? CurrentPowerState() =>
        source.HasBattery ? new PowerState(source.IsOnBattery, source.ChargePercent) : null;

    public void Dispose() => source.Dispose();
}

/// <summary>
/// The apps' light or dark mode (<see cref="IAppearance"/>) over <see cref="AppearanceWatcher"/>. Windows also reports
/// accent color changes, so <see cref="Changed"/> is raised only when light and dark really swap, on the UI thread.
/// </summary>
public sealed class SystemAppearance : IAppearance, IDisposable
{
    private readonly AppearanceWatcher watcher = new();
    private readonly SynchronizationContext ui;
    private bool isDark;

    /// <param name="ui">Where <see cref="Changed"/> is raised; the current thread's context by default.</param>
    public SystemAppearance(SynchronizationContext? ui = null)
    {
        this.ui = UiContext.Resolve(ui, "appearance");
        isDark = watcher.IsDark;
        watcher.Changed += () => this.ui.Post(_ => Update(watcher.IsDark), null);
    }

    public bool IsDark => isDark;

    public event Action<bool>? Changed;

    public void Dispose() => watcher.Dispose();

    /// <summary>Takes a fresh reading; raises <see cref="Changed"/> when it differs from the last.</summary>
    internal void Update(bool dark)
    {
        if (dark == isDark)
            return;
        isDark = dark;
        Changed?.Invoke(dark);
    }
}

internal static class UiContext
{
    public static SynchronizationContext Resolve(SynchronizationContext? ui, string what) =>
        ui
        ?? SynchronizationContext.Current
        ?? throw new InvalidOperationException($"Create the {what} on the UI thread, or pass its SynchronizationContext.");
}
