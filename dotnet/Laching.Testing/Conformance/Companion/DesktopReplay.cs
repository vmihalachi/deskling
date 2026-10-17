using System.Text.Json;
using Laching.Companion.Desktop;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;

namespace Laching.Testing.Conformance;

/// <summary>
/// Replays a desktop.json (Swift's <c>DesktopVector.verify</c>): the constants, <see cref="DesktopPresence"/> step for
/// step over each timeline (steps at <c>t</c> seconds after Unix 1 800 000 000), each sample line's bubble time and,
/// when present, each <c>partDelays</c> entry through <see cref="PhraseTiming.PartDelaySeconds"/>.
/// </summary>
public static class DesktopReplay
{
    private static readonly DateTimeOffset Start = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    /// <summary>The problems found, empty when the code agrees with the file.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var problems = new List<string>();
        if (root.GetProperty("reappearDelaySeconds").GetInt32() != (int)DesktopPresence.ReappearDelaySeconds)
            problems.Add($"{file}: reappearDelaySeconds differs");
        if (root.GetProperty("hourHideSeconds").GetInt32() != (int)DesktopPresence.HourHideSeconds)
            problems.Add($"{file}: hourHideSeconds differs");
        foreach (var timeline in root.GetProperty("timelines").EnumerateArray())
        {
            var name = timeline.GetProperty("name").GetString();
            var presence = new DesktopPresence();
            foreach (var step in timeline.GetProperty("steps").EnumerateArray())
            {
                var t = step.GetProperty("t").GetDouble();
                var now = Start.AddSeconds(t);
                if (VectorSupport.Has(step, "hideForSeconds", out var hide))
                    presence.Hide(now.AddSeconds(hide.GetInt32()));
                if (VectorSupport.Has(step, "showNow", out var showNow) && showNow.GetBoolean())
                    presence.ShowNow();
                var snapshot = new Snapshot
                {
                    Busy = VectorSupport.Strings(step.GetProperty("busy")).Select(VectorSupport.ParseBusy).OfType<Laching.Core.Scheduling.BusyReason>().ToHashSet(),
                };
                var shown = presence.Update(step.GetProperty("enabled").GetBoolean(), snapshot, now);
                if (shown != step.GetProperty("shown").GetBoolean())
                {
                    problems.Add($"{file}: timeline {name} differs at {t} s");
                    break;
                }
            }
        }
        foreach (var bubble in root.GetProperty("bubbles").EnumerateArray())
        {
            var text = bubble.GetProperty("text").GetString()!;
            if (Math.Abs(DesktopPresence.BubbleSeconds(text) - bubble.GetProperty("seconds").GetDouble()) > 1e-9)
                problems.Add($"{file}: bubble seconds for \"{text}\" differ");
        }
        if (VectorSupport.Has(root, "partDelays", out var delays))
        {
            foreach (var delay in delays.EnumerateArray())
            {
                var spoken = delay.GetProperty("spoken").GetBoolean();
                var previous = delay.GetProperty("previousPart").GetString()!;
                if (Math.Abs(PhraseTiming.PartDelaySeconds(spoken, previous) - delay.GetProperty("seconds").GetDouble()) > 1e-9)
                    problems.Add($"{file}: the part delay after \"{previous}\" (spoken: {spoken}) differs");
            }
        }
        return problems;
    }
}
