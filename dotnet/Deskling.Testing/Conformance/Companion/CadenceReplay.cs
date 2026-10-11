using System.Text.Json;
using Deskling.Companion.Cadence;
using Deskling.Core.Random;
using Deskling.Core.Scheduling;

namespace Deskling.Testing.Conformance;

/// <summary>
/// Replays a <c>cadence/*.json</c> (Swift's <c>CadenceVector.verify</c>): the <c>settings</c> must build the committed
/// <c>config</c> through <see cref="ChatterCadence"/>, and a scheduler seeded with <c>seed</c>, ticked once a minute
/// from <c>start</c> for <c>minutes</c> with nobody busy or away, must fire at the committed <c>fireMinutes</c>.
/// </summary>
public static class CadenceReplay
{
    /// <summary>The problems found, empty when the cadence agrees with the file.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var s = root.GetProperty("settings");
        var frequency =
            Frequencies.Parse(s.GetProperty("frequency").GetString())
            ?? throw new InvalidDataException($"{file}: unknown frequency {s.GetProperty("frequency")}");
        var config = ChatterCadence.Config(
            frequency,
            s.GetProperty("activeStartMinute").GetInt32(),
            s.GetProperty("activeEndMinute").GetInt32(),
            VectorSupport.Ints(s.GetProperty("activeWeekdays")).ToHashSet(),
            VectorSupport.Ints(s.GetProperty("quietHours")).ToHashSet(),
            s.GetProperty("hushInFullScreen").GetBoolean()
        );
        var problems = new List<string>();
        var c = root.GetProperty("config");
        var same =
            c.GetProperty("intervalSeconds").GetInt32() == (int)config.IntervalSeconds
            && c.GetProperty("intervalJitterSeconds").GetInt32() == (int)config.IntervalJitterSeconds
            && c.GetProperty("activeStartMinute").GetInt32() == config.ActiveStartMinute
            && c.GetProperty("activeEndMinute").GetInt32() == config.ActiveEndMinute
            && VectorSupport.Ints(c.GetProperty("activeWeekdays")).SequenceEqual(config.ActiveWeekdays.Order())
            && VectorSupport.Ints(c.GetProperty("quietHours")).SequenceEqual(config.QuietHours.Order())
            && c.GetProperty("idleThresholdSeconds").GetInt32() == (int)config.IdleThresholdSeconds
            && c.GetProperty("postponeDuringCalls").GetBoolean() == config.PostponeDuringCalls
            && c.GetProperty("postponeDuringFullScreen").GetBoolean() == config.PostponeDuringFullScreen
            && c.GetProperty("busyGraceSeconds").GetInt32() == (int)config.BusyGraceSeconds
            && c.GetProperty("afterCallFallbackSeconds").GetInt32() == (int)config.AfterCallFallbackSeconds;
        if (!same)
            problems.Add($"{file}: config differs");
        var clock = new MockClock(ConformanceSupport.Date(root.GetProperty("start").GetString()!));
        var scheduler = new ReminderScheduler(
            config,
            clock,
            new MockIdle(),
            new MockBusy(),
            ConformanceSupport.TimeZone(root.GetProperty("timeZone").GetString()!),
            new SplitMix64(root.GetProperty("seed").GetUInt64())
        );
        var fires = new List<int>();
        var minutes = root.GetProperty("minutes").GetInt32();
        for (var minute = 1; minute <= minutes; minute++)
        {
            clock.AdvanceMinutes(1);
            if (scheduler.Tick() == SchedulerDecision.Remind)
                fires.Add(minute);
        }
        var expected = VectorSupport.Ints(root.GetProperty("fireMinutes"));
        if (!fires.SequenceEqual(expected))
            problems.Add($"{file}: fires [{string.Join(", ", fires)}] != [{string.Join(", ", expected)}]");
        return problems;
    }
}
