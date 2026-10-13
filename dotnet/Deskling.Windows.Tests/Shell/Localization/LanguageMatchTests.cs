using Deskling.Windows.Shell.Localization;

namespace Deskling.Windows.Tests.Shell.Localization;

public sealed class LanguageMatchTests
{
    private static readonly string[] App = ["en", "it", "pt-BR"];

    [Fact]
    public void ThePickedLanguageWinsOverWindowsPreferences()
    {
        Assert.Equal("it", LanguageMatch.Choose(App, "it", ["en-US"]));
    }

    [Fact]
    public void WithoutAPickTheFirstPreferredLanguageTheAppHasRuns()
    {
        Assert.Equal("it", LanguageMatch.Choose(App, null, ["de-DE", "it-IT", "en-US"]));
        Assert.Equal("en", LanguageMatch.Choose(App, "", ["en-GB"]));
    }

    [Fact]
    public void AnExactTagBeatsABaseLanguageAndBothIgnoreCase()
    {
        Assert.Equal("pt-BR", LanguageMatch.Choose(App, null, ["PT-br"]));
        Assert.Equal("pt-BR", LanguageMatch.Choose(App, null, ["pt-PT"]));
    }

    [Fact]
    public void NothingInCommonRunsTheDefault()
    {
        Assert.Equal("en", LanguageMatch.Choose(App, "fr", ["de-DE", "ja"]));
    }
}
