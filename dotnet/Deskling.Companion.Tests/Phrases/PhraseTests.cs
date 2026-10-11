using Deskling.Companion.Phrases;

namespace Deskling.Companion.Tests;

public sealed class PhraseTests
{
    [Fact]
    public void ALineHasOneKeyAndOneClip()
    {
        var line = new Phrase("hello", Phrase.Line("Hello"), [Tag.General]);
        Assert.Equal(1, line.PartCount);
        Assert.Equal("phrase.hello", line.CatalogKey);
        Assert.Equal(["phrase.hello"], line.CatalogKeys);
        Assert.Equal(["hello"], line.ClipIDs);
        Assert.False(line.IsTune);
    }

    [Fact]
    public void ASequenceHasAKeyAndAClipPerPart()
    {
        var joke = new Phrase("joke", Phrase.Sequence("Setup", "Middle", "Punchline"), [Tag.General]);
        Assert.Equal(3, joke.PartCount);
        Assert.Equal("phrase.joke", joke.CatalogKey);
        Assert.Equal(["phrase.joke.1", "phrase.joke.2", "phrase.joke.3"], joke.CatalogKeys);
        Assert.Equal(["joke.1", "joke.2", "joke.3"], joke.ClipIDs);
        Assert.False(joke.IsTune);
    }

    [Fact]
    public void ASequenceNeedsTwoParts() =>
        Assert.Throws<ArgumentException>(() => new Phrase("joke", Phrase.Sequence("Only a setup"), [Tag.General]));

    [Fact]
    public void ATuneHasNothingToSay()
    {
        var tune = new Phrase("tune", Phrase.Tune("tune-1"), [Tag.General]);
        Assert.Equal(1, tune.PartCount);
        Assert.Empty(tune.CatalogKeys);
        Assert.Empty(tune.ClipIDs);
        Assert.True(tune.IsTune);
    }

    [Fact]
    public void LanguagesCompareByTheirBase()
    {
        var everywhere = new Phrase("any", Phrase.Line("Any"), [Tag.General]);
        var italian = new Phrase("it", Phrase.Line("Ciao"), [Tag.General], languages: ["it"]);
        foreach (var language in new[] { "en", "it", "pt-BR" })
            Assert.True(everywhere.FitsLanguage(language), language);
        Assert.True(italian.FitsLanguage("it"));
        Assert.True(italian.FitsLanguage("it-CH"));
        Assert.True(italian.FitsLanguage("IT_it"));
        Assert.False(italian.FitsLanguage("en"));
        Assert.False(italian.FitsLanguage("id"));
        Assert.Equal("pt", Phrase.BaseLanguage("pt-BR"));
        Assert.Equal("zh", Phrase.BaseLanguage("zh_Hans_CN"));
        Assert.Equal("en", Phrase.BaseLanguage("en"));
    }

    [Fact]
    public void Skins()
    {
        var mine = new Phrase("mine", Phrase.Line("Mine"), [Tag.General], skins: [new SkinId("one")]);
        Assert.True(mine.Fits("one"));
        Assert.False(mine.Fits("two"));
        Assert.True(new Phrase("any", Phrase.Line("Any"), [Tag.General]).Fits("two"));
    }

    [Fact]
    public void PhrasesCompareByValue()
    {
        Phrase Joke() => new("joke", Phrase.Sequence("a", "b"), [Tag.General, Tag.ButtonPress], 3, "a", null, ["it"]);
        Assert.Equal(Joke(), Joke());
        Assert.NotEqual(Joke(), new Phrase("joke", Phrase.Sequence("a", "c"), [Tag.General, Tag.ButtonPress], 3, "a", null, ["it"]));
        Assert.NotEqual(Joke(), new Phrase("joke", Phrase.Sequence("a", "b"), [Tag.General, Tag.ButtonPress], 3, "a"));
    }

    [Fact]
    public void TagsRoundTripTheirRawValues()
    {
        foreach (var tag in Tags.All)
            Assert.Equal(tag, Tags.Parse(tag.RawValue()));
        Assert.Null(Tags.Parse("lunch"));
        Assert.Equal(9, Tags.Events.Count);
    }
}
