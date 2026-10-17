namespace Laching.Companion.Shell.Signals;

/// <summary>Whether the sound output is muted (Swift's <c>AudioOutputStateProvider</c>); null when it can't be read.</summary>
public interface IAudioOutputState
{
    bool? IsOutputMuted();
}

/// <summary>Whether the session is locked (Swift's <c>ScreenLockWatcher</c>).</summary>
public interface IScreenLock
{
    bool IsLocked { get; }
}

/// <summary>The power source at one moment.</summary>
public readonly record struct PowerState(bool IsOnBattery, int? Percent);

/// <summary>The power source and its changes (Swift's <c>SystemPowerSource</c>); null state on a desktop without a battery reading.</summary>
public interface IPowerSource
{
    PowerState? CurrentPowerState();

    event Action? Changed;
}

/// <summary>The system's light or dark appearance and its changes (Swift's <c>AppearanceWatcher</c>).</summary>
public interface IAppearance
{
    bool IsDark { get; }

    /// <summary>The appearance changed: true when it is dark now.</summary>
    event Action<bool>? Changed;
}
