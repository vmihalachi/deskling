using Laching.Companion.Shell.Speech;

namespace Laching.Testing;

/// <summary>
/// Timers a test runs by hand: <see cref="RunNext"/> fires the oldest pending one-shot whatever its delay (the Swift
/// tests' scripted <c>schedule</c>), <see cref="Advance"/> moves time and fires everything due, in order, repeating
/// timers included, moving <paramref name="clock"/> with it when given. A disposed timer never fires.
/// </summary>
public sealed class ManualTimers(MockClock? clock = null) : ITimers
{
    private readonly List<Entry> entries = [];
    private double now;
    private long order;

    /// <summary>The delays of the one-shots still pending, oldest first.</summary>
    public IReadOnlyList<double> Delays => entries.Where(e => e.Interval is null).Select(e => e.Delay).ToArray();

    /// <summary>How many repeating timers are running.</summary>
    public int RepeatingCount => entries.Count(e => e.Interval is not null);

    public IDisposable After(double seconds, Action work) => Add(seconds, null, work);

    public IDisposable Every(double seconds, Action work) => Add(seconds, seconds, work);

    /// <summary>Fires the oldest pending one-shot now, without moving time.</summary>
    public void RunNext()
    {
        var entry = entries.FirstOrDefault(e => e.Interval is null) ?? throw new InvalidOperationException("no timer is pending");
        entries.Remove(entry);
        entry.Work();
    }

    /// <summary>Moves time <paramref name="seconds"/> ahead, firing every timer that comes due on the way, in order.</summary>
    public void Advance(double seconds)
    {
        var end = now + seconds;
        while (entries.Where(e => e.Due <= end).OrderBy(e => e.Due).ThenBy(e => e.Order).FirstOrDefault() is { } entry)
        {
            Move(entry.Due);
            if (entry.Interval is { } interval)
                entry.Due += interval;
            else
                entries.Remove(entry);
            entry.Work();
        }
        Move(end);
    }

    private void Move(double to)
    {
        clock?.AdvanceSeconds(to - now);
        now = to;
    }

    private Entry Add(double delay, double? interval, Action work)
    {
        var entry = new Entry(this, delay, interval, work) { Due = now + delay, Order = order++ };
        entries.Add(entry);
        return entry;
    }

    private sealed class Entry(ManualTimers owner, double delay, double? interval, Action work) : IDisposable
    {
        public double Delay { get; } = delay;

        public double? Interval { get; } = interval;

        public Action Work { get; } = work;

        public double Due { get; set; }

        public long Order { get; init; }

        public void Dispose() => owner.entries.Remove(this);
    }
}
