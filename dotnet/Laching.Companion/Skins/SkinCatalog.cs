using Laching.Companion.Figures;
using Laching.Companion.Phrases;

namespace Laching.Companion.Skins;

/// <summary>One skin an app offers: its art, and the store product that unlocks it (null = free).</summary>
public sealed record SkinEntry(CharacterArt Art, string? UnlockedBy = null)
{
    public SkinId Id => Art.Skin;
}

/// <summary>
/// The skins an app offers, the default first. Decides which skin to draw from the one the person picked and the
/// products they own, so a refund or an unknown id drops back to the default. Pure; never talks to the store.
/// </summary>
public sealed class SkinCatalog
{
    /// <summary><paramref name="skins"/>[0] is the default and must be free; ids must be unique.</summary>
    public SkinCatalog(IEnumerable<SkinEntry> skins)
    {
        Skins = skins.ToArray();
        if (Skins.Count == 0)
            throw new ArgumentException("a skin catalog needs at least the default skin", nameof(skins));
        if (Skins[0].UnlockedBy is not null)
            throw new ArgumentException($"the default skin {Skins[0].Id} must be free", nameof(skins));
        if (Skins.Select(s => s.Id).Distinct().Count() != Skins.Count)
            throw new ArgumentException("skin ids must be unique", nameof(skins));
    }

    public IReadOnlyList<SkinEntry> Skins { get; }

    public SkinId DefaultSkin => Skins[0].Id;

    public SkinEntry? this[SkinId id] => Skins.FirstOrDefault(s => s.Id == id);

    /// <summary>The character for <paramref name="id"/>, or the default's when the catalog doesn't have it.</summary>
    public RiggedCharacter Character(SkinId id) => new((this[id] ?? Skins[0]).Art);

    /// <summary>Whether <paramref name="id"/> is in the catalog and free or unlocked by one of <paramref name="owned"/> (store product ids).</summary>
    public bool IsUnlocked(SkinId id, IReadOnlySet<string> owned) =>
        this[id] is { } entry && (entry.UnlockedBy is not { } product || owned.Contains(product));

    /// <summary><paramref name="selected"/> when it's unlocked, else the default: what to draw for a stored choice.</summary>
    public SkinId EffectiveSkin(SkinId selected, IReadOnlySet<string> owned) => IsUnlocked(selected, owned) ? selected : DefaultSkin;
}
