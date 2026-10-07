#if os(macOS)
    import Foundation
    import StoreKit
    import os

    /// The StoreKit 2 implementation of `StoreBackend`. Verifies every transaction (unverified ones are
    /// ignored), finishes them, and treats a revocation date as "not owned". Needs the app's products in
    /// App Store Connect (or a `.storekit` configuration while developing); talks only to the App Store.
    @MainActor
    public final class StoreKitBackend: StoreBackend {
        private var cache: [String: Product] = [:]
        private var updates: Task<Void, Never>?
        private let logger: Logger

        public init(logger: Logger = Logger(subsystem: "deskling", category: "store")) {
            self.logger = logger
        }

        deinit { updates?.cancel() }

        public func loadProducts(_ ids: [String]) async throws -> [StoreProduct] {
            let storefront = await Storefront.current
            logger.notice("Storefront: \(storefront?.countryCode ?? "none", privacy: .public)")
            let products = try await Product.products(for: ids)
            for p in products { cache[p.id] = p }
            return products.map { StoreProduct(id: $0.id, displayPrice: $0.displayPrice) }
        }

        public func entitledProductIDs() async -> Set<String> {
            var ids = Set<String>()
            for await result in Transaction.currentEntitlements {
                if case .verified(let t) = result, t.revocationDate == nil { ids.insert(t.productID) }
            }
            return ids
        }

        public func purchase(_ id: String) async throws -> PurchaseOutcome {
            let product: Product
            if let cached = cache[id] {
                product = cached
            } else if let fetched = try await Product.products(for: [id]).first {
                cache[id] = fetched
                product = fetched
            } else {
                throw StoreKitError.notAvailableInStorefront
            }
            switch try await product.purchase() {
            case .success(let result):
                guard case .verified(let transaction) = result else { throw StoreKitError.notEntitled }
                await transaction.finish()
                return .purchased
            case .pending:
                return .pending
            case .userCancelled:
                return .cancelled
            @unknown default:
                return .cancelled
            }
        }

        public func restore() async throws {
            try await AppStore.sync()
        }

        public func observeTransactions(_ onUpdate: @escaping @MainActor (TransactionUpdate) -> Void) {
            updates?.cancel()
            updates = Task.detached {
                for await result in Transaction.updates {
                    guard case .verified(let transaction) = result else { continue }
                    await transaction.finish()
                    let update = TransactionUpdate(
                        productID: transaction.productID,
                        isRevoked: transaction.revocationDate != nil)
                    await onUpdate(update)
                }
            }
        }
    }
#endif
