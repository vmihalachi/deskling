using Deskling.Companion.Rules;
using Deskling.Companion.Shell.Speech;
using Deskling.Core.Scheduling;

namespace Deskling.Companion.Shell.Signals;

/// <summary>
/// The system signals a companion reacts to, in one place: the <see cref="Snapshot"/> the coordinator reads on every
/// tick (busy reasons, idle time, the lock, output mute, speech, notification permission), events the character may
/// comment on (unplugged, charging, battery low, light and dark), and a 1 s busy watch while a line is spoken or the
/// character stands on the desktop, so a call that starts mid-line stops it and the desktop character steps aside.
/// Reads device and window state only; never opens a device or prompts. UI thread only.
/// </summary>
public sealed class SignalHub
{
    private readonly IIdleTimeProvider idle;
    private readonly IBusyStateProvider busy;
    private readonly IAudioOutputState? audio;
    private readonly IScreenLock? screenLock;
    private readonly IPowerSource? power;
    private readonly ITimers timers;
    private PowerState? lastPower;
    private IDisposable? busyWatch;
    private bool watchingSpeech;
    private bool watchingDesktop;
    private IReadOnlySet<BusyReason> lastBusy = new HashSet<BusyReason>();

    public SignalHub(
        IIdleTimeProvider idle,
        IBusyStateProvider busy,
        IAudioOutputState? audio,
        IScreenLock? screenLock,
        IPowerSource? power,
        IAppearance? appearance,
        ITimers timers,
        int lowBatteryPercent = 20)
    {
        LowBatteryPercent = lowBatteryPercent;
        this.idle = idle;
        this.busy = busy;
        this.audio = audio;
        this.screenLock = screenLock;
        this.power = power;
        this.timers = timers;
        lastPower = power?.CurrentPowerState();
        if (power is not null)
            power.Changed += PowerChanged;
        if (appearance is not null)
            appearance.Changed += dark => OnEvent?.Invoke(dark ? Trigger.WentDark : Trigger.WentLight);
    }

    /// <summary>The battery level a "battery low" comment fires below, as it drops past it on battery.</summary>
    public int LowBatteryPercent { get; }

    /// <summary>Whether a line is being spoken right now, beats included (the app's <see cref="SpeechSequencer.IsActive"/>).</summary>
    public Func<bool> IsSpeaking { get; set; } = () => false;

    /// <summary>The last known notification permission, refreshed by the app.</summary>
    public bool NotificationsAuthorized { get; set; }

    /// <summary>Something happened that the character may comment on.</summary>
    public Action<Trigger>? OnEvent { get; set; }

    /// <summary>A call started while a line was being spoken.</summary>
    public Action? OnCallStarted { get; set; }

    /// <summary>
    /// Every poll of the busy watch: the busy reasons, and whether they changed since the last poll (a call or full
    /// screen began or ended).
    /// </summary>
    public Action<IReadOnlySet<BusyReason>, bool>? OnBusyPoll { get; set; }

    public Snapshot Snapshot() =>
        new()
        {
            Busy = busy.CurrentBusyReasons(),
            IdleSeconds = idle.SecondsSinceLastInput(),
            IsScreenLocked = screenLock?.IsLocked ?? false,
            IsOutputMuted = audio?.IsOutputMuted(),
            IsSpeaking = IsSpeaking(),
            NotificationsAuthorized = NotificationsAuthorized,
        };

    public void SpeechStarted()
    {
        watchingSpeech = true;
        UpdateBusyWatch();
    }

    public void SpeechEnded()
    {
        watchingSpeech = false;
        UpdateBusyWatch();
    }

    /// <summary>The character stands on the desktop (or waits to come back after a call): watch for calls and full screen.</summary>
    public void WatchForDesktop(bool on)
    {
        if (on == watchingDesktop)
            return;
        watchingDesktop = on;
        UpdateBusyWatch();
    }

    public bool IsWatchingBusy => busyWatch is not null;

    /// <summary>One poll of the 1 s watch: a call that just started cuts the line, and the busy reasons are reported.</summary>
    public void PollBusy()
    {
        var reasons = busy.CurrentBusyReasons();
        if (watchingSpeech && reasons.Contains(BusyReason.Call))
        {
            SpeechEnded();
            OnCallStarted?.Invoke();
        }
        var changed = !reasons.SetEquals(lastBusy);
        lastBusy = reasons;
        OnBusyPoll?.Invoke(reasons, changed);
    }

    private void UpdateBusyWatch()
    {
        if (watchingSpeech || watchingDesktop)
        {
            if (busyWatch is not null)
                return;
            lastBusy = new HashSet<BusyReason>();
            busyWatch = timers.Every(1, PollBusy);
        }
        else
        {
            busyWatch?.Dispose();
            busyWatch = null;
        }
    }

    private void PowerChanged()
    {
        if (power?.CurrentPowerState() is not { } state)
            return;
        var previous = lastPower;
        lastPower = state;
        if (previous is not { } before)
            return;
        if (state.IsOnBattery != before.IsOnBattery)
            OnEvent?.Invoke(state.IsOnBattery ? Trigger.Unplugged : Trigger.Charging);
        if (state.IsOnBattery && state.Percent is { } percent && before.Percent is { } was && was >= LowBatteryPercent && percent < LowBatteryPercent)
            OnEvent?.Invoke(Trigger.BatteryLow);
    }
}
