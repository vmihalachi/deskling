using System.ComponentModel;
using System.Runtime.CompilerServices;
using Deskling.Companion.Cadence;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Core.Settings;

namespace Deskling.Companion.Settings;

/// <summary>
/// Every generic companion preference, observable (<see cref="INotifyPropertyChanged"/> in place of Swift's
/// <c>@Observable</c>), written through <see cref="Store"/> under <see cref="SettingsKeys"/> as soon as it is set (even
/// to the same value, as Swift's <c>didSet</c>). First-launch values come from <see cref="Defaults"/>; a missing or
/// unreadable value falls back to the default. Reading writes nothing. Never touches the network; where values live is
/// the store's business. Not thread-safe: use it from one thread (the UI one, in an app).
/// <para>
/// An app subclasses it to add its own preferences, under keys of its own, with the same write-through pattern:
/// </para>
/// <code>
/// public sealed class AppSettings : CompanionSettings
/// {
///     private string? voice;
///
///     public AppSettings(ISettingsStore store, CompanionDefaults defaults) : base(store, defaults) =>
///         voice = store.GetString("voice");
///
///     public string? Voice
///     {
///         get => voice;
///         set => SetProperty(ref voice, value, () => SetOrRemove("voice", value));
///     }
///
///     public override void ResetToDefaults()
///     {
///         base.ResetToDefaults();
///         Voice = null;
///     }
/// }
/// </code>
/// </summary>
public class CompanionSettings : INotifyPropertyChanged
{
    private Channels channels;
    private Frequency frequency;
    private int activeStartMinute;
    private int activeEndMinute;
    private IReadOnlySet<int> activeWeekdays;
    private IReadOnlySet<int> quietHours;
    private bool hushInFullScreen;
    private bool greetOnReturn;
    private bool commentOnPower;
    private bool commentOnAppearance;
    private string? voiceIdentifier;
    private double speechRate;
    private double speechVolume;
    private IReadOnlySet<FlavorId> phraseFlavors;
    private IReadOnlySet<string> mutedPhraseIDs;
    private AppearanceSetting appearance;
    private SkinId skin;
    private DateTimeOffset? shushedUntil;
    private bool showOnDesktop;
    private DesktopSize desktopSize;
    private DesktopPosition? desktopPosition;
    private bool hasCompletedOnboarding;

    public CompanionSettings(ISettingsStore store, CompanionDefaults defaults)
    {
        Store = store;
        Defaults = defaults;
        channels = store.GetStrings(SettingsKeys.Channels) is { } names ? Channels.FromNames(names) : defaults.Channels;
        frequency = Frequencies.Parse(store.GetString(SettingsKeys.Frequency)) ?? defaults.Frequency;
        activeStartMinute = store.GetInteger(SettingsKeys.ActiveStartMinute) ?? defaults.ActiveStartMinute;
        activeEndMinute = store.GetInteger(SettingsKeys.ActiveEndMinute) ?? defaults.ActiveEndMinute;
        activeWeekdays = store.GetIntegers(SettingsKeys.ActiveWeekdays) is { } days ? days.ToHashSet() : defaults.ActiveWeekdays;
        quietHours = store.GetIntegers(SettingsKeys.QuietHours) is { } hours ? hours.ToHashSet() : defaults.QuietHours;
        hushInFullScreen = store.GetBool(SettingsKeys.HushInFullScreen) ?? defaults.HushInFullScreen;
        greetOnReturn = store.GetBool(SettingsKeys.GreetOnReturn) ?? defaults.GreetOnReturn;
        commentOnPower = store.GetBool(SettingsKeys.CommentOnPower) ?? defaults.CommentOnPower;
        commentOnAppearance = store.GetBool(SettingsKeys.CommentOnAppearance) ?? defaults.CommentOnAppearance;
        voiceIdentifier = store.GetString(SettingsKeys.VoiceIdentifier) ?? defaults.VoiceIdentifier;
        speechRate = store.GetDouble(SettingsKeys.SpeechRate) ?? defaults.SpeechRate;
        speechVolume = store.GetDouble(SettingsKeys.SpeechVolume) ?? defaults.SpeechVolume;
        phraseFlavors = store.GetStrings(SettingsKeys.PhraseFlavors) is { } flavors
            ? flavors.Select(name => new FlavorId(name)).ToHashSet()
            : defaults.PhraseFlavors;
        mutedPhraseIDs = store.GetStrings(SettingsKeys.MutedPhraseIDs) is { } muted
            ? muted.ToHashSet(StringComparer.Ordinal)
            : defaults.MutedPhraseIDs;
        appearance = Appearances.Parse(store.GetString(SettingsKeys.Appearance)) ?? defaults.Appearance;
        skin = store.GetString(SettingsKeys.Skin) is { Length: > 0 } skinName ? new SkinId(skinName) : defaults.Skin;
        shushedUntil = store.GetDouble(SettingsKeys.ShushedUntil) is { } seconds ? DateTimeOffset.UnixEpoch.AddSeconds(seconds) : defaults.ShushedUntil;
        showOnDesktop = store.GetBool(SettingsKeys.ShowOnDesktop) ?? defaults.ShowOnDesktop;
        desktopSize = DesktopSizes.Parse(store.GetString(SettingsKeys.DesktopSize)) ?? defaults.DesktopSize;
        desktopPosition = store.GetIntegers(SettingsKeys.DesktopPosition) is { } spot ? DecodePosition(spot) : defaults.DesktopPosition;
        hasCompletedOnboarding = store.GetBool(SettingsKeys.HasCompletedOnboarding) ?? defaults.HasCompletedOnboarding;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ISettingsStore Store { get; }

    public CompanionDefaults Defaults { get; }

    public Channels Channels
    {
        get => channels;
        set => SetProperty(ref channels, value, () => Store.SetStrings(SettingsKeys.Channels, value.Names));
    }

    public Frequency Frequency
    {
        get => frequency;
        set => SetProperty(ref frequency, value, () => Store.SetString(SettingsKeys.Frequency, value.RawValue()));
    }

    public int ActiveStartMinute
    {
        get => activeStartMinute;
        set => SetProperty(ref activeStartMinute, value, () => Store.SetInteger(SettingsKeys.ActiveStartMinute, value));
    }

    public int ActiveEndMinute
    {
        get => activeEndMinute;
        set => SetProperty(ref activeEndMinute, value, () => Store.SetInteger(SettingsKeys.ActiveEndMinute, value));
    }

    public IReadOnlySet<int> ActiveWeekdays
    {
        get => activeWeekdays;
        set => SetProperty(ref activeWeekdays, value, () => Store.SetIntegers(SettingsKeys.ActiveWeekdays, value.Order().ToArray()));
    }

    public IReadOnlySet<int> QuietHours
    {
        get => quietHours;
        set => SetProperty(ref quietHours, value, () => Store.SetIntegers(SettingsKeys.QuietHours, value.Order().ToArray()));
    }

    public bool HushInFullScreen
    {
        get => hushInFullScreen;
        set => SetProperty(ref hushInFullScreen, value, () => Store.SetBool(SettingsKeys.HushInFullScreen, value));
    }

    public bool GreetOnReturn
    {
        get => greetOnReturn;
        set => SetProperty(ref greetOnReturn, value, () => Store.SetBool(SettingsKeys.GreetOnReturn, value));
    }

    public bool CommentOnPower
    {
        get => commentOnPower;
        set => SetProperty(ref commentOnPower, value, () => Store.SetBool(SettingsKeys.CommentOnPower, value));
    }

    public bool CommentOnAppearance
    {
        get => commentOnAppearance;
        set => SetProperty(ref commentOnAppearance, value, () => Store.SetBool(SettingsKeys.CommentOnAppearance, value));
    }

    public string? VoiceIdentifier
    {
        get => voiceIdentifier;
        set => SetProperty(ref voiceIdentifier, value, () => SetOrRemove(SettingsKeys.VoiceIdentifier, value));
    }

    public double SpeechRate
    {
        get => speechRate;
        set => SetProperty(ref speechRate, value, () => Store.SetDouble(SettingsKeys.SpeechRate, value));
    }

    public double SpeechVolume
    {
        get => speechVolume;
        set => SetProperty(ref speechVolume, value, () => Store.SetDouble(SettingsKeys.SpeechVolume, value));
    }

    /// <summary>
    /// What the character talks about. Empty would mean every flavor; the app's Settings keeps at least one on. Ids are
    /// kept as stored, so the app drops any it retires.
    /// </summary>
    public IReadOnlySet<FlavorId> PhraseFlavors
    {
        get => phraseFlavors;
        set =>
            SetProperty(
                ref phraseFlavors,
                value,
                () => Store.SetStrings(SettingsKeys.PhraseFlavors, value.Select(f => f.RawValue).Order(StringComparer.Ordinal).ToArray())
            );
    }

    /// <summary>Lines the user asked not to hear again (phrase ids; retired ids are harmless).</summary>
    public IReadOnlySet<string> MutedPhraseIDs
    {
        get => mutedPhraseIDs;
        set => SetProperty(ref mutedPhraseIDs, value, () => Store.SetStrings(SettingsKeys.MutedPhraseIDs, value.Order(StringComparer.Ordinal).ToArray()));
    }

    public AppearanceSetting Appearance
    {
        get => appearance;
        set => SetProperty(ref appearance, value, () => Store.SetString(SettingsKeys.Appearance, value.RawValue()));
    }

    public SkinId Skin
    {
        get => skin;
        set => SetProperty(ref skin, value, () => Store.SetString(SettingsKeys.Skin, value.RawValue));
    }

    /// <summary>Stored as seconds since 1970 (Swift's <c>timeIntervalSince1970</c>); removed when null.</summary>
    public DateTimeOffset? ShushedUntil
    {
        get => shushedUntil;
        set =>
            SetProperty(
                ref shushedUntil,
                value,
                () =>
                {
                    if (value is { } date)
                        Store.SetDouble(SettingsKeys.ShushedUntil, (date - DateTimeOffset.UnixEpoch).TotalSeconds);
                    else
                        Store.Remove(SettingsKeys.ShushedUntil);
                }
            );
    }

    public bool ShowOnDesktop
    {
        get => showOnDesktop;
        set => SetProperty(ref showOnDesktop, value, () => Store.SetBool(SettingsKeys.ShowOnDesktop, value));
    }

    public DesktopSize DesktopSize
    {
        get => desktopSize;
        set => SetProperty(ref desktopSize, value, () => Store.SetString(SettingsKeys.DesktopSize, value.RawValue()));
    }

    /// <summary>Stored as <c>[x, y]</c>; removed when the character goes back to its corner.</summary>
    public DesktopPosition? DesktopPosition
    {
        get => desktopPosition;
        set =>
            SetProperty(
                ref desktopPosition,
                value,
                () =>
                {
                    if (value is { } spot)
                        Store.SetIntegers(SettingsKeys.DesktopPosition, [spot.X, spot.Y]);
                    else
                        Store.Remove(SettingsKeys.DesktopPosition);
                }
            );
    }

    public bool HasCompletedOnboarding
    {
        get => hasCompletedOnboarding;
        set => SetProperty(ref hasCompletedOnboarding, value, () => Store.SetBool(SettingsKeys.HasCompletedOnboarding, value));
    }

    /// <summary>The settings <see cref="QuietRules"/> consults.</summary>
    public Manners Manners => new(Channels, GreetOnReturn, CommentOnPower, CommentOnAppearance);

    /// <summary>
    /// Puts every resettable preference back to its default (onboarding stays done) and removes their keys from the
    /// store, so a user who reset follows the app's defaults when a later version changes them. A subclass overrides
    /// it to reset its own preferences too, removing its own keys the same way, and calls the base.
    /// </summary>
    public virtual void ResetToDefaults()
    {
        Channels = Defaults.Channels;
        Frequency = Defaults.Frequency;
        ActiveStartMinute = Defaults.ActiveStartMinute;
        ActiveEndMinute = Defaults.ActiveEndMinute;
        ActiveWeekdays = Defaults.ActiveWeekdays;
        QuietHours = Defaults.QuietHours;
        HushInFullScreen = Defaults.HushInFullScreen;
        GreetOnReturn = Defaults.GreetOnReturn;
        CommentOnPower = Defaults.CommentOnPower;
        CommentOnAppearance = Defaults.CommentOnAppearance;
        VoiceIdentifier = Defaults.VoiceIdentifier;
        SpeechRate = Defaults.SpeechRate;
        SpeechVolume = Defaults.SpeechVolume;
        PhraseFlavors = Defaults.PhraseFlavors;
        MutedPhraseIDs = Defaults.MutedPhraseIDs;
        Appearance = Defaults.Appearance;
        Skin = Defaults.Skin;
        ShushedUntil = Defaults.ShushedUntil;
        ShowOnDesktop = Defaults.ShowOnDesktop;
        DesktopSize = Defaults.DesktopSize;
        DesktopPosition = Defaults.DesktopPosition;
        foreach (var key in SettingsKeys.Resettable)
            Store.Remove(key);
    }

    /// <summary>Sets <paramref name="field"/>, writes it through with <paramref name="write"/> and raises <see cref="PropertyChanged"/>.</summary>
    protected void SetProperty<T>(ref T field, T value, Action write, [CallerMemberName] string? propertyName = null)
    {
        field = value;
        write();
        OnPropertyChanged(propertyName);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>, or removes the key when it's null.</summary>
    protected void SetOrRemove(string key, string? value)
    {
        if (value is { } text)
            Store.SetString(key, text);
        else
            Store.Remove(key);
    }

    /// <summary><c>[x, y]</c>; anything else is unreadable and falls back to the default spot.</summary>
    internal static DesktopPosition? DecodePosition(IReadOnlyList<int> values) =>
        values.Count == 2 ? new DesktopPosition(values[0], values[1]) : null;
}
