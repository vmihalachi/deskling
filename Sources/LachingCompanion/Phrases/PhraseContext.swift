import Foundation

/// Everything the picker needs to know about the moment, as a value, so a pick is a pure function. The skin and
/// the language have no defaults: they are the app's.
public struct PhraseContext: Equatable, Sendable {
    /// Local hour of the day, 0–23.
    public var hour: Int
    /// Calendar weekday, 1 = Sunday … 7 = Saturday.
    public var weekday: Int
    /// What just happened, or nil for ambient chatter.
    public var event: Tag?
    /// Tunes play only when the chime channel is on.
    public var chimeEnabled: Bool
    /// The ids said most recently, oldest first.
    public var recentIDs: [String]
    /// The flavors the user keeps on; empty means all. Tunes have no flavor and are never filtered by it.
    public var flavors: Set<FlavorID>
    /// Ids never to pick: lines the user muted, and lines pending a recording in the voice in use.
    public var excludedIDs: Set<String>
    /// The character speaking: lines it can't say are never picked.
    public var skin: SkinID
    /// The app's language (BCP-47): phrases that don't exist in it are never picked.
    public var language: String

    public init(
        hour: Int, weekday: Int, event: Tag? = nil, chimeEnabled: Bool, recentIDs: [String] = [],
        flavors: Set<FlavorID> = [], excludedIDs: Set<String> = [], skin: SkinID, language: String
    ) {
        self.hour = hour
        self.weekday = weekday
        self.event = event
        self.chimeEnabled = chimeEnabled
        self.recentIDs = recentIDs
        self.flavors = flavors
        self.excludedIDs = excludedIDs
        self.skin = skin
        self.language = language
    }
}
