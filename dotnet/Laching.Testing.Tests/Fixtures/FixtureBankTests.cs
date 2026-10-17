using System.Text.Json.Nodes;
using Laching.Companion.Phrases;
using Xunit.Sdk;

namespace Laching.Testing.Tests.Fixtures;

/// <summary>The fixture bank and defaults load from the vectors folder, whole and as Swift generated them.</summary>
public sealed class FixtureBankTests
{
    private static readonly string Root = ConformanceRoot.Find();

    private static readonly PhraseBank Bank = FixtureBank.Load(Root);

    private static Phrase PhraseNamed(string id) => Bank[id] ?? throw new XunitException($"{id} is missing from the fixture bank");

    [Fact]
    public void LoadsEveryPhraseInTheFileInOrder()
    {
        var file = JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "companion", "phrases.json")))!;
        var ids = file["phrases"]!.AsArray().Select(phrase => (string)phrase!["id"]!).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(ids, Bank.Phrases.Select(phrase => phrase.Id));
    }

    [Fact]
    public void ReadsAPhrasesWeightTagsAndFlavor()
    {
        var first = PhraseNamed("general-a1");
        Assert.Equal(1, first.Weight);
        Assert.Equal(new HashSet<Tag> { Tag.General }, first.Tags);
        Assert.Equal(FixtureIds.FlavorA, first.Flavor);
        Assert.Equal(3, PhraseNamed("general-a2").Weight);
    }

    [Fact]
    public void TheDefaultsPickTheVectorSkinAndEveryFlavor()
    {
        Assert.Equal(FixtureIds.VectorSkin, FixtureDefaults.Value.Skin);
        Assert.Equal(FixtureIds.Flavors.ToHashSet(), FixtureDefaults.Value.PhraseFlavors);
    }
}
