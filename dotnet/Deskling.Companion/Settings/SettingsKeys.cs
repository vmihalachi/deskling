namespace Deskling.Companion.Settings;

/// <summary>
/// The storage key of every generic companion preference. Persisted on users' machines and listed in settings.json:
/// never rename one (a new key must keep the old one readable). An app adds its own keys next to these, in its
/// <see cref="CompanionSettings"/> subclass. Retired, never to be reused: <c>sayShortcut</c> (the global shortcut, until 0.9).
/// </summary>
public static class SettingsKeys
{
    public const string Channels = "channels";
    public const string Frequency = "frequency";
    public const string ActiveStartMinute = "activeStartMinute";
    public const string ActiveEndMinute = "activeEndMinute";
    public const string ActiveWeekdays = "activeWeekdays";
    public const string QuietHours = "quietHours";
    public const string HushInFullScreen = "hushInFullScreen";
    public const string GreetOnReturn = "greetOnReturn";
    public const string CommentOnPower = "commentOnPower";
    public const string CommentOnAppearance = "commentOnAppearance";
    public const string VoiceIdentifier = "voiceIdentifier";
    public const string SpeechRate = "speechRate";
    public const string SpeechVolume = "speechVolume";
    public const string PhraseFlavors = "phraseFlavors";
    public const string MutedPhraseIDs = "mutedPhraseIDs";
    public const string Appearance = "appearance";
    public const string Skin = "skin";
    public const string ShushedUntil = "shushedUntil";
    public const string ShowOnDesktop = "showOnDesktop";
    public const string DesktopSize = "desktopSize";
    public const string DesktopPosition = "desktopPosition";
    public const string HasCompletedOnboarding = "hasCompletedOnboarding";

    /// <summary>Every generic key, in the order of settings.json.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Channels,
        Frequency,
        ActiveStartMinute,
        ActiveEndMinute,
        ActiveWeekdays,
        QuietHours,
        HushInFullScreen,
        GreetOnReturn,
        CommentOnPower,
        CommentOnAppearance,
        VoiceIdentifier,
        SpeechRate,
        SpeechVolume,
        PhraseFlavors,
        MutedPhraseIDs,
        Appearance,
        Skin,
        ShushedUntil,
        ShowOnDesktop,
        DesktopSize,
        DesktopPosition,
        HasCompletedOnboarding,
    ];

    /// <summary>The generic keys "Reset to defaults" clears. Onboarding stays done.</summary>
    public static IReadOnlyList<string> Resettable { get; } = All.Where(key => key != HasCompletedOnboarding).ToArray();
}
