using Laching.Companion.Shell.Signals;
using Laching.Companion.Shell.Speech;

namespace Laching.Testing;

/// <summary>A voice that records what it says and finishes when the test says so (<see cref="Finish"/>).</summary>
public sealed class FakeSpeaker : ISpeaking
{
    private readonly List<string> spoken = [];
    private readonly List<string?> clips = [];

    public IReadOnlyList<string> Spoken => spoken;

    /// <summary>The clip passed with each line, in order (null: the system voice).</summary>
    public IReadOnlyList<string?> Clips => clips;

    public int Stops { get; private set; }

    public bool IsSpeaking { get; set; }

    public Action? OnFinished { get; set; }

    /// <summary>Where the playing clip is; null plays like the system voice.</summary>
    public double? ClipTime { get; set; }

    public void Speak(string text, string? clip, string? voiceIdentifier, double rate, double volume)
    {
        spoken.Add(text);
        clips.Add(clip);
        IsSpeaking = true;
    }

    public void Stop()
    {
        Stops++;
        Finish();
    }

    /// <summary>The part ended.</summary>
    public void Finish()
    {
        if (!IsSpeaking)
            return;
        IsSpeaking = false;
        OnFinished?.Invoke();
    }
}

/// <summary>Records the sounds played; each finishes at once.</summary>
public sealed class FakeChime : IChimePlaying
{
    private readonly List<string> played = [];

    public IReadOnlyList<string> Played => played;

    public void Play(string sound, double volume, Action? completion)
    {
        played.Add(sound);
        completion?.Invoke();
    }

    public void Stop() { }
}

/// <summary>The output's mute state, set by the test (null: unknown, as when no device answers).</summary>
public sealed class FakeAudio : IAudioOutputState
{
    public bool? Muted { get; set; }

    public bool? IsOutputMuted() => Muted;
}

/// <summary>Whether the session is locked, set by the test.</summary>
public sealed class FakeScreenLock : IScreenLock
{
    public bool IsLocked { get; set; }
}

/// <summary>The power source, set by the test: <see cref="Set"/> changes it and raises <see cref="Changed"/>.</summary>
public sealed class FakePower : IPowerSource
{
    /// <summary>The reading; null as on a desktop without a battery reading.</summary>
    public PowerState? State { get; set; }

    public event Action? Changed;

    public PowerState? CurrentPowerState() => State;

    public void Set(PowerState state)
    {
        State = state;
        Changed?.Invoke();
    }
}

/// <summary>The system's light or dark appearance, set by the test: <see cref="Set"/> changes it and raises <see cref="Changed"/>.</summary>
public sealed class FakeAppearance : IAppearance
{
    public bool IsDark { get; set; }

    public event Action<bool>? Changed;

    public void Set(bool dark)
    {
        IsDark = dark;
        Changed?.Invoke(dark);
    }
}
