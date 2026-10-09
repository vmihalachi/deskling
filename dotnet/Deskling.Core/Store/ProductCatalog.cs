namespace Deskling.Core.Store;

/// <summary>
/// The app's in-app purchases by product id, as set up in the store: non-consumable <see cref="Unlockables"/> the
/// user owns once bought, and consumable <see cref="Tips"/> that only say thanks. Nothing else is needed; the store
/// never guesses ids. On Windows the id is the add-on's Product ID in Partner Center (its <c>InAppOfferToken</c>).
/// </summary>
public sealed class ProductCatalog
{
    public ProductCatalog(IReadOnlyList<string> unlockables, IReadOnlyList<string>? tips = null)
    {
        Unlockables = unlockables;
        Tips = tips ?? [];
    }

    public IReadOnlyList<string> Unlockables { get; }

    public IReadOnlyList<string> Tips { get; }

    /// <summary>Every product, unlockables first.</summary>
    public IReadOnlyList<string> All => [.. Unlockables, .. Tips];

    public bool IsUnlockable(string id) => Unlockables.Contains(id);

    public bool IsTip(string id) => Tips.Contains(id);
}
