package laching.android

import android.app.Activity
import android.content.Context
import android.os.Handler
import android.os.Looper
import com.android.billingclient.api.AcknowledgePurchaseParams
import com.android.billingclient.api.BillingClient
import com.android.billingclient.api.BillingClientStateListener
import com.android.billingclient.api.BillingFlowParams
import com.android.billingclient.api.BillingResult
import com.android.billingclient.api.ConsumeParams
import com.android.billingclient.api.PendingPurchasesParams
import com.android.billingclient.api.ProductDetails
import com.android.billingclient.api.Purchase
import com.android.billingclient.api.QueryProductDetailsParams
import com.android.billingclient.api.QueryPurchasesParams
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import laching.core.store.ProductCatalog
import laching.core.store.PurchaseOutcome
import laching.core.store.StoreBackend
import laching.core.store.StoreProduct
import laching.core.store.TransactionUpdate
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

/**
 * [StoreBackend] over Google Play Billing: connects on first use (and reconnects by itself), lists the catalog's
 * one-time products, runs the purchase flow from the app's current [activity], acknowledges unlockables and consumes
 * tips, and keeps what the user owns in SharedPreferences so [entitledProductIds] answers offline. Purchases that
 * finish outside [purchase] (a pending approval, a refund, another device) reach [observeTransactions]. The only
 * part of a Laching Android app that talks to Play; shows nothing but Play's own sheet.
 */
public class PlayBillingBackend(
    context: Context,
    private val catalog: ProductCatalog,
    private val activity: () -> Activity?,
    private val log: (String) -> Unit = {},
) : StoreBackend {
    /** Play refused or failed; [code] is a `BillingClient.BillingResponseCode`. */
    public class BillingException(
        public val code: Int,
        message: String,
    ) : Exception("$message (code $code)")

    private val preferences = context.getSharedPreferences("laching.billing", Context.MODE_PRIVATE)
    private val main = Handler(Looper.getMainLooper())
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val details = HashMap<String, ProductDetails>()
    private var onUpdate: ((TransactionUpdate) -> Unit)? = null
    private var inFlight: CompletableDeferred<PurchaseOutcome>? = null
    private var inFlightProduct: String? = null
    private var connecting: CompletableDeferred<Unit>? = null

    private val client: BillingClient =
        BillingClient
            .newBuilder(context)
            .setListener { result, purchases -> onPurchasesUpdated(result, purchases) }
            .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
            .enableAutoServiceReconnection()
            .build()

    override suspend fun loadProducts(ids: List<String>): List<StoreProduct> {
        connect()
        val products =
            ids.map { id ->
                QueryProductDetailsParams.Product
                    .newBuilder()
                    .setProductId(id)
                    .setProductType(BillingClient.ProductType.INAPP)
                    .build()
            }
        val params = QueryProductDetailsParams.newBuilder().setProductList(products).build()
        val found =
            suspendCancellableCoroutine { continuation ->
                client.queryProductDetailsAsync(params) { result, queryResult ->
                    if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                        continuation.resume(queryResult.productDetailsList)
                    } else {
                        continuation.resumeWithException(result.toException("Loading products failed"))
                    }
                }
            }
        for (product in found) details[product.productId] = product
        return found.mapNotNull { product ->
            val price = product.oneTimePurchaseOfferDetails?.formattedPrice ?: return@mapNotNull null
            StoreProduct(product.productId, price)
        }
    }

    override suspend fun entitledProductIds(): Set<String> {
        val purchases =
            try {
                connect()
                queryPurchases()
            } catch (e: BillingException) {
                log("Reading purchases failed, using the cache: ${e.message}")
                return cachedOwned()
            }
        val owned = HashSet<String>()
        for (purchase in purchases) {
            if (purchase.purchaseState != Purchase.PurchaseState.PURCHASED) continue
            for (id in purchase.products) if (catalog.isUnlockable(id)) owned += id
            settle(purchase)
        }
        preferences.edit().putStringSet(OWNED_KEY, owned).apply()
        return owned
    }

    override suspend fun purchase(id: String): PurchaseOutcome {
        connect()
        val product =
            details[id] ?: run {
                loadProducts(listOf(id))
                details[id]
            } ?: throw BillingException(
                BillingClient.BillingResponseCode.ITEM_UNAVAILABLE,
                "Play doesn't list $id",
            )
        val activity =
            activity() ?: throw BillingException(BillingClient.BillingResponseCode.ERROR, "No activity to buy from")
        if (inFlight !=
            null
        ) {
            throw BillingException(BillingClient.BillingResponseCode.ERROR, "A purchase is already under way")
        }
        val outcome = CompletableDeferred<PurchaseOutcome>()
        inFlight = outcome
        inFlightProduct = id
        val params =
            BillingFlowParams
                .newBuilder()
                .setProductDetailsParamsList(
                    listOf(
                        BillingFlowParams.ProductDetailsParams
                            .newBuilder()
                            .setProductDetails(product)
                            .build(),
                    ),
                ).build()
        val launched = client.launchBillingFlow(activity, params)
        if (launched.responseCode != BillingClient.BillingResponseCode.OK) {
            inFlight = null
            inFlightProduct = null
            if (launched.responseCode ==
                BillingClient.BillingResponseCode.USER_CANCELED
            ) {
                return PurchaseOutcome.CANCELLED
            }
            throw launched.toException("Starting the purchase failed")
        }
        return try {
            outcome.await()
        } finally {
            inFlight = null
            inFlightProduct = null
        }
    }

    override suspend fun restore() {
        connect()
        val purchases = queryPurchases()
        val owned = HashSet<String>()
        for (purchase in purchases) {
            if (purchase.purchaseState != Purchase.PurchaseState.PURCHASED) continue
            for (id in purchase.products) if (catalog.isUnlockable(id)) owned += id
            settle(purchase)
        }
        preferences.edit().putStringSet(OWNED_KEY, owned).apply()
    }

    override fun observeTransactions(onUpdate: (TransactionUpdate) -> Unit) {
        this.onUpdate = onUpdate
    }

    private suspend fun connect() {
        if (client.isReady) return
        connecting?.let { return it.await() }
        val ready = CompletableDeferred<Unit>()
        connecting = ready
        client.startConnection(
            object : BillingClientStateListener {
                override fun onBillingSetupFinished(result: BillingResult) {
                    if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                        ready.complete(Unit)
                    } else {
                        ready.completeExceptionally(result.toException("Connecting to Play failed"))
                    }
                    connecting = null
                }

                override fun onBillingServiceDisconnected() {
                    // enableAutoServiceReconnection reconnects before the next call.
                }
            },
        )
        ready.await()
    }

    private suspend fun queryPurchases(): List<Purchase> =
        suspendCancellableCoroutine { continuation ->
            val params = QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.INAPP).build()
            client.queryPurchasesAsync(params) { result, purchases ->
                if (result.responseCode == BillingClient.BillingResponseCode.OK) {
                    continuation.resume(purchases)
                } else {
                    continuation.resumeWithException(result.toException("Reading purchases failed"))
                }
            }
        }

    /** Acknowledges a bought unlockable, consumes a tip, so Play doesn't refund it after three days. */
    private fun settle(purchase: Purchase) {
        if (purchase.purchaseState != Purchase.PurchaseState.PURCHASED) return
        val token = purchase.purchaseToken
        if (purchase.products.any(catalog::isTip)) {
            val params = ConsumeParams.newBuilder().setPurchaseToken(token).build()
            client.consumeAsync(params) { result, _ ->
                if (result.responseCode !=
                    BillingClient.BillingResponseCode.OK
                ) {
                    log("Consuming a tip failed: ${result.debugMessage}")
                }
            }
        } else if (!purchase.isAcknowledged) {
            val params = AcknowledgePurchaseParams.newBuilder().setPurchaseToken(token).build()
            client.acknowledgePurchase(params) { result ->
                if (result.responseCode !=
                    BillingClient.BillingResponseCode.OK
                ) {
                    log("Acknowledging failed: ${result.debugMessage}")
                }
            }
        }
    }

    private fun onPurchasesUpdated(
        result: BillingResult,
        purchases: List<Purchase>?,
    ) {
        main.post {
            val waiting = inFlight
            val waitingFor = inFlightProduct
            when (result.responseCode) {
                BillingClient.BillingResponseCode.OK -> {
                    for (purchase in purchases.orEmpty()) {
                        val state = purchase.purchaseState
                        if (state == Purchase.PurchaseState.PURCHASED) {
                            settle(purchase)
                            val owned = cachedOwned().toMutableSet()
                            for (id in purchase.products) if (catalog.isUnlockable(id)) owned += id
                            preferences.edit().putStringSet(OWNED_KEY, owned).apply()
                        }
                        for (id in purchase.products) {
                            if (waiting != null && id == waitingFor) {
                                val outcome =
                                    if (state ==
                                        Purchase.PurchaseState.PENDING
                                    ) {
                                        PurchaseOutcome.PENDING
                                    } else {
                                        PurchaseOutcome.PURCHASED
                                    }
                                waiting.complete(outcome)
                            } else if (state == Purchase.PurchaseState.PURCHASED) {
                                onUpdate?.invoke(TransactionUpdate(id, isRevoked = false))
                            }
                        }
                    }
                    // Play told us purchases changed but listed none we waited for: let the store re-read.
                    if (waiting != null && !waiting.isCompleted) waiting.complete(PurchaseOutcome.CANCELLED)
                    if (purchases.isNullOrEmpty()) onUpdate?.invoke(TransactionUpdate("", isRevoked = false))
                }

                BillingClient.BillingResponseCode.USER_CANCELED -> {
                    waiting?.complete(PurchaseOutcome.CANCELLED)
                }

                BillingClient.BillingResponseCode.ITEM_ALREADY_OWNED -> {
                    scope.launch { runCatching { entitledProductIds() } }
                    waiting?.complete(PurchaseOutcome.PURCHASED)
                }

                else -> {
                    waiting?.completeExceptionally(result.toException("The purchase failed"))
                }
            }
        }
    }

    private fun cachedOwned(): Set<String> = preferences.getStringSet(OWNED_KEY, emptySet()).orEmpty().toSet()

    private fun BillingResult.toException(what: String): BillingException =
        BillingException(responseCode, "$what: ${debugMessage.ifEmpty { "no details" }}")

    private companion object {
        const val OWNED_KEY = "owned"
    }
}
