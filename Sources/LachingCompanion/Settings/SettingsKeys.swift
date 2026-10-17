import Foundation

/// The storage key of every generic companion preference. Persisted on users' machines and listed in
/// `settings.json`: never rename one (a new key must keep the old one readable). An app adds its own keys next to
/// these, in its `CompanionSettings` subclass. Retired, never to be reused: `sayShortcut` (the global shortcut, until 0.9).
public enum SettingsKeys {
    public static let channels = "channels"
    public static let frequency = "frequency"
    public static let activeStartMinute = "activeStartMinute"
    public static let activeEndMinute = "activeEndMinute"
    public static let activeWeekdays = "activeWeekdays"
    public static let quietHours = "quietHours"
    public static let hushInFullScreen = "hushInFullScreen"
    public static let greetOnReturn = "greetOnReturn"
    public static let commentOnPower = "commentOnPower"
    public static let commentOnAppearance = "commentOnAppearance"
    public static let voiceIdentifier = "voiceIdentifier"
    public static let speechRate = "speechRate"
    public static let speechVolume = "speechVolume"
    public static let phraseFlavors = "phraseFlavors"
    public static let mutedPhraseIDs = "mutedPhraseIDs"
    public static let appearance = "appearance"
    public static let skin = "skin"
    public static let shushedUntil = "shushedUntil"
    public static let showOnDesktop = "showOnDesktop"
    public static let desktopSize = "desktopSize"
    public static let desktopPosition = "desktopPosition"
    public static let hasCompletedOnboarding = "hasCompletedOnboarding"

    /// Every generic key, in the order of `settings.json`.
    public static let all: [String] = [
        channels, frequency, activeStartMinute, activeEndMinute, activeWeekdays, quietHours, hushInFullScreen,
        greetOnReturn, commentOnPower, commentOnAppearance, voiceIdentifier, speechRate, speechVolume,
        phraseFlavors, mutedPhraseIDs, appearance, skin, shushedUntil, showOnDesktop, desktopSize, desktopPosition, hasCompletedOnboarding,
    ]

    /// The generic keys "Reset to defaults" clears. Onboarding stays done.
    public static let resettable: [String] = all.filter { $0 != hasCompletedOnboarding }
}
