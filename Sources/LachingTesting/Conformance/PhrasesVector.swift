import Foundation
import LachingCompanion

/// `phrases.json`: a phrase bank as data. The Windows bank loads it; the localization tool syncs its source lines into
/// the String Catalog. Lines carry `key` and `english` (the catalog's source text, whatever its language: the field
/// keeps sunnysays' name), tunes `sound`, sequences `keys` and `parts`; `languages` is written only for a phrase
/// scoped to some languages.
public enum PhrasesVector {
    public static let file = "phrases.json"

    public struct Entry: Codable, Equatable, Sendable {
        public var id: String
        /// "line", "tune" or "sequence".
        public var kind: String
        /// The String Catalog key (lines only).
        public var key: String?
        /// The catalog's source text (lines only). Apps display the translation, never this.
        public var english: String?
        /// One catalog key per part (sequences only).
        public var keys: [String]?
        /// The source text of each part (sequences only).
        public var parts: [String]?
        /// The sound file name, without extension (tunes only).
        public var sound: String?
        public var tags: [String]
        public var weight: Int
        public var flavor: String?
        /// The skins that can say it, sorted; empty means every skin.
        public var skins: [String]
        /// The languages it exists in, sorted; absent means every language.
        public var languages: [String]?

        public init(_ phrase: Phrase) {
            id = phrase.id
            tags = phrase.tags.map(\.rawValue).sorted()
            weight = phrase.weight
            flavor = phrase.flavor?.rawValue
            skins = phrase.skins.map(\.rawValue).sorted()
            languages = phrase.languages.isEmpty ? nil : phrase.languages.sorted()
            switch phrase.kind {
            case .line(let source):
                kind = "line"
                key = phrase.catalogKey
                english = source
            case .tune(let sound):
                kind = "tune"
                self.sound = sound
            case .sequence(let source):
                kind = "sequence"
                keys = phrase.catalogKeys
                parts = source
            }
        }
    }

    public struct Vector: Codable, Equatable, Sendable {
        public var phrases: [Entry]
        /// Every tag, in declaration order.
        public var tags: [String]
        /// The app's flavors, in its order.
        public var flavors: [String]
        public var eventTags: [String]
        /// Daypart tag → [firstHour, lastHour] inclusive.
        public var dayparts: [String: [Int]]
        /// Weekday tag → the calendar weekdays (1 = Sunday … 7 = Saturday) it covers.
        public var weekdays: [String: [Int]]
        public var recentWindow: Int
    }

    /// `flavors` is the app's list of flavors in its display order.
    public static func vector(bank: PhraseBank, flavors: [FlavorID]) -> Vector {
        var dayparts: [String: [Int]] = [:]
        for tag in Tag.dayparts {
            let hours = (0..<24).filter { Tag.daypart(hour: $0) == tag }
            dayparts[tag.rawValue] = [hours.min()!, hours.max()!]
        }
        var weekdays: [String: [Int]] = [:]
        for tag in Tag.weekdays {
            weekdays[tag.rawValue] = (1...7).filter { Tag.weekdayTag(weekday: $0) == tag }
        }
        return Vector(
            phrases: bank.phrases.map(Entry.init), tags: Tag.allCases.map(\.rawValue), flavors: flavors.map(\.rawValue),
            eventTags: Tag.events.map(\.rawValue).sorted(), dayparts: dayparts, weekdays: weekdays,
            recentWindow: PhrasePicker.recentWindow)
    }

    public static func files(bank: PhraseBank, flavors: [FlavorID]) throws -> [String: Data] {
        [file: try ConformanceSupport.encode(vector(bank: bank, flavors: flavors))]
    }

    /// Replays a committed file: every entry must match the bank's phrase of that id, and nothing may be missing.
    public static func verify(_ data: Data, file: String, bank: PhraseBank, flavors: [FlavorID]) throws -> [String] {
        let committed = try ConformanceSupport.decode(Vector.self, from: data)
        let current = vector(bank: bank, flavors: flavors)
        var problems: [String] = []
        if committed.phrases.count != current.phrases.count {
            problems.append("\(file): \(committed.phrases.count) phrases, the bank has \(current.phrases.count)")
        }
        for (expected, actual) in zip(committed.phrases, current.phrases) where expected != actual {
            problems.append("\(file): \(expected.id) differs from the bank")
        }
        var rest = committed
        rest.phrases = current.phrases
        if rest != current { problems.append("\(file): the tags, flavors or tables differ") }
        return problems
    }
}
