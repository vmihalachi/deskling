import Foundation
import LachingCore

/// Every generic preference's value on first launch. The package's numbers are the defaults; the two product
/// choices (the default skin, the flavors on) have none, so every app states them.
/// Adjust any other value after `init`. Pinned by the app's `settings.json`, which its Windows port loads.
public struct CompanionDefaults: Equatable, Sendable {
    /// Speak out loud; no notification until the user turns that channel on, so first launch asks for nothing.
    public var channels: Channels = [.voice]
    /// Every hour or so: the character also speaks on its occasions (a greeting, a comment), so more is a lot.
    public var frequency: Frequency = .rarely
    public var activeStartMinute = 9 * 60
    public var activeEndMinute = 18 * 60
    /// Monday to Friday (1 = Sunday … 7 = Saturday).
    public var activeWeekdays: Set<Int> = [2, 3, 4, 5, 6]
    public var quietHours: Set<Int> = []
    public var hushInFullScreen = true
    public var greetOnReturn = true
    public var commentOnPower = true
    public var commentOnAppearance = true
    /// nil: the system's default voice for the app language.
    public var voiceIdentifier: String? = nil
    /// The platform's normal speaking rate, on a 0…1 scale.
    public var speechRate = 0.5
    public var speechVolume = 0.8
    /// The flavors on at first launch; empty would mean every flavor.
    public var phraseFlavors: Set<FlavorID>
    /// No line muted ("Don't say that again").
    public var mutedPhraseIDs: Set<String> = []
    public var appearance: AppearanceSetting = .system
    public var skin: SkinID
    public var shushedUntil: Date? = nil
    /// The character stays in the menu bar until the user puts it on the desktop.
    public var showOnDesktop = false
    public var desktopSize: DesktopSize = .medium
    /// nil: the bottom right of the main screen.
    public var desktopPosition: DesktopPosition? = nil
    public var hasCompletedOnboarding = false

    public init(skin: SkinID, phraseFlavors: Set<FlavorID>) {
        self.skin = skin
        self.phraseFlavors = phraseFlavors
    }
}
