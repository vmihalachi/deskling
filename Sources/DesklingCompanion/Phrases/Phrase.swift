import Foundation

/// What a line does for the listener's mood; the app names its own flavors and lets the user turn them off. Raw
/// values are public: they appear in `phrases.json`, the vectors and the settings. Never rename one.
public struct FlavorID: RawRepresentable, Hashable, Codable, Sendable, ExpressibleByStringLiteral {
    public let rawValue: String

    public init(rawValue: String) {
        self.rawValue = rawValue
    }

    public init(stringLiteral value: String) {
        self.init(rawValue: value)
    }
}

/// Which character the user picked; the app names its own skins. Raw values are stored and name the skin's art;
/// never rename one.
public struct SkinID: RawRepresentable, Hashable, Codable, Sendable, ExpressibleByStringLiteral {
    public let rawValue: String

    public init(rawValue: String) {
        self.rawValue = rawValue
    }

    public init(stringLiteral value: String) {
        self.init(rawValue: value)
    }
}

/// One thing the character can say: a spoken line (a String Catalog key, resolved by the app), a joke told in
/// beats (one key per part) or a hummed tune (a sound file name). `id` is stable and public: it is persisted in
/// the recent list and the muted list, written to `phrases.json` and used in the catalog keys. Never rename or
/// reuse one. Recents, muting and the quiet rules are keyed on the id, so a joke counts as one phrase.
public struct Phrase: Equatable, Sendable, Identifiable {
    public enum Kind: Equatable, Sendable {
        /// `source` is the catalog's source text for the key `phrase.<id>`; the app displays the catalog's
        /// translation, never this string directly.
        case line(source: String)
        /// A short original tune in the app's sound files, played only when the chime channel is on.
        case tune(sound: String)
        /// A joke in beats, at least two: a setup and a punchline. Part *n* is the catalog key `phrase.<id>.<n>`,
        /// counted from 1.
        case sequence(source: [String])
    }

    public let id: String
    public let kind: Kind
    public let tags: Set<Tag>
    /// 1 (rare) … 5 (a favorite); 3 is normal. Doubled when a daypart tag matches the moment.
    public let weight: Int
    /// Lines and sequences have one; tunes have none.
    public let flavor: FlavorID?
    /// The characters that can say it; empty means every character.
    public let skins: Set<SkinID>
    /// The languages it exists in, as BCP-47 base codes ("it"); empty means every language. A joke that only works
    /// in Italian is written in Italian and listed for `["it"]` only.
    public let languages: Set<String>

    /// A `.sequence` needs at least two parts.
    public init(
        _ id: String, _ kind: Kind, tags: Set<Tag>, weight: Int = 3, flavor: FlavorID? = nil, skins: Set<SkinID> = [],
        languages: Set<String> = []
    ) {
        if case .sequence(let parts) = kind {
            precondition(parts.count >= 2, "Phrase \(id): a sequence needs at least two parts")
        }
        self.id = id
        self.kind = kind
        self.tags = tags
        self.weight = weight
        self.flavor = flavor
        self.skins = skins
        self.languages = languages
    }

    /// How many beats it is delivered in: the parts of a sequence, otherwise 1.
    public var partCount: Int {
        if case .sequence(let parts) = kind { return parts.count }
        return 1
    }

    /// The String Catalog key of a spoken line; the stem of a sequence's part keys.
    public var catalogKey: String { "phrase.\(id)" }

    /// The catalog keys whose text is spoken, in order: `phrase.<id>` for a line, `phrase.<id>.1`, `.2`, … for a
    /// sequence, none for a tune.
    public var catalogKeys: [String] {
        switch kind {
        case .line: [catalogKey]
        case .tune: []
        case .sequence(let parts): (1...parts.count).map { "\(catalogKey).\($0)" }
        }
    }

    /// The ids of its recorded clips, in order: `<id>` for a line, `<id>.1`, `<id>.2`, … for a sequence, none for a tune.
    public var clipIDs: [String] {
        switch kind {
        case .line: [id]
        case .tune: []
        case .sequence(let parts): (1...parts.count).map { "\(id).\($0)" }
        }
    }

    public var isTune: Bool {
        if case .tune = kind { return true }
        return false
    }

    public var hasEventTag: Bool { !tags.isDisjoint(with: Tag.events) }

    /// Whether the character `skin` can say it.
    public func fits(_ skin: SkinID) -> Bool { skins.isEmpty || skins.contains(skin) }

    /// Whether it exists in `language`, a BCP-47 code compared by its base ("pt-BR" fits `["pt"]`).
    public func fits(language: String) -> Bool {
        languages.isEmpty || languages.contains(Self.baseLanguage(language))
    }

    /// The base of a BCP-47 code, lowercased: "pt-BR" and "pt_BR" → "pt".
    public static func baseLanguage(_ code: String) -> String {
        String(code.prefix { $0 != "-" && $0 != "_" }).lowercased()
    }
}
