using System.Text;
using System.Text.Json.Nodes;
using Deskling.Companion.Figures;

namespace Deskling.Companion.Tests;

/// <summary>Each replay body reports a vector that no longer matches the code: one changed value is enough.</summary>
public sealed class ReplaysCatchChangesTests
{
    private static readonly IReadOnlyList<RiggedCharacter> Characters = FixtureArt.Characters(Fixture.Root);

    /// <summary>The committed file at <paramref name="path"/> with <paramref name="change"/> applied, as a stream.</summary>
    private static MemoryStream Changed(string path, Action<JsonNode> change)
    {
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(Fixture.Root, path)))!;
        change(node);
        return new MemoryStream(Encoding.UTF8.GetBytes(node.ToJsonString()));
    }

    [Fact]
    public void Phrases()
    {
        var weight = Changed("companion/phrases.json", n => n["phrases"]![0]!["weight"] = 4);
        Assert.Single(PhrasesReplay.Verify(weight, "phrases.json", Fixture.Bank, FixtureIds.Flavors));
        var language = Changed("companion/phrases.json", n => n["phrases"]![0]!["languages"] = new JsonArray("en"));
        Assert.Single(PhrasesReplay.Verify(language, "phrases.json", Fixture.Bank, FixtureIds.Flavors));
        var window = Changed("companion/phrases.json", n => n["recentWindow"] = 11);
        Assert.Single(PhrasesReplay.Verify(window, "phrases.json", Fixture.Bank, FixtureIds.Flavors));
    }

    [Fact]
    public void Settings()
    {
        var rate = Changed("companion/settings.json", n => n["defaults"]!["speechRate"] = 0.6);
        Assert.Single(SettingsReplay.Verify(rate, "settings.json", Fixture.Defaults));
        var beat = Changed("companion/settings.json", n => n["partBeatSeconds"] = 1.5);
        Assert.Single(SettingsReplay.Verify(beat, "settings.json", Fixture.Defaults));
        // An app's own keys sit beside the generic ones.
        var app = Changed(
            "companion/settings.json",
            n =>
            {
                n["defaults"]!["voice"] = "female";
                n["keys"]!["voice"] = "voice";
                n["keyOrder"]!.AsArray().Insert(13, "voice");
            }
        );
        Assert.Empty(SettingsReplay.Verify(app, "settings.json", Fixture.Defaults));
    }

    [Fact]
    public void Picker()
    {
        var draw = Changed("companion/picker/sequence-picked.json", n => n["draws"]![0]!["id"] = "button-press-1");
        Assert.Single(PickerReplay.Verify(draw, "sequence-picked.json", Fixture.Bank));
        var language = Changed("companion/picker/language-scoped-kept.json", n => n["context"]!.AsObject().Remove("language"));
        Assert.NotEmpty(PickerReplay.Verify(language, "language-scoped-kept.json", Fixture.Bank));
    }

    [Fact]
    public void Cadence()
    {
        var fires = Changed("companion/cadence/chatty.json", n => n["fireMinutes"]![0] = 1);
        Assert.Single(CadenceReplay.Verify(fires, "chatty.json"));
        var config = Changed("companion/cadence/chatty.json", n => n["config"]!["busyGraceSeconds"] = 31);
        Assert.Single(CadenceReplay.Verify(config, "chatty.json"));
    }

    [Fact]
    public void Quiet()
    {
        var mood = Changed("companion/quiet/sequence-delivered.json", n => n["steps"]![0]!["mood"] = "asleep");
        Assert.Single(QuietReplay.Verify(mood, "sequence-delivered.json", Fixture.Bank, Fixture.Defaults));
        var language = Changed("companion/quiet/italian-afternoon.json", n => n.AsObject().Remove("language"));
        Assert.NotEmpty(QuietReplay.Verify(language, "italian-afternoon.json", Fixture.Bank, Fixture.Defaults));
    }

    [Fact]
    public void Desktop()
    {
        var shown = Changed(
            "companion/desktop.json",
            n => n["timelines"]![0]!["steps"]![0]!["shown"] = !n["timelines"]![0]!["steps"]![0]!["shown"]!.GetValue<bool>()
        );
        Assert.Single(DesktopReplay.Verify(shown, "desktop.json"));
        var delay = Changed("companion/desktop.json", n => n["partDelays"]![0]!["seconds"] = 9);
        Assert.Single(DesktopReplay.Verify(delay, "desktop.json"));
    }

    [Fact]
    public void Character()
    {
        var angle = Changed("companion/characters/fixture-raster-poses.json", n => n["poses"]![1]!["pose"]!["headAngle"] = 1.5);
        Assert.Single(CharacterReplay.Verify(angle, "fixture-raster-poses.json", Characters));
        var layer = Changed("companion/characters/fixture-raster-poses.json", n => n["layers"]![1]!["layers"]![1]!["transform"]![4] = 0.5);
        Assert.Single(CharacterReplay.Verify(layer, "fixture-raster-poses.json", Characters));
        var path = Changed(
            "companion/characters/fixture-vector-poses.json",
            n => n["shapes"]![0]!["shapes"]![0]!["d"] = n["shapes"]![0]!["shapes"]![0]!["d"]!.GetValue<string>().Replace(" Z", "")
        );
        Assert.Single(CharacterReplay.Verify(path, "fixture-vector-poses.json", Characters));
        var glyph = Changed("companion/characters/fixture-vector-poses.json", n => n["glyph"]![0]!["shapes"]![0]!["strokeWidth"] = 2);
        Assert.Single(CharacterReplay.Verify(glyph, "fixture-vector-poses.json", Characters));
        var art = Changed("companion/characters/fixture-vector.json", n => n["rig"]!["bubble"]!["tuck"] = 0.2);
        Assert.Single(CharacterReplay.Verify(art, "fixture-vector.json", Characters));
    }
}
