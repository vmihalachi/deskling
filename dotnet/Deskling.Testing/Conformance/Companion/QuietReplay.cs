using System.Text.Json;
using Deskling.Companion.Cadence;
using Deskling.Companion.Coordination;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Core.Settings;

namespace Deskling.Testing.Conformance;

/// <summary>
/// Replays a <c>quiet/*.json</c> (Swift's <c>QuietTimeline.verify</c>): apply the <c>settings</c> over
/// <c>defaults</c>, build a <see cref="CompanionCoordinator"/> with the vector's <c>seed</c> and <c>language</c> (absent
/// = "en"), then for each step advance the clock and perform the action <c>repeat</c> times with the step's snapshot,
/// and compare the deliveries (index, at, id, trigger, speak, chime, notify), the mood, <c>isShushed</c> and
/// <c>shushedUntil</c> with the committed step.
/// </summary>
public static class QuietReplay
{
    /// <summary>The problems found, one per differing step; empty when the timeline replays exactly.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file, PhraseBank bank, CompanionDefaults defaults)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var tz = ConformanceSupport.TimeZone(root.GetProperty("timeZone").GetString()!);
        var clock = new MockClock(ConformanceSupport.Date(root.GetProperty("start").GetString()!));
        var idle = new MockIdle();
        var busy = new MockBusy();
        var settings = new CompanionSettings(new InMemorySettingsStore(), defaults);
        ApplySettings(root.GetProperty("settings"), settings, file);
        var coordinator = new CompanionCoordinator(
            settings,
            bank,
            VectorSupport.OptionalString(root, "language") ?? "en",
            clock,
            idle,
            busy,
            tz,
            root.GetProperty("seed").GetUInt64()
        );
        var problems = new List<string>();
        var index = 0;
        foreach (var step in root.GetProperty("steps").EnumerateArray())
        {
            var action = step.GetProperty("action").GetString()!;
            var where = $"{file}: step {index} ({action})";
            var argument = VectorSupport.OptionalString(step, "argument");
            var repeat = VectorSupport.Has(step, "repeat", out var r) ? r.GetInt32() : 1;
            var advance = step.GetProperty("advanceSeconds").GetInt32();
            var snapshot = VectorSupport.ReadSnapshot(step.GetProperty("snapshot"));
            var deliveries = new List<string>();
            for (var i = 0; i < repeat; i++)
            {
                clock.AdvanceSeconds(advance);
                idle.Idle = snapshot.IdleSeconds;
                busy.Reasons = snapshot.Busy.ToHashSet();
                if (Perform(coordinator, settings, action, argument, snapshot, where) is { } delivery)
                {
                    var plan = delivery.Plan;
                    var at = ConformanceSupport.Iso(clock.Now, tz);
                    deliveries.Add(Describe(i, at, delivery.Phrase.Id, delivery.Trigger.RawValue(), plan.Speak, plan.Chime, plan.Notify));
                }
            }
            var expected = Expected(step);
            var actual = new Outcome(
                string.Join("; ", deliveries),
                coordinator.Mood(snapshot).RawValue(),
                coordinator.IsShushed,
                settings.ShushedUntil is { } until ? ConformanceSupport.Iso(until, tz) : null
            );
            if (expected != actual)
                problems.Add($"{where} differs: got {actual}, expected {expected}");
            index++;
        }
        return problems;
    }

    private static void ApplySettings(JsonElement s, CompanionSettings settings, string file)
    {
        settings.Channels = Channels.FromNames(VectorSupport.Strings(s.GetProperty("channels")));
        settings.Frequency =
            Frequencies.Parse(s.GetProperty("frequency").GetString())
            ?? throw new InvalidDataException($"{file}: unknown frequency {s.GetProperty("frequency")}");
        settings.ActiveStartMinute = s.GetProperty("activeStartMinute").GetInt32();
        settings.ActiveEndMinute = s.GetProperty("activeEndMinute").GetInt32();
        settings.ActiveWeekdays = VectorSupport.Ints(s.GetProperty("activeWeekdays")).ToHashSet();
        settings.QuietHours = VectorSupport.Ints(s.GetProperty("quietHours")).ToHashSet();
        settings.HushInFullScreen = s.GetProperty("hushInFullScreen").GetBoolean();
        settings.GreetOnReturn = s.GetProperty("greetOnReturn").GetBoolean();
        settings.CommentOnPower = s.GetProperty("commentOnPower").GetBoolean();
        settings.CommentOnAppearance = s.GetProperty("commentOnAppearance").GetBoolean();
        settings.ShushedUntil = VectorSupport.OptionalString(s, "shushedUntil") is { } until ? ConformanceSupport.Date(until) : null;
    }

    private static Delivery? Perform(
        CompanionCoordinator coordinator,
        CompanionSettings settings,
        string action,
        string? argument,
        Snapshot snapshot,
        string where
    )
    {
        switch (action)
        {
            case "tick":
                return coordinator.Tick(snapshot);
            case "poke":
                return coordinator.Poke(snapshot);
            case "wake":
                return coordinator.Wake(snapshot);
            case "shush":
                coordinator.Shush(Shushes.Parse(argument) ?? throw new InvalidDataException($"{where}: unknown shush {argument}"));
                return null;
            case "event":
                return coordinator.Event(Triggers.Parse(argument) ?? throw new InvalidDataException($"{where}: unknown trigger {argument}"), snapshot);
            case "setFrequency":
                settings.Frequency = Frequencies.Parse(argument) ?? throw new InvalidDataException($"{where}: unknown frequency {argument}");
                coordinator.SettingsChanged();
                return null;
            case "setChannels":
                settings.Channels = Channels.FromNames((argument ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries));
                coordinator.SettingsChanged();
                return null;
            default:
                throw new InvalidDataException($"{where}: unknown action {action}");
        }
    }

    private static Outcome Expected(JsonElement step)
    {
        var deliveries = step.GetProperty("deliveries")
            .EnumerateArray()
            .Select(d =>
                Describe(
                    d.GetProperty("index").GetInt32(),
                    d.GetProperty("at").GetString()!,
                    d.GetProperty("id").GetString()!,
                    d.GetProperty("trigger").GetString()!,
                    d.GetProperty("speak").GetBoolean(),
                    d.GetProperty("chime").GetBoolean(),
                    d.GetProperty("notify").GetBoolean()
                )
            );
        return new Outcome(
            string.Join("; ", deliveries),
            step.GetProperty("mood").GetString()!,
            step.GetProperty("isShushed").GetBoolean(),
            VectorSupport.OptionalString(step, "shushedUntil")
        );
    }

    /// <summary>One delivery as text, so a whole step compares and prints as plain values.</summary>
    private static string Describe(int index, string at, string id, string trigger, bool speak, bool chime, bool notify) =>
        $"{index} {at} {id} {trigger} speak={speak} chime={chime} notify={notify}";

    private sealed record Outcome(string Deliveries, string Mood, bool IsShushed, string? ShushedUntil);
}
