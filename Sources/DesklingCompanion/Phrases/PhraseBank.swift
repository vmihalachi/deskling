import Foundation

/// Everything the character can say, in a stable order: the app builds one from its own lines and injects it.
/// The order matters (the picker draws in bank order), and ids are permanent.
public struct PhraseBank: Equatable, Sendable {
    public let phrases: [Phrase]

    public init(_ phrases: [Phrase]) {
        self.phrases = phrases
    }

    public subscript(id: String) -> Phrase? {
        phrases.first { $0.id == id }
    }

    public func phrases(tagged tag: Tag) -> [Phrase] {
        phrases.filter { $0.tags.contains(tag) }
    }

    /// The phrases the character `skin` can say, in bank order.
    public func phrases(for skin: SkinID) -> [Phrase] {
        phrases.filter { $0.fits(skin) }
    }

    /// The phrases that exist in `language` (a BCP-47 code, compared by its base), in bank order.
    public func phrases(in language: String) -> [Phrase] {
        phrases.filter { $0.fits(language: language) }
    }
}
