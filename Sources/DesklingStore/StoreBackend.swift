#if os(macOS)
    import Foundation

    /// A product the App Store listed.
    public struct StoreProduct: Identifiable, Equatable, Sendable {
        public let id: String
        /// Localized by the App Store, e.g. "0,99 €".
        public let displayPrice: String

        public init(id: String, displayPrice: String) {
            self.id = id
            self.displayPrice = displayPrice
        }
    }

    public enum PurchaseOutcome: Equatable, Sendable {
        case purchased
        /// Waiting for Ask to Buy or a payment check.
        case pending
        case cancelled
    }

    /// A transaction that arrived outside `purchase`: Ask to Buy approval,
    /// a refund, or a purchase on another device.
    public struct TransactionUpdate: Equatable, Sendable {
        public let productID: String
        public let isRevoked: Bool

        public init(productID: String, isRevoked: Bool) {
            self.productID = productID
            self.isRevoked = isRevoked
        }
    }

    /// What `PurchaseStore` needs from the App Store. `StoreKitBackend` in the app, `MockStoreBackend`
    /// (`DesklingTesting`) in tests. The only part of a Deskling app that talks to the network.
    @MainActor
    public protocol StoreBackend: AnyObject {
        func loadProducts(_ ids: [String]) async throws -> [StoreProduct]
        /// Verified, unrevoked non-consumables the user owns. Works offline.
        func entitledProductIDs() async -> Set<String>
        func purchase(_ id: String) async throws -> PurchaseOutcome
        /// Asks the App Store to re-sync transactions (Restore purchases).
        func restore() async throws
        func observeTransactions(_ onUpdate: @escaping @MainActor (TransactionUpdate) -> Void)
    }
#endif
