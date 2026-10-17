import Foundation

/// Why the character is about to say something.
public enum Trigger: String, CaseIterable, Codable, Sendable {
    /// The chatter interval came up.
    case due
    /// The user pressed Say something.
    case buttonPress
    /// A line held during a call is released (one held for full screen comes back as `due`).
    case afterCall
    case welcomeBack
    case shushEnd
    case batteryLow, charging, unplugged
    case wentDark, wentLight

    /// The phrase tag the picker should prefer, or nil for ambient chatter.
    public var tag: Tag? {
        switch self {
        case .due: nil
        case .buttonPress: .buttonPress
        case .afterCall: .afterCall
        case .welcomeBack: .welcomeBack
        case .shushEnd: .shushEnd
        case .batteryLow: .batteryLow
        case .charging: .charging
        case .unplugged: .unplugged
        case .wentDark: .wentDark
        case .wentLight: .wentLight
        }
    }
}

/// How a line is delivered. The panel's speech bubble always shows it.
public struct ChannelPlan: Equatable, Sendable {
    public var speak: Bool
    public var chime: Bool
    public var notify: Bool

    public init(speak: Bool, chime: Bool, notify: Bool) {
        self.speak = speak
        self.chime = chime
        self.notify = notify
    }

    public static let bubbleOnly = ChannelPlan(speak: false, chime: false, notify: false)
}

/// Why a line was not delivered.
public enum SkipReason: String, Codable, Sendable {
    /// A call or a full-screen app.
    case busy
    case away
    case locked
    /// A line is being spoken; two never overlap (a button press is delivered, and the shell cuts the line).
    case speaking
    /// The user turned this kind of comment off.
    case disabled
}

public enum Outcome: Equatable, Sendable {
    case skip(SkipReason)
    case deliver(ChannelPlan)
}

/// The settings the rules consult.
public struct Manners: Equatable, Sendable {
    public var channels: Channels
    public var greetOnReturn: Bool
    public var commentOnPower: Bool
    public var commentOnAppearance: Bool

    public init(channels: Channels, greetOnReturn: Bool = true, commentOnPower: Bool = true, commentOnAppearance: Bool = true) {
        self.channels = channels
        self.greetOnReturn = greetOnReturn
        self.commentOnPower = commentOnPower
        self.commentOnAppearance = commentOnAppearance
    }
}

/// Decides whether a line may be delivered right now and how. Pure; `conformance/companion/quiet` pins it.
///
/// In order: a comment the user turned off is skipped; nothing starts while a line is being spoken, except a
/// button press: the user asking now (Say something, a click on the character, a notification's Say more, which
/// comes while its line is still heard) is delivered, and the shell cuts the line being said first, as Shh does, so
/// two still never overlap and nothing waits in a queue; nothing is said to a locked screen; nothing is said to
/// an empty room (idle past the threshold) except the welcome back itself; a call or full-screen app holds
/// everything except a button press, which still shows in the panel but neither speaks nor chimes. Then the
/// channels: voice and chime only when the output isn't muted; a notification when that channel is on, or as the
/// fallback when voice or chime would have played but the output is muted and notifications are allowed. A button
/// press never notifies.
public enum QuietRules {
    public static let awayThreshold: TimeInterval = ChatterCadence.idleThreshold

    public static func decide(trigger: Trigger, snapshot: Snapshot, manners: Manners) -> Outcome {
        switch trigger {
        case .welcomeBack where !manners.greetOnReturn: return .skip(.disabled)
        case .batteryLow, .charging, .unplugged: if !manners.commentOnPower { return .skip(.disabled) }
        case .wentDark, .wentLight: if !manners.commentOnAppearance { return .skip(.disabled) }
        default: break
        }
        let isButton = trigger == .buttonPress
        if snapshot.isSpeaking && !isButton { return .skip(.speaking) }
        if snapshot.isScreenLocked { return .skip(.locked) }
        if snapshot.idleSeconds >= awayThreshold && trigger != .welcomeBack && !isButton { return .skip(.away) }
        let busy = !snapshot.busy.isEmpty
        if busy && !isButton { return .skip(.busy) }

        let muted = snapshot.isOutputMuted == true
        let audible = !muted && !busy
        let speak = manners.channels.contains(.voice) && audible
        let chime = manners.channels.contains(.chime) && audible
        let wantedSound = manners.channels.contains(.voice) || manners.channels.contains(.chime)
        let fallback = muted && wantedSound && snapshot.notificationsAuthorized
        let notify = !isButton && (manners.channels.contains(.notification) || fallback)
        return .deliver(ChannelPlan(speak: speak, chime: chime, notify: notify))
    }
}
