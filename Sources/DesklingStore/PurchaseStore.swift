#if os(macOS)
    import Foundation
    import os

    /// Why a purchase or restore didn't go through. The app maps these to its own localized text.
    public enum PurchaseError: Error, Equatable, Sendable {
        /// The purchase failed or was refused; the user hasn't been charged.
        case purchaseFailed
        /// The App Store couldn't re-sync transactions.
        case restoreFailed
    }

    /// Ownership of the catalog's unlockables plus tips, over a `StoreBackend`. The last known `owned` set is
    /// cached in `defaults` under `cacheKey` so the UI doesn't flash locked at launch before StoreKit answers.
    /// Needs `start()` once; it then follows transaction updates (Ask to Buy approvals, refunds, purchases on
    /// another device). Never shows UI of its own and never charges without `buy` or `tip`.
    @MainActor
    public final class PurchaseStore: ObservableObject {
        /// The unlockables the user owns, verified and unrevoked. Tips never appear here.
        @Published public private(set) var owned: Set<String> {
            didSet { defaults.set(owned.sorted(), forKey: cacheKey) }
        }
        @Published public private(set) var products: [String: StoreProduct] = [:]
        /// Product being bought, to disable the buttons meanwhile.
        @Published public private(set) var purchasing: String?
        @Published public private(set) var isRestoring = false
        /// A purchase is waiting for approval (Ask to Buy).
        @Published public private(set) var isPending = false
        /// Set after a tip goes through. The view shows its thank-you note, then calls `acknowledgeTip()`.
        @Published public private(set) var justTipped = false
        /// The last failure; cleared when the next purchase or restore starts, or by the app.
        @Published public var error: PurchaseError?
        /// Where the price lookup stands, so a buy button can say "getting price" or offer a retry.
        @Published public private(set) var loadState: LoadState = .idle

        public enum LoadState: Equatable, Sendable {
            case idle, loading, loaded
            /// The App Store didn't answer, or didn't list an unlockable.
            case failed
        }

        public let catalog: ProductCatalog
        private let backend: StoreBackend
        private let defaults: UserDefaults
        private let cacheKey: String
        private let logger: Logger
        private var started = false

        /// `backend` nil means StoreKit 2 (the default can't be written inline: `StoreKitBackend` is main-actor
        /// isolated and default arguments aren't, in Swift 5 mode).
        public init(
            catalog: ProductCatalog, backend: StoreBackend? = nil, defaults: UserDefaults = .standard,
            cacheKey: String = "deskling.ownedProducts", logger: Logger = Logger(subsystem: "deskling", category: "store")
        ) {
            self.catalog = catalog
            self.backend = backend ?? StoreKitBackend(logger: logger)
            self.defaults = defaults
            self.cacheKey = cacheKey
            self.logger = logger
            // Last known state, so the UI doesn't flash at launch.
            owned = Set(defaults.stringArray(forKey: cacheKey) ?? [])
        }

        public func isOwned(_ id: String) -> Bool { owned.contains(id) }

        public var isBusy: Bool { purchasing != nil || isRestoring }

        public func start() async {
            guard !started else { return }
            started = true
            backend.observeTransactions { [weak self] update in
                guard let self else { return }
                if !update.isRevoked, self.catalog.isTip(update.productID) {
                    self.isPending = false
                    self.justTipped = true
                }
                Task { await self.refreshEntitlements() }
            }
            await refreshEntitlements()
            await loadProducts()
        }

        public func loadProducts() async {
            guard loadState != .loading else { return }
            loadState = .loading
            let ids = catalog.all
            let loaded: [StoreProduct]
            do {
                loaded = try await backend.loadProducts(ids)
            } catch {
                logger.error("Loading products failed: \(String(describing: error), privacy: .public)")
                loadState = .failed
                return
            }
            let missing = Set(ids).subtracting(loaded.map(\.id)).sorted().joined(separator: ", ")
            logger.notice(
                "Loaded \(loaded.count, privacy: .public)/\(ids.count, privacy: .public) products; missing: \(missing, privacy: .public)")
            products = Dictionary(loaded.map { ($0.id, $0) }, uniquingKeysWith: { a, _ in a })
            let complete = catalog.unlockables.isEmpty ? !products.isEmpty : catalog.unlockables.allSatisfy { products[$0] != nil }
            loadState = complete ? .loaded : .failed
        }

        /// Retries when a purchase screen appears, in case loading at launch came back empty.
        public func loadProductsIfNeeded() async {
            guard loadState != .loaded, loadState != .loading else { return }
            await loadProducts()
        }

        public func refreshEntitlements() async {
            let entitled = await backend.entitledProductIDs().intersection(catalog.unlockables)
            logger.notice("Owned: \(entitled.sorted().joined(separator: ", "), privacy: .public)")
            if !entitled.subtracting(owned).isEmpty { isPending = false }
            if entitled != owned { owned = entitled }
        }

        public func price(_ id: String) -> String? { products[id]?.displayPrice }

        /// Buys an unlockable; `owned` updates once the App Store confirms it.
        public func buy(_ id: String) async { await purchase(id) }

        /// Buys a tip: `justTipped` is set and nothing is unlocked.
        public func tip(_ id: String) async { await purchase(id) }

        public func restore() async {
            guard !isBusy else { return }
            isRestoring = true
            error = nil
            defer { isRestoring = false }
            do {
                try await backend.restore()
            } catch {
                logger.error("Restore failed: \(String(describing: error), privacy: .public)")
                self.error = .restoreFailed
            }
            await refreshEntitlements()
        }

        public func acknowledgeTip() {
            if justTipped { justTipped = false }
        }

        private func purchase(_ id: String) async {
            guard !isBusy else { return }
            purchasing = id
            error = nil
            isPending = false
            defer { purchasing = nil }
            do {
                switch try await backend.purchase(id) {
                case .purchased:
                    if catalog.isTip(id) { justTipped = true }
                    await refreshEntitlements()
                case .pending:
                    isPending = true
                case .cancelled:
                    break
                }
            } catch {
                logger.error("Purchase of \(id, privacy: .public) failed: \(String(describing: error), privacy: .public)")
                self.error = .purchaseFailed
            }
        }
    }
#endif
