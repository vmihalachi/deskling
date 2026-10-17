namespace Laching.Companion.Phrases;

/// <summary>
/// Everything the picker needs to know about the moment, as a value, so a pick is a pure function. The skin and the
/// language have no defaults: they are the app's.
/// </summary>
/// <param name="Hour">Local hour of the day, 0–23.</param>
/// <param name="Weekday">Calendar weekday, 1 = Sunday … 7 = Saturday.</param>
/// <param name="ChimeEnabled">Tunes play only when the chime channel is on.</param>
/// <param name="Skin">The character speaking: lines it can't say are never picked.</param>
/// <param name="Language">The app's language (BCP-47): phrases that don't exist in it are never picked.</param>
public sealed record PhraseContext(int Hour, int Weekday, bool ChimeEnabled, SkinId Skin, string Language)
{
    /// <summary>What just happened, or null for ambient chatter.</summary>
    public Tag? Event { get; init; }

    /// <summary>The ids said most recently, oldest first.</summary>
    public IReadOnlyList<string> RecentIDs { get; init; } = [];

    /// <summary>The flavors the user keeps on; empty means all. Tunes have no flavor and are never filtered by it.</summary>
    public IReadOnlySet<FlavorId> Flavors { get; init; } = new HashSet<FlavorId>();

    /// <summary>Ids never to pick: lines the user muted, and lines pending a recording in the voice in use.</summary>
    public IReadOnlySet<string> ExcludedIDs { get; init; } = new HashSet<string>(StringComparer.Ordinal);
}
