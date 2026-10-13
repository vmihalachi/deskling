using System.Globalization;
using Deskling.Companion.Settings;
using Deskling.Core.Input;
using Deskling.Core.Settings;
using Deskling.Testing.Conformance;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Settings;
using Deskling.Windows.Companion.Support;

namespace Deskling.Windows.Companion.Tests.Settings;

/// <summary>Swift's <c>SettingsRowsTests</c> and <c>SupportWindowTests</c>, for the parts that are plain logic.</summary>
public sealed class SettingsRowsTests
{
    private readonly CompanionSettings settings = new(new InMemorySettingsStore(), FixtureDefaults.Value);

    [Fact]
    public void TheWeekdaysStartWhereTheCultureStartsItsWeek()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], ActiveHoursEditor.OrderedWeekdays(1));
        Assert.Equal([2, 3, 4, 5, 6, 7, 1], ActiveHoursEditor.OrderedWeekdays(2));
        Assert.Equal([7, 1, 2, 3, 4, 5, 6], ActiveHoursEditor.OrderedWeekdays(7));
    }

    [Fact]
    public void ATimePickersTimeRoundTripsTheMinuteOfTheDay()
    {
        Assert.Equal(new TimeSpan(9, 30, 0), ActiveHoursEditor.Time(9 * 60 + 30));
        Assert.Equal(17 * 60 + 45, ActiveHoursEditor.Minute(new TimeSpan(17, 45, 0)));
        // The end of the day shows as its last minute.
        Assert.Equal(new TimeSpan(23, 59, 0), ActiveHoursEditor.Time(24 * 60));
    }

    [Fact]
    public void QuietHoursAreLabelledTheWayTheCultureWritesHours()
    {
        var american = QuietHoursEditor.Label(15, CultureInfo.GetCultureInfo("en-US"));
        var german = QuietHoursEditor.Label(15, CultureInfo.GetCultureInfo("de-DE"));
        var british = QuietHoursEditor.Label(9, CultureInfo.GetCultureInfo("en-GB"));

        Assert.True(american.Contains('3') && american.Contains("PM"), american);
        Assert.True(german.Contains("15") && !german.Contains("PM"), german);
        Assert.Equal("09", british);
    }

    [Fact]
    public void FlavorsAndTheMutedCountWriteAndReadThrough()
    {
        Assert.True(settings.IsOn(FixtureIds.FlavorB));
        settings.SetFlavor(FixtureIds.FlavorB, false);
        Assert.Equal([FixtureIds.FlavorA, FixtureIds.FlavorC], settings.PhraseFlavors.OrderBy(f => f.RawValue));
        settings.SetFlavor(FixtureIds.FlavorB, true);
        Assert.Equal(FixtureIds.Flavors, settings.PhraseFlavors.OrderBy(f => f.RawValue));

        settings.MutedPhraseIDs = new HashSet<string> { "general-a1", "morning-1", "retired-line" };
        Assert.Equal(2, settings.MutedCount(FixtureBank.Load(ConformanceRoot.Find())));
    }

    [Fact]
    public void ALanguageIsNamedInItsOwnWords()
    {
        Assert.Equal("Deutsch", LanguageRow.NativeName("de"));
        Assert.StartsWith("Português", LanguageRow.NativeName("pt-BR"));
        Assert.StartsWith("Română", LanguageRow.NativeName("ro"));
        Assert.Equal("xx-nope", LanguageRow.NativeName("xx-nope"));
    }

    [Fact]
    public void AShortcutReadsAsItsModifiersThenItsKey()
    {
        var text = ShortcutRecorder.Describe(new KeyShortcut(0x46, KeyShortcut.Control | KeyShortcut.Option, "F"));

        var parts = text.Split('+');
        Assert.Equal(3, parts.Length);
        Assert.Equal("F", parts[^1]);
        Assert.StartsWith("Win+", ShortcutRecorder.Describe(new KeyShortcut(0x46, KeyShortcut.Command, "F")));
    }

    [Fact]
    public void OneAskAtATimeAndOnlyThanksRightAfterAPurchase()
    {
        Assert.Equal(SupportStage.Pitch, SupportWindow.Stage(justSupported: false, justTipped: false, isSupporter: false));
        Assert.Equal(SupportStage.Supporter, SupportWindow.Stage(justSupported: false, justTipped: false, isSupporter: true));
        Assert.Equal(SupportStage.Welcome, SupportWindow.Stage(justSupported: true, justTipped: false, isSupporter: true));
        Assert.Equal(SupportStage.Tipped, SupportWindow.Stage(justSupported: false, justTipped: true, isSupporter: true));
        // The purchase thanks first.
        Assert.Equal(SupportStage.Welcome, SupportWindow.Stage(justSupported: true, justTipped: true, isSupporter: true));
    }
}
