import Foundation

/// A localized string as a catalog key plus its arguments. Pure code returns these and the UI layer
/// localizes them, so conformance vectors never contain display text.
public struct LocalizedKey: Codable, Equatable, Sendable {
    public struct Argument: Codable, Equatable, Sendable {
        /// "int" (a number, also the plural count), or "durationMinutes" (format with the platform's
        /// abbreviated hours/minutes duration style, e.g. "34 min", "1 hr, 5 min").
        public var type: String
        public var value: Int

        public init(type: String, value: Int) {
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
