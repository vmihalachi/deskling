namespace Laching.Companion.Shell.Speech;

/// <summary>Says nothing and finishes at once: screenshot runs and previews without sound.</summary>
public sealed class SilentSpeaker : ISpeaking
{
    public bool IsSpeaking => false;

    public Action? OnFinished { get; set; }

    public void Speak(string text, string? clip, string? voiceIdentifier, double rate, double volume) => OnFinished?.Invoke();

    public void Stop() { }
}

/// <summary>Plays nothing and completes at once.</summary>
public sealed class SilentChime : IChimePlaying
{
    public void Play(string sound, double volume, Action? completion) => completion?.Invoke();

    public void Stop() { }
}
