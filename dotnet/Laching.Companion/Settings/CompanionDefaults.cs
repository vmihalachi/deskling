using Laching.Companion.Cadence;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;

namespace Laching.Companion.Settings;

/// <summary>
/// Every generic preference's value on first launch. The package's numbers are the defaults; the two product
/// choices (the default skin, the flavors on) have none, so every app states them.
/// Adjust any other value with an object initializer or <c>with</c>. Pinned by the app's settings.json.
/// </summary>
/// <param name="Skin">The skin on first launch.</param>
/// <param name="PhraseFlavors">The flavors on at first launch; empty would mean every flavor.</param>
public sealed record CompanionDefaults(SkinId Skin, IReadOnlySet<FlavorId> PhraseFlavors)
{
    /// <summary>Speak out loud; no notification until the user turns that channel on, so first launch asks for nothing.</summary>
    public Channels Channels { get; init; } = Channels.Voice;

    /// <summary>Every hour or so: the character also speaks on its occasions (a greeting, a comment), so more is a lot.</summary>
    public Frequency Frequency { get; init; } = Frequency.Rarely;

    public int ActiveStartMinute { get; init; } = 9 * 60;

    public int ActiveEndMinute { get; init; } = 18 * 60;

    /// <summary>Monday to Friday (1 = Sunday … 7 = Saturday).</summary>
    public IReadOnlySet<int> ActiveWeekdays { get; init; } = new HashSet<int> { 2, 3, 4, 5, 6 };

    public IReadOnlySet<int> QuietHours { get; init; } = new HashSet<int>();

    public bool HushInFullScreen { get; init; } = true;

    public bool GreetOnReturn { get; init; } = true;

    public bool CommentOnPower { get; init; } = true;

    public bool CommentOnAppearance { get; init; } = true;

    /// <summary>null: the system's default voice for the app language.</summary>
    public string? VoiceIdentifier { get; init; }

    /// <summary>The platform's normal speaking rate, on a 0…1 scale.</summary>
    public double SpeechRate { get; init; } = 0.5;

    public double SpeechVolume { get; init; } = 0.8;

    /// <summary>No line muted ("Don't say that again").</summary>
    public IReadOnlySet<string> MutedPhraseIDs { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public AppearanceSetting Appearance { get; init; } = AppearanceSetting.System;

    public DateTimeOffset? ShushedUntil { get; init; }

    /// <summary>The character stays in the menu bar or notification area until the user puts it on the desktop.</summary>
    public bool ShowOnDesktop { get; init; }

    public DesktopSize DesktopSize { get; init; } = DesktopSize.Medium;

    /// <summary>null: the bottom right of the main screen.</summary>
    public DesktopPosition? DesktopPosition { get; init; }

    public bool HasCompletedOnboarding { get; init; }
}
