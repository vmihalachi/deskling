import Foundation

/// How often the character pipes up when left alone. Raw values are stored in settings; never rename one.
public enum Frequency: String, CaseIterable, Codable, Sendable {
    /// About once an hour.
    case rarely
    /// About twice an hour: the default.
    case sometimes
    /// About four times an hour.
    case chatty

    /// The base interval between lines.
    public var interval: TimeInterval {
        switch self {
        case .rarely: 60 * 60
        case .sometimes: 30 * 60
        case .chatty: 15 * 60
        }
    }

    /// Each interval is offset by a random amount within ±jitter, so the character never feels like a clock.
    public var jitter: TimeInterval {
        switch self {
        case .rarely: 15 * 60
        case .sometimes: 10 * 60
        case .chatty: 5 * 60
        }
    }
}
