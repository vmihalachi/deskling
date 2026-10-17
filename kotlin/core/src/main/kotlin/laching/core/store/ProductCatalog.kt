package laching.core.store

/**
 * The app's in-app purchases by product id, as set up in the store: non-consumable [unlockables] the user owns once
 * bought, and consumable [tips] that only say thanks. Nothing else is needed; the store never guesses ids. On Android
 * the id is the product ID in the Play Console.
 */
public class ProductCatalog(
    public val unlockables: List<String>,
    public val tips: List<String> = emptyList(),
) {
    /** Every product, unlockables first. */
    public val all: List<String>
        get() = unlockables + tips

    public fun isUnlockable(id: String): Boolean = id in unlockables

    public fun isTip(id: String): Boolean = id in tips
}
