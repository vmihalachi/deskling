using System.Text;
using System.Text.Json.Nodes;
using Deskling.Companion.Phrases;

namespace Deskling.Companion.Tests;

public sealed class PhraseBankTests
{
    private static readonly PhraseBank Bank = Fixture.Bank;

    [Fact]
    public void IdsAreUniqueAndStable()
    {
        var ids = Bank.Phrases.Select(p => p.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        foreach (var id in ids)
            Assert.True(id.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'), id);
    }

    [Fact]
    public void EveryPhraseHasTagsAndASaneWeight()
    {
        foreach (var phrase in Bank.Phrases)
        {
            Assert.NotEmpty(phrase.Tags);
            Assert.InRange(phrase.Weight, 1, 5);
        }
    }

    [Fact]
    public void EveryLineHasAFlavorAndTunesFitEveryone()
    {
        foreach (var phrase in Bank.Phrases)
            Assert.True(phrase.Flavor is null == phrase.IsTune, phrase.Id);
        foreach (var tune in Bank.Phrases.Where(p => p.IsTune))
        {
            Assert.Empty(tune.Skins);
            Assert.Empty(tune.Languages);
        }
    }

    [Fact]
    public void TheFixtureHasEveryMoment()
    {
        foreach (var tag in Tags.All)
            Assert.NotEmpty(Bank.Tagged(tag));
        Assert.Equal(2, Bank.Tagged(Tag.WelcomeBack).Count);
    }

    [Fact]
    public void CatalogKeys()
    {
        Assert.Equal("phrase.general-a1", Bank["general-a1"]?.CatalogKey);
        Assert.Equal(["phrase.joke-1.1", "phrase.joke-1.2"], Bank["joke-1"]!.CatalogKeys);
        Assert.Equal(new PhraseKind.Sequence(["Fixture joke, setup", "Fixture joke, punchline"]), Bank["joke-1"]!.Kind);
        Assert.Null(Bank["nope"]);
    }

    [Fact]
    public void SkinAndLanguageViews()
    {
        Assert.DoesNotContain(Bank.For(FixtureIds.VectorSkin), p => p.Id == "skin-raster-only");
        Assert.Contains(Bank.For(FixtureIds.RasterSkin), p => p.Id == "skin-raster-only");
        Assert.Equal(2, Bank.Phrases.Count - Bank.InLanguage("en").Count);
        Assert.Equal(Bank.Phrases.Count, Bank.InLanguage("it-IT").Count);
        Assert.Equal(["it-only", "joke-it"], Bank.Phrases.Where(p => !p.FitsLanguage("en")).Select(p => p.Id));
    }

    [Fact]
    public void DaypartsAndWeekdays()
    {
        Assert.Equal(Tag.Morning, Tags.Daypart(5));
        Assert.Equal(Tag.Morning, Tags.Daypart(11));
        Assert.Equal(Tag.Afternoon, Tags.Daypart(12));
        Assert.Equal(Tag.Afternoon, Tags.Daypart(16));
        Assert.Equal(Tag.Evening, Tags.Daypart(17));
        Assert.Equal(Tag.Evening, Tags.Daypart(21));
        Assert.Equal(Tag.Night, Tags.Daypart(22));
        Assert.Equal(Tag.Night, Tags.Daypart(4));
        Assert.Equal(Tag.Monday, Tags.WeekdayTag(2));
        Assert.Equal(Tag.Friday, Tags.WeekdayTag(6));
        Assert.Equal(Tag.Weekend, Tags.WeekdayTag(1));
        Assert.Equal(Tag.Weekend, Tags.WeekdayTag(7));
        Assert.Null(Tags.WeekdayTag(4));
    }

    // PhraseBank.Parse (phrases.json)

    private static PhraseBank ParseChanged(Action<JsonNode> change)
    {
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(Fixture.Root, "companion", "phrases.json")))!;
        change(node);
        return PhraseBank.Parse(new MemoryStream(Encoding.UTF8.GetBytes(node.ToJsonString())));
    }

    private static JsonNode Entry(JsonNode file, string id) => file["phrases"]!.AsArray().First(e => e!["id"]!.GetValue<string>() == id)!;

    [Fact]
    public void ParseReadsLanguagesAndSequences()
    {
        Assert.Equal(27, Bank.Phrases.Count);
        Assert.Equal(["it"], Bank["joke-it"]!.Languages);
        Assert.Empty(Bank["joke-1"]!.Languages);
        Assert.Equal(new FlavorId("c"), Bank["it-only"]!.Flavor);
        Assert.Equal(new PhraseKind.Tune("tune-1"), Bank["tune-1"]!.Kind);
        Assert.Equal([FixtureIds.RasterSkin], Bank["skin-raster-only"]!.Skins);
    }

    [Theory]
    [InlineData("unknown tag")]
    [InlineData("unknown kind")]
    [InlineData("extra field")]
    [InlineData("unlisted flavor")]
    [InlineData("wrong key")]
    [InlineData("wrong part keys")]
    [InlineData("one part")]
    public void ParseIsStrict(string change)
    {
        Action<JsonNode> edit = change switch
        {
            "unknown tag" => n => Entry(n, "general-a1")["tags"] = new JsonArray("lunch"),
            "unknown kind" => n => Entry(n, "general-a1")["kind"] = "song",
            "extra field" => n => Entry(n, "general-a1")["mood"] = "happy",
            "unlisted flavor" => n => Entry(n, "general-a1")["flavor"] = "d",
            "wrong key" => n => Entry(n, "general-a1")["key"] = "phrase.other",
            "wrong part keys" => n => Entry(n, "joke-1")["keys"] = new JsonArray("phrase.joke-1.1", "phrase.joke-1.3"),
            _ => n =>
            {
                Entry(n, "joke-1")["parts"] = new JsonArray("Setup only");
                Entry(n, "joke-1")["keys"] = new JsonArray("phrase.joke-1.1");
            }
            ,
        };
        Assert.Throws<InvalidDataException>(() => ParseChanged(edit));
    }
}
