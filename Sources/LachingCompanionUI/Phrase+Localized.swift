#if os(macOS)
    import Foundation
    import LachingCompanion

    extension Phrase {
        /// The phrase in the user's language, one string per part (a setup and its punchline, or a single line): the
        /// String Catalog entries for `catalogKeys` (`phrase.<id>`, `.1`, `.2`) in `bundle`, with the bank's source as the
        /// fallback. Empty for a tune.
        public func localizedParts(bundle: Bundle = .main) -> [String] {
            switch kind {
            case .line(let source): [Self.localize(catalogKey, source, bundle: bundle)]
            case .sequence(let sources): zip(catalogKeys, sources).map { Self.localize($0, $1, bundle: bundle) }
            case .tune: []
            }
        }

        /// The whole phrase on one line (both parts of a joke), from the app's String Catalog; nil for a tune.
        public var localizedText: String? {
            let parts = localizedParts()
            return parts.isEmpty ? nil : parts.joined(separator: " ")
        }

        /// The String Catalog's text for `key` in `bundle`, `source` when it has none.
        public static func localize(_ key: String, _ source: String, bundle: Bundle = .main) -> String {
            bundle.localizedString(forKey: key, value: source, table: nil)
        }
    }
#endif
