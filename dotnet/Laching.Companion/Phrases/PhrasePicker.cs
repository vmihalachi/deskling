namespace Laching.Companion.Phrases;

/// <summary>
/// Picks the phrase that fits the moment. Pure: the same bank, context and random unit always give the same phrase,
/// and <c>conformance/companion/picker</c> pins it.
/// <para>
/// Rules: phrases that don't exist in the context's language are dropped first, then excluded ids, lines the current
/// skin can't say and lines whose flavor is turned off, as if they weren't in the bank; an event's own lines win when
/// there is an event (falling back to ambient lines when it has none); ambient chatter draws from
/// <see cref="Tag.General"/> plus the current daypart and weekday tags, never from event lines; tunes only with the
/// chime on; the last <see cref="RecentWindow"/> ids are skipped unless that would leave nothing (then only the very
/// last one is); a line whose daypart tag matches the hour counts double; the draw is weighted, in bank order, from one
/// uniform unit in [0, 1). A sequence is one candidate like a line.
/// </para>
/// </summary>
public static class PhrasePicker
{
    /// <summary>How many recent lines are kept out of the draw.</summary>
    public const int RecentWindow = 12;

    public static Phrase? Pick(PhraseBank bank, PhraseContext context, double unit)
    {
        var candidates = Candidates(bank, context);
        if (candidates.Count == 0)
            return null;
        var daypart = Tags.Daypart(context.Hour);
        var weights = candidates.Select(p => p.Weight * (p.Tags.Contains(daypart) ? 2 : 1)).ToArray();
        var total = weights.Sum();
        var remaining = (int)Math.Floor(unit * total);
        remaining = Math.Min(Math.Max(remaining, 0), total - 1);
        for (var index = 0; index < candidates.Count; index++)
        {
            if (remaining < weights[index])
                return candidates[index];
            remaining -= weights[index];
        }
        return candidates[candidates.Count - 1];
    }

    /// <summary>The phrases the draw chooses from, in bank order.</summary>
    public static IReadOnlyList<Phrase> Candidates(PhraseBank bank, PhraseContext context)
    {
        var available = bank.InLanguage(context.Language)
            .Where(p =>
                !context.ExcludedIDs.Contains(p.Id)
                && p.Fits(context.Skin)
                && (p.Flavor is not { } flavor || context.Flavors.Count == 0 || context.Flavors.Contains(flavor))
            )
            .ToList();
        List<Phrase> pool;
        if (context.Event is { } eventTag && available.Any(p => p.Tags.Contains(eventTag)))
        {
            pool = available.Where(p => p.Tags.Contains(eventTag)).ToList();
        }
        else
        {
            var tags = new HashSet<Tag> { Tag.General, Tags.Daypart(context.Hour) };
            if (Tags.WeekdayTag(context.Weekday) is { } weekdayTag)
                tags.Add(weekdayTag);
            pool = available.Where(p => !p.HasEventTag && p.Tags.Overlaps(tags)).ToList();
        }
        if (!context.ChimeEnabled)
            pool.RemoveAll(p => p.IsTune);
        var recent = context.RecentIDs.TakeLast(RecentWindow).ToHashSet(StringComparer.Ordinal);
        var fresh = pool.Where(p => !recent.Contains(p.Id)).ToList();
        if (fresh.Count > 0)
            return fresh;
        if (context.RecentIDs.Count > 0)
        {
            var last = context.RecentIDs[context.RecentIDs.Count - 1];
            var notLast = pool.Where(p => p.Id != last).ToList();
            if (notLast.Count > 0)
                return notLast;
        }
        return pool;
    }
}
