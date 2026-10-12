import Foundation

/// How often the character pipes up when left alone, from every 15 minutes to every 3 hours. Declared from the most
/// frequent to the least, the order a picker lists them in. Raw values are stored in settings; never rename one (the
/// first three presets keep their original names).
public enum Frequency: String, CaseIterable, Codable, Sendable {
    /// Every 15 minutes or so.
    case chatty
    /// Every 30 minutes or so.
    case sometimes
    /// Every 45 minutes or so.
    case every45Minutes
    /// Every hour or so: the default.
    case rarely
    /// Every 90 minutes or so.
    case every90Minutes
    /// Every 2 hours or so.
    case every2Hours
    /// Every 3 hours or so. Being away restarts the interval, so on most days this means once or twice.
    case every3Hours

    /// The base interval between lines.
    public var interval: TimeInterval {
        switch self {
        case .chatty: 15 * 60
        case .sometimes: 30 * 60
        case .every45Minutes: 45 * 60
        case .rarely: 60 * 60
        case .every90Minutes: 90 * 60
        case .every2Hours: 120 * 60
        case .every3Hours: 180 * 60
        }
    }

    /// Each interval is offset by a random amount within ±jitter, so the character never feels like a clock.
    public var jitter: TimeInterval {
        switch self {
        case .chatty: 5 * 60
        case .sometimes: 10 * 60
        case .every45Minutes: 10 * 60
        case .rarely: 15 * 60
        case .every90Minutes: 20 * 60
        case .every2Hours: 30 * 60
        case .every3Hours: 45 * 60
        }
    }
}
