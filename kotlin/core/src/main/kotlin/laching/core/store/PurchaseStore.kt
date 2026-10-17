package laching.core.store

import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import laching.core.settings.SettingsStore

public enum class PurchaseError {
    PURCHASE_FAILED,
    RESTORE_FAILED,
}

public enum class LoadState {
    IDLE,
    LOADING,
    LOADED,
    FAILED,
}

/**
 * The app's view of its in-app purchases: what the user owns, the listed prices, and whether a purchase or a
 * restore is under way. Pure logic over a [StoreBackend]; the same flow as `Laching.Core`'s `PurchaseStore`.
 * Ownership is cached in [settings] under [cacheKey] so the UI doesn't flash at launch. With the default key,
 * Deskling's cache under `deskling.ownedProducts` (before 0.13.0) is taken over once: its value moves there when the
 * default key holds nothing, and the old key is removed. A key the app passes is read as it is. Call it from one thread;
 * [scope] runs the re-reads that transaction updates trigger, and listeners registered with [onChanged] hear
 * about every visible change.
 */
public class PurchaseStore(
    public val catalog: ProductCatalog,
    private val backend: StoreBackend,
    private val settings: SettingsStore,
    private val scope: CoroutineScope,
    private val cacheKey: String = DEFAULT_CACHE_KEY,
    private val log: (String) -> Unit = {},
) {
    private val listeners = mutableListOf<() -> Unit>()
    private var started = false

    public var owned: Set<String> = cached(settings, cacheKey).toSet()
        private set(value) {
            field = value
            settings.setStrings(cacheKey, value.sorted())
        }

    public var products: Map<String, StoreProduct> = emptyMap()
        private set
    public var purchasing: String? = null
        private set
    public var isRestoring: Boolean = false
        private set
    public var isPending: Boolean = false
        private set
    public var justTipped: Boolean = false
        private set
    public var error: PurchaseError? = null
        private set
    public var loadState: LoadState = LoadState.IDLE
        private set

    public val isBusy: Boolean
        get() = purchasing != null || isRestoring

    public fun isOwned(id: String): Boolean = id in owned

    public fun price(id: String): String? = products[id]?.displayPrice

    /** Registers [listener] for every visible change; the returned function removes it. */
    public fun onChanged(listener: () -> Unit): () -> Unit {
        listeners += listener
        return { listeners -= listener }
    }

    public suspend fun start() {
        if (started) return
        started = true
        backend.observeTransactions { update ->
            if (!update.isRevoked && catalog.isTip(update.productId)) {
                isPending = false
                justTipped = true
                changed()
            }
            scope.launch { refreshEntitlements() }
        }
        refreshEntitlements()
        loadProducts()
    }

    public suspend fun loadProducts() {
        if (loadState == LoadState.LOADING) return
        loadState = LoadState.LOADING
        changed()
        val ids = catalog.all
        val loaded =
            try {
                backend.loadProducts(ids)
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log("Loading products failed: ${e::class.simpleName} ${e.message}")
                loadState = LoadState.FAILED
                changed()
                return
            }
        // The first of a repeated id wins; the backend's order is kept.
        val byId = loaded.distinctBy { it.id }.associateBy { it.id }
        val missing = (ids.toSet() - byId.keys).sorted()
        log("Loaded ${loaded.size}/${ids.size} products; missing: ${missing.joinToString(", ")}")
        products = byId
        val complete =
            if (catalog.unlockables.isEmpty()) byId.isNotEmpty() else catalog.unlockables.all { it in byId }
        loadState = if (complete) LoadState.LOADED else LoadState.FAILED
        changed()
    }

    public suspend fun loadProductsIfNeeded() {
        if (loadState == LoadState.LOADED || loadState == LoadState.LOADING) return
        loadProducts()
    }

    public suspend fun refreshEntitlements() {
        val entitled = backend.entitledProductIds().filter(catalog::isUnlockable).toSet()
        log("Owned: ${entitled.sorted().joinToString(", ")}")
        var changed = false
        if ((entitled - owned).isNotEmpty() && isPending) {
            isPending = false
            changed = true
        }
        if (entitled != owned) {
            owned = entitled
            changed = true
        }
        if (changed) changed()
    }

    public suspend fun buy(id: String): Unit = purchase(id)

    public suspend fun tip(id: String): Unit = purchase(id)

    public suspend fun restore() {
        if (isBusy) return
        isRestoring = true
        error = null
        changed()
        try {
            backend.restore()
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            log("Restore failed: ${e::class.simpleName} ${e.message}")
            error = PurchaseError.RESTORE_FAILED
        } finally {
            isRestoring = false
            changed()
        }
        refreshEntitlements()
    }

    public fun acknowledgeTip() {
        if (!justTipped) return
        justTipped = false
        changed()
    }

    public fun clearError() {
        if (error == null) return
        error = null
        changed()
    }

    private suspend fun purchase(id: String) {
        if (isBusy) return
        purchasing = id
        error = null
        isPending = false
        changed()
        try {
            when (backend.purchase(id)) {
                PurchaseOutcome.PURCHASED -> {
                    if (catalog.isTip(id)) justTipped = true
                    refreshEntitlements()
                }

                PurchaseOutcome.PENDING -> {
                    isPending = true
                }

                PurchaseOutcome.CANCELLED -> {}
            }
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            log("Purchase of $id failed: ${e::class.simpleName} ${e.message}")
            error = PurchaseError.PURCHASE_FAILED
        } finally {
            purchasing = null
            changed()
        }
    }

    private fun changed() {
        for (listener in listeners.toList()) listener()
    }

    private companion object {
        const val DEFAULT_CACHE_KEY = "laching.ownedProducts"
        const val DESKLING_CACHE_KEY = "deskling.ownedProducts"

        /**
         * The cache under [key]. Under the default key, Deskling's cache moves over when the key holds nothing, and is
         * removed either way.
         */
        fun cached(
            settings: SettingsStore,
            key: String,
        ): List<String> {
            val owned = settings.getStrings(key)
            if (key != DEFAULT_CACHE_KEY) return owned ?: emptyList()
            val deskling = settings.getStrings(DESKLING_CACHE_KEY) ?: return owned ?: emptyList()
            settings.remove(DESKLING_CACHE_KEY)
            if (owned != null) return owned
            settings.setStrings(key, deskling)
            return deskling
        }
    }
}
