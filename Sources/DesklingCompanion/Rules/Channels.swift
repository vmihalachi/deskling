import Foundation

/// How the character delivers a line: any combination of speaking it, playing a chime and showing a notification.
/// Empty means "quietly": the line appears only in the panel. Names are stored in settings; never rename one.
public struct Channels: OptionSet, Hashable, Sendable {
    public let rawValue: Int

    public init(rawValue: Int) {
        self.rawValue = rawValue
    }

    public static let voice = Channels(rawValue: 1 << 0)
    public static let chime = Channels(rawValue: 1 << 1)
    public static let notification = Channels(rawValue: 1 << 2)
    public static let quiet: Channels = []
    public static let all: Channels = [.voice, .chime, .notification]

    private static let named: [(Channels, String)] = [(.voice, "voice"), (.chime, "chime"), (.notification, "notification")]

    /// The stable names, in a fixed order, for storage and vectors.
    public var names: [String] {
        Self.named.filter { contains($0.0) }.map(\.1)
    }

    /// Unknown names are ignored, so a newer setting reads on an older build.
    public init(names: [String]) {
        var set = Channels.quiet
        for (channel, name) in Self.named where names.contains(name) {
            set.insert(channel)
        }
        self = set
    }
}
