#if os(macOS)
    import DesklingStore
    import Foundation

    /// A `StoreBackend` for tests: every product in the catalog is listed (priced "€" plus its id's length),
    /// buying an unlockable makes it owned, buying a tip doesn't, and switches make loading, buying or
    /// restoring fail. `send` plays a transaction update as if it came from the App Store.
    @MainActor
    public final class MockStoreBackend: StoreBackend {
        public let catalog: ProductCatalog
        /// What `entitledProductIDs` answers; set it to simulate a refund or a purchase elsewhere.
        public var owned: Set<String> = []
        public var nextOutcome: PurchaseOutcome = .purchased
        public var failPurchase = false
        public var failProducts = false
        public var failRestore = false
        /// Ids `loadProducts` leaves out, as if App Store Connect didn't list them.
        public var missingProducts: Set<String> = []
        public private(set) var purchased: [String] = []
        public private(set) var restores = 0
        private var onUpdate: (@MainActor (TransactionUpdate) -> Void)?

        public struct Failure: Error, Sendable {
            public init() {}
        }

        public init(catalog: ProductCatalog) {
            self.catalog = catalog
        }

        public func loadProducts(_ ids: [String]) async throws -> [StoreProduct] {
            if failProducts { throw Failure() }
            return ids.filter { !missingProducts.contains($0) }.map { StoreProduct(id: $0, displayPrice: "€\($0.count)") }
        }

        public func entitledProductIDs() async -> Set<String> { owned }

        public func purchase(_ id: String) async throws -> PurchaseOutcome {
            if failPurchase { throw Failure() }
            purchased.append(id)
            if nextOutcome == .purchased, catalog.isUnlockable(id) { owned.insert(id) }
            return nextOutcome
        }

        public func restore() async throws {
            if failRestore { throw Failure() }
            restores += 1
        }

        public func observeTransactions(_ onUpdate: @escaping @MainActor (TransactionUpdate) -> Void) {
            self.onUpdate = onUpdate
        }

        /// Simulates a transaction arriving from outside the app (refund, Ask to Buy, another device).
        public func send(_ update: TransactionUpdate) { onUpdate?(update) }
    }
#endif
