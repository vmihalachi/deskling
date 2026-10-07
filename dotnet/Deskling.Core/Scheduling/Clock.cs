namespace Deskling.Core.Scheduling;

/// <summary>Where the scheduler reads the time from, so tests and conformance vectors can drive it.</summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}

/// <summary>The wall clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
