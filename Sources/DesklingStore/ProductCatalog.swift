#if os(macOS)
    import Foundation

    /// The app's in-app purchases by product id, as set up in App Store Connect: non-consumable
    /// `unlockables` the user owns once bought, and consumable `tips` that only say thanks. Nothing else is
    /// needed; the store never guesses ids.
    public struct ProductCatalog: Equatable, Sendable {
        public var unlockables: [String]
        public var tips: [String]

        public init(unlockables: [String], tips: [String] = []) {
            self.unlockables = unlockables
            self.tips = tips
        }

        /// Every product, unlockables first.
        public var all: [String] { unlockables + tips }

        public func isUnlockable(_ id: String) -> Bool { unlockables.contains(id) }

        public func isTip(_ id: String) -> Bool { tips.contains(id) }
    }
#endif
