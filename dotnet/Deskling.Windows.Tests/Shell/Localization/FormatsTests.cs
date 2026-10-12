using Deskling.Windows.Shell.Localization;

namespace Deskling.Windows.Tests.Shell.Localization;

public sealed class FormatsTests
{
    // Through Formatter, not WeekdayMonthDay and Hour: those read Loc.Language, and Loc needs the Windows App SDK's
    // resources, which this unpackaged host doesn't have. 0.5.1 and earlier threw building the formatter.
    [Fact]
    public void BuildsDateFormattersForALanguage()
    {
        var day = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        // Windows marks each part's direction (U+200E), which doesn't show.
        static string Visible(string text) => text.Replace("‎", "");
        Assert.Equal("Mon, Oct 5", Visible(Formats.Formatter("dayofweek.abbreviated month.abbreviated day", "en-US").Format(day, "UTC")));
        Assert.Equal("3 PM", Visible(Formats.Formatter("hour", "en-US").Format(day.AddHours(3), "UTC")));
    }
}
