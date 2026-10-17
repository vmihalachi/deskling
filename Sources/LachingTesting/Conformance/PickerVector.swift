import LachingCompanion
import Foundation

/// `picker/*.json`: a `PhraseContext`, the candidates it yields in bank order, and the id a few uniform draws pick.
/// Pins `PhrasePicker` against a bank; the app supplies the cases.
public enum PickerVector {
    public static let directory = "picker"

    public struct Context: Codable, Equatable, Sendable {
        public var hour: Int
        /// 1 = Sunday … 7 = Saturday.
        public var weekday: Int
        public var event: String?
        public var chimeEnabled: Bool
        public var recentIDs: [String]
        /// Sorted; empty means every flavor.
        public var flavors: [String]
        /// Sorted.
        public var excludedIDs: [String]
        /// The skin speaking.
        public var skin: String
        /// The app language; absent means "en".
        public var language: String?

        public init(_ c: PhraseContext) {
            hour = c.hour
            weekday = c.weekday
            event = c.event?.rawValue
            chimeEnabled = c.chimeEnabled
            recentIDs = c.recentIDs
            flavors = c.flavors.map(\.rawValue).sorted()
            excludedIDs = c.excludedIDs.sorted()
            skin = c.skin.rawValue
            language = c.language == "en" ? nil : c.language
        }

        public var context: PhraseContext {
            PhraseContext(
                hour: hour, weekday: weekday, event: event.flatMap(Tag.init(rawValue:)), chimeEnabled: chimeEnabled,
                recentIDs: recentIDs, flavors: Set(flavors.map(FlavorID.init(rawValue:))), excludedIDs: Set(excludedIDs),
                skin: SkinID(rawValue: skin), language: language ?? "en")
        }
    }

    public struct Draw: Codable, Equatable, Sendable {
        public var unit: Double
        public var id: String?
    }

    public struct Vector: Codable, Equatable, Sendable {
        public var name: String
        public var context: Context
        public var candidates: [String]
        public var draws: [Draw]
    }

    public struct Case: Sendable {
        public var name: String
        public var context: PhraseContext

        public init(name: String, context: PhraseContext) {
            self.name = name
            self.context = context
        }
    }

    /// The uniform draws every case makes.
    public static let units: [Double] = [0, 0.1, 0.25, 0.5, 0.75, 0.9, 0.999_999]

    public static func vector(_ c: Case, bank: PhraseBank) -> Vector {
        Vector(
            name: c.name, context: Context(c.context),
            candidates: PhrasePicker.candidates(in: bank, context: c.context).map(\.id),
            draws: units.map { Draw(unit: $0, id: PhrasePicker.pick(from: bank, context: c.context, unit: $0)?.id) })
    }

    public static func files(cases: [Case], bank: PhraseBank) throws -> [String: Data] {
        var files: [String: Data] = [:]
        for c in cases {
            files["\(directory)/\(c.name).json"] = try ConformanceSupport.encode(vector(c, bank: bank))
        }
        return files
    }

    public static func verify(_ data: Data, file: String, bank: PhraseBank) throws -> [String] {
        let vector = try ConformanceSupport.decode(Vector.self, from: data)
        let context = vector.context.context
        var problems: [String] = []
        let candidates = PhrasePicker.candidates(in: bank, context: context).map(\.id)
        if candidates != vector.candidates { problems.append("\(file): candidates \(candidates) != \(vector.candidates)") }
        for draw in vector.draws {
            let id = PhrasePicker.pick(from: bank, context: context, unit: draw.unit)?.id
            if id != draw.id { problems.append("\(file): unit \(draw.unit) picked \(id ?? "nil"), expected \(draw.id ?? "nil")") }
        }
        return problems
    }
}
