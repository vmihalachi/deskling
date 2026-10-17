import Foundation

/// A localized string as a catalog key plus its arguments. Pure code returns these and the UI layer
/// localizes them, so conformance vectors never contain display text.
public struct LocalizedKey: Codable, Equatable, Sendable {
    public struct Argument: Codable, Equatable, Sendable {
        /// What an argument is, and so how the UI layer formats it. Encodes as its name (`"int"`,
        /// `"durationMinutes"`), the same strings the C# side reads.
        public enum Kind: String, Codable, Equatable, Sendable {
            /// A number, also the plural count.
            case int
            /// Minutes, formatted with the platform's abbreviated hours/minutes duration style,
            /// e.g. "34 min", "1 hr, 5 min".
            case durationMinutes
        }

        public var type: Kind
        public var value: Int

        public init(type: Kind, value: Int) {
            self.type = type
            self.value = value
        }
    }

    public var key: String
    public var args: [Argument]

    public init(_ key: String, _ args: [Argument] = []) {
        self.key = key
        self.args = args
    }
}
