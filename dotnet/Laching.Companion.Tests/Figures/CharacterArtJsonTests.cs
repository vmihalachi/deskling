using System.Text;
using System.Text.Json.Nodes;
using Laching.Companion.Figures;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;

namespace Laching.Companion.Tests;

/// <summary><see cref="CharacterArt.Parse"/> reads what <c>tools/character.py</c> writes and refuses broken art.</summary>
public sealed class CharacterArtJsonTests
{
    private static string Committed(string skin) => File.ReadAllText(Path.Combine(Fixture.Root, "companion", "characters", skin + ".json"));

    private static CharacterArt Parse(string json) => CharacterArt.Parse(new MemoryStream(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void TheFixturesParse()
    {
        Assert.Equal(FixtureIds.VectorSkin, Fixture.Vector.Skin);
        Assert.Equal(new Size(100, 100), Fixture.Vector.ViewBox);
        Assert.Equal(12, Fixture.Vector.Parts.Count);
        Assert.All(Fixture.Vector.Parts.Values, p => Assert.IsType<PartContent.Vector>(p));
        Assert.Equal(9, Fixture.Raster.Parts.Count);
        Assert.Equal([4.2, 5.6, 3.8], Fixture.Vector.Rig.Blink.Intervals);
        Assert.Equal(["eyes-closed"], [Fixture.Raster.Rig.Moods[Mood.Asleep].Eyes]);
        Assert.Equal(Fixture.Vector.GlyphFrames.Keys.Order(), ["asleep", "idle", "talk-1", "talk-2"]);
    }

    /// <summary>A copy of sunnysays' sunflower.json, recompiled by <c>tools/character.py</c>: real art, a hundred-odd shapes.</summary>
    [Fact]
    public void SunflowerParses()
    {
        var art = Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Resources", "sunflower.json")));
        Assert.Equal(new SkinId("sunflower"), art.Skin);
        Assert.Equal(new Size(120, 132), art.ViewBox);
        Assert.Equal(new BubbleAnchor("face", 0.12), art.Rig.Bubble);
        Assert.Equal(["zz"], art.Rig.Moods[Mood.Asleep].Overlays);
        Assert.Empty(art.Rig.Moods[Mood.OffHours].HeadExtras);
        Assert.Equal(17, art.Parts.Count);
        Assert.NotEmpty(Assert.IsType<PartContent.Vector>(art.Parts["petals"]).Shapes);
        var sunny = new RiggedCharacter(art);
        Assert.Equal("eyes-lidded", sunny.StillPose(Mood.Shushed).Eyes);
        Assert.Equal("hush-leaf", sunny.StillPose(Mood.HushedForCall).Mouth);
    }

    /// <summary>As Swift's <c>Decodable</c>: a member the reader doesn't know is ignored.</summary>
    [Fact]
    public void ParseIgnoresUnknownMembers()
    {
        var file = JsonNode.Parse(Committed("fixture-vector"))!;
        file["credits"] = "someone";
        file["rig"]!["moods"]!["awake"]!["wiggle"] = 1;
        Assert.Equal(Fixture.Vector.Rig.Bubble, Parse(file.ToJsonString()).Rig.Bubble);
    }

    [Theory]
    [InlineData("missing mood", "asleep")]
    [InlineData("unknown part", "snore")]
    [InlineData("unknown pivot", "hat")]
    [InlineData("no bubble", "bubble")]
    [InlineData("bad frame", "frame")]
    [InlineData("mouths without a rate", "mouthRate")]
    [InlineData("unknown mood", "dancing")]
    [InlineData("null skin", "not character art JSON")]
    [InlineData("unknown command", "Q")]
    public void ParseRefusesBrokenArt(string change, string needle)
    {
        var file = JsonNode.Parse(Committed("fixture-vector"))!;
        var rig = file["rig"]!.AsObject();
        var moods = rig["moods"]!.AsObject();
        switch (change)
        {
            case "missing mood":
                moods.Remove("asleep");
                break;
            case "unknown part":
                moods["asleep"]!["overlays"] = new JsonArray("snore");
                break;
            case "unknown pivot":
                rig["bubble"] = new JsonObject { ["pivot"] = "hat", ["tuck"] = 0.1 };
                break;
            case "no bubble":
                rig.Remove("bubble");
                break;
            case "bad frame":
                file["parts"]!["head"] = new JsonObject { ["image"] = "x", ["frame"] = new JsonArray(1, 2, 3) };
                break;
            case "mouths without a rate":
                moods["talking"]!["mouthRate"] = 0;
                break;
            case "unknown mood":
                moods["dancing"] = moods["awake"]!.DeepClone();
                break;
            case "null skin":
                file["skin"] = null;
                break;
            default:
                file["parts"]!["head"]![0]!["d"]![0]![0] = "Q";
                break;
        }
        var error = Assert.Throws<InvalidDataException>(() => Parse(file.ToJsonString()));
        Assert.Contains(needle, error.Message);
    }
}
