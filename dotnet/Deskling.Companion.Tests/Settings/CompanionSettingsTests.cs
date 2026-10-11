using Deskling.Companion.Cadence;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Deskling.Core.Input;
using Deskling.Core.Settings;

namespace Deskling.Companion.Tests;

public sealed class CompanionSettingsTests
{
    private static CompanionSettings Make(InMemorySettingsStore store) => new(store, Fixture.Defaults);

    [Fact]
    public void DefaultsOnAFreshStore()
    {
        var store = new InMemorySettingsStore();
        var settings = Make(store);
        Assert.Equal(Channels.Voice, settings.Channels);
        Assert.Equal(Frequency.Sometimes, settings.Frequency);
        Assert.Equal(540, settings.ActiveStartMinute);
        Assert.Equal(1080, settings.ActiveEndMinute);
        Assert.Equal([2, 3, 4, 5, 6], settings.ActiveWeekdays.Order());
        Assert.Empty(settings.QuietHours);
        Assert.True(settings.HushInFullScreen);
        Assert.True(settings.GreetOnReturn);
        Assert.Null(settings.VoiceIdentifier);
        Assert.Equal(0.5, settings.SpeechRate);
        Assert.Equal(Fixture.Defaults.SayShortcut, settings.SayShortcut);
        Assert.Equal(AppearanceSetting.System, settings.Appearance);
        Assert.Equal(FixtureIds.VectorSkin, settings.Skin);
        Assert.Null(settings.ShushedUntil);
        Assert.False(settings.HasCompletedOnboarding);
        // Reading defaults writes nothing.
        Assert.Empty(store.Keys);
    }

    [Fact]
    public void ChangesWriteThroughUnderStableKeys()
    {
        var store = new InMemorySettingsStore();
        var settings = Make(store);
        var until = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        settings.Channels = Channels.Chime | Channels.Notification;
        settings.Frequency = Frequency.Chatty;
        settings.ActiveWeekdays = new HashSet<int> { 7, 1 };
        settings.QuietHours = new HashSet<int> { 13 };
        settings.VoiceIdentifier = "com.apple.voice.compact.en-US.Samantha";
        settings.ShushedUntil = until;
        settings.SayShortcut = null;
        settings.HasCompletedOnboarding = true;
        Assert.Equal(["chime", "notification"], store.GetStrings(SettingsKeys.Channels)!);
        Assert.Equal("chatty", store.GetString(SettingsKeys.Frequency));
        Assert.Equal([1, 7], store.GetIntegers(SettingsKeys.ActiveWeekdays)!);
        Assert.Equal([13], store.GetIntegers(SettingsKeys.QuietHours)!);
        Assert.Equal("com.apple.voice.compact.en-US.Samantha", store.GetString(SettingsKeys.VoiceIdentifier));
        Assert.Equal(1_800_000_000, store.GetDouble(SettingsKeys.ShushedUntil));
        Assert.Equal("", store.GetString(SettingsKeys.SayShortcut));
        Assert.True(store.GetBool(SettingsKeys.HasCompletedOnboarding));

        var reloaded = Make(store);
        Assert.Equal(Channels.Chime | Channels.Notification, reloaded.Channels);
        Assert.Equal(Frequency.Chatty, reloaded.Frequency);
        Assert.Equal([1, 7], reloaded.ActiveWeekdays.Order());
        Assert.Equal(until, reloaded.ShushedUntil);
        Assert.Null(reloaded.SayShortcut);
        Assert.True(reloaded.HasCompletedOnboarding);
    }

    [Fact]
    public void FlavorsAndMutedLinesRoundTripAndReset()
    {
        var store = new InMemorySettingsStore();
        var settings = Make(store);
        Assert.True(settings.PhraseFlavors.SetEquals(FixtureIds.Flavors));
        Assert.Empty(settings.MutedPhraseIDs);
        settings.PhraseFlavors = new HashSet<FlavorId> { FixtureIds.FlavorC, FixtureIds.FlavorB };
        settings.MutedPhraseIDs = new HashSet<string> { "general-a2" };
        Assert.Equal(["b", "c"], store.GetStrings(SettingsKeys.PhraseFlavors)!);
        Assert.Equal(["general-a2"], store.GetStrings(SettingsKeys.MutedPhraseIDs)!);
        var reloaded = Make(store);
        Assert.True(reloaded.PhraseFlavors.SetEquals([FixtureIds.FlavorC, FixtureIds.FlavorB]));
        Assert.Equal(["general-a2"], reloaded.MutedPhraseIDs);
        reloaded.ResetToDefaults();
        Assert.True(reloaded.PhraseFlavors.SetEquals(FixtureIds.Flavors));
        Assert.Empty(reloaded.MutedPhraseIDs);
    }

    [Fact]
    public void ShortcutRoundTripsInTheSwiftShape()
    {
        var store = new InMemorySettingsStore();
        var shortcut = new KeyShortcut(96, KeyShortcut.Option, "F5");
        Make(store).SayShortcut = shortcut;
        Assert.Equal("""{"keyCode":96,"modifiers":2048,"key":"F5"}""", store.GetString(SettingsKeys.SayShortcut));
        Assert.Equal(shortcut, Make(store).SayShortcut);
        store.SetString(SettingsKeys.SayShortcut, "{not json");
        Assert.Null(Make(store).SayShortcut);
    }

    [Fact]
    public void UnknownStoredValuesFallBackToDefaults()
    {
        var store = new InMemorySettingsStore();
        store.SetString(SettingsKeys.Frequency, "sometimes-ish");
        store.SetStrings(SettingsKeys.Channels, ["voice", "drums"]);
        store.SetString(SettingsKeys.Skin, "");
        store.SetInteger(SettingsKeys.Appearance, 3);
        var settings = Make(store);
        Assert.Equal(Frequency.Sometimes, settings.Frequency);
        Assert.Equal(Channels.Voice, settings.Channels);
        Assert.Equal(FixtureIds.VectorSkin, settings.Skin);
        Assert.Equal(AppearanceSetting.System, settings.Appearance);
    }

    [Fact]
    public void SkinsAndFlavorsAreTheAppsIds()
    {
        var store = new InMemorySettingsStore();
        Make(store).Skin = FixtureIds.RasterSkin;
        Assert.Equal("fixture-raster", store.GetString(SettingsKeys.Skin));
        // Validity is the app's: an id the package doesn't know is kept as stored.
        store.SetString(SettingsKeys.Skin, "retired-skin");
        store.SetStrings(SettingsKeys.PhraseFlavors, ["a", "retired"]);
        var reloaded = Make(store);
        Assert.Equal(new SkinId("retired-skin"), reloaded.Skin);
        Assert.True(reloaded.PhraseFlavors.SetEquals([new FlavorId("a"), new FlavorId("retired")]));
    }

    [Fact]
    public void DefaultsComeFromTheApp()
    {
        var defaults = new CompanionDefaults("one", new HashSet<FlavorId> { "x" }, null)
        {
            Frequency = Frequency.Chatty,
            Channels = Channels.Voice | Channels.Chime,
        };
        var settings = new CompanionSettings(new InMemorySettingsStore(), defaults);
        Assert.Equal(new SkinId("one"), settings.Skin);
        Assert.Equal([new FlavorId("x")], settings.PhraseFlavors);
        Assert.Null(settings.SayShortcut);
        Assert.Equal(Frequency.Chatty, settings.Frequency);
        Assert.Equal(Channels.Voice | Channels.Chime, settings.Channels);
        Assert.Same(defaults, settings.Defaults);
    }

    [Fact]
    public void ResetKeepsOnboardingDone()
    {
        var settings = Make(new InMemorySettingsStore());
        settings.Frequency = Frequency.Rarely;
        settings.HasCompletedOnboarding = true;
        settings.ResetToDefaults();
        Assert.Equal(Frequency.Sometimes, settings.Frequency);
        Assert.True(settings.HasCompletedOnboarding);
        Assert.Equal(23, SettingsKeys.All.Count);
        Assert.Equal(23, SettingsKeys.All.Distinct().Count());
        Assert.Equal(SettingsKeys.All.Count - 1, SettingsKeys.Resettable.Count);
        Assert.DoesNotContain(SettingsKeys.HasCompletedOnboarding, SettingsKeys.Resettable);
    }

    [Fact]
    public void DesktopSettingsRoundTripAndReset()
    {
        var store = new InMemorySettingsStore();
        var settings = Make(store);
        Assert.False(settings.ShowOnDesktop);
        Assert.Equal(DesktopSize.Medium, settings.DesktopSize);
        Assert.Null(settings.DesktopPosition);
        settings.ShowOnDesktop = true;
        settings.DesktopSize = DesktopSize.Large;
        settings.DesktopPosition = new DesktopPosition(1300, -40);
        Assert.True(store.GetBool(SettingsKeys.ShowOnDesktop));
        Assert.Equal("large", store.GetString(SettingsKeys.DesktopSize));
        Assert.Equal([1300, -40], store.GetIntegers(SettingsKeys.DesktopPosition)!);

        var reloaded = Make(store);
        Assert.True(reloaded.ShowOnDesktop);
        Assert.Equal(DesktopSize.Large, reloaded.DesktopSize);
        Assert.Equal(new DesktopPosition(1300, -40), reloaded.DesktopPosition);

        reloaded.ResetToDefaults();
        Assert.False(reloaded.ShowOnDesktop);
        Assert.Equal(DesktopSize.Medium, reloaded.DesktopSize);
        Assert.Null(reloaded.DesktopPosition);
        Assert.Null(store.GetIntegers(SettingsKeys.DesktopPosition));
    }

    [Fact]
    public void AnUnreadableDesktopPositionMeansTheCorner()
    {
        var store = new InMemorySettingsStore();
        store.SetIntegers(SettingsKeys.DesktopPosition, [12]);
        store.SetString(SettingsKeys.DesktopSize, "huge");
        var settings = Make(store);
        Assert.Null(settings.DesktopPosition);
        Assert.Equal(DesktopSize.Medium, settings.DesktopSize);
    }

    [Fact]
    public void MannersMirrorTheSettings()
    {
        var settings = Make(new InMemorySettingsStore());
        settings.CommentOnPower = false;
        Assert.Equal(new Manners(Channels.Voice, GreetOnReturn: true, CommentOnPower: false, CommentOnAppearance: true), settings.Manners);
    }

    [Fact]
    public void EverySetRaisesPropertyChangedAndWritesThrough()
    {
        var store = new InMemorySettingsStore();
        var settings = Make(store);
        var changed = new List<string?>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        settings.Frequency = Frequency.Sometimes; // the same value, as Swift's didSet
        settings.Skin = FixtureIds.RasterSkin;
        Assert.Equal([nameof(CompanionSettings.Frequency), nameof(CompanionSettings.Skin)], changed);
        Assert.Equal("sometimes", store.GetString(SettingsKeys.Frequency));
    }
}
