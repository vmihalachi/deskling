using System.Globalization;
using Deskling.Companion.Settings;
using Deskling.Core.Settings;
using Deskling.Testing.Conformance;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Settings;
using Deskling.Windows.Companion.Support;
using Deskling.Windows.Companion.Views;

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
    public void QuietHourChipsTakeSixColumnsUnlessTheWidestChipDoesNotFit()
    {
        // The fixture's Settings: 356 px across, a 24-hour chip ("10") about 38 px wide, a 12-hour one ("10 AM") about 63.
        Assert.Equal(6, QuietHoursEditor.Columns(356, 38));
        Assert.Equal(4, QuietHoursEditor.Columns(356, 63));
        Assert.Equal(3, QuietHoursEditor.Columns(356, 100));
        // A column exactly as wide as the chip fits it; the gaps between the columns count.
        Assert.Equal(6, QuietHoursEditor.Columns(6 * 50 + 5 * Space.Tight, 50));
        Assert.Equal(4, QuietHoursEditor.Columns(6 * 50 + 5 * Space.Tight - 1, 50));
        // Three when nothing fits, six with no limit.
        Assert.Equal(3, QuietHoursEditor.Columns(100, 200));
        Assert.Equal(6, QuietHoursEditor.Columns(double.PositiveInfinity, 63));
    }

    [Fact]
    public void NarratorReadsEachQuietHourAsATimeInTheCulture()
    {
        var american = QuietHoursEditor.HourName(15, CultureInfo.GetCultureInfo("en-US"));
        var german = QuietHoursEditor.HourName(15, CultureInfo.GetCultureInfo("de-DE"));
        var british = QuietHoursEditor.HourName(9, CultureInfo.GetCultureInfo("en-GB"));

        // ICU may put a narrow no-break space before PM.
        Assert.True(american.StartsWith("3:00", StringComparison.Ordinal) && american.EndsWith("PM", StringComparison.Ordinal), american);
        Assert.Equal("15:00", german);
        Assert.Equal("09:00", british);
        Assert.Equal("00:00", QuietHoursEditor.HourName(0, CultureInfo.GetCultureInfo("de-DE")));
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
