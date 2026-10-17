using System.Text.Json;
using System.Text.Json.Serialization;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;

namespace Laching.Companion.Figures;

public sealed partial record CharacterArt
{
    /// <summary>
    /// Reads the <c>characters/&lt;skin&gt;.json</c> format (Swift's <c>CharacterArtJSON.decode</c>; what
    /// <c>tools/character.py --out</c> writes): a part is an array of shapes or an image
    /// <c>{"image": "&lt;skin&gt;.&lt;part&gt;", "frame": [x, y, w, h]}</c>. Refuses art whose rig names a part, pivot,
    /// glyph frame or mood that isn't there, so <see cref="RiggedCharacter"/> never meets a missing part. Reads through a
    /// source-generated context, so Native AOT is happy. Throws <see cref="InvalidDataException"/>.
    /// </summary>
    public static CharacterArt Parse(Stream json)
    {
        CharacterArt art;
        try
        {
            var file = JsonSerializer.Deserialize(json, ArtJson.Default.ArtFile) ?? throw new InvalidDataException("character art JSON is empty");
            art = file.Art();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new InvalidDataException($"not character art JSON: {error.Message}", error);
        }
        Validate(art);
        return art;
    }

    /// <summary>A part: an array of shapes, or an image object.</summary>
    internal static PartContent Content(JsonElement e, string name)
    {
        if (e.ValueKind == JsonValueKind.Array)
            return new PartContent.Vector(e.Deserialize(ArtJson.Default.ShapeFileArray)!.Select(s => s.Shape()).ToArray());
        var image = e.Deserialize(ArtJson.Default.ImageFile) ?? throw new InvalidDataException($"part '{name}' is null");
        if (image.Frame.Length != 4)
            throw new InvalidDataException($"part '{name}': frame needs 4 numbers");
        return new PartContent.Image(image.Image, new Rect(image.Frame[0], image.Frame[1], image.Frame[2], image.Frame[3]));
    }

    /// <summary>A path command: <c>["M", x, y]</c>, <c>["L", x, y]</c>, <c>["C", x1, y1, x2, y2, x, y]</c> or <c>["Z"]</c>.</summary>
    internal static PathCommand CommandOf(JsonElement[] items)
    {
        var op = items[0].GetString();
        var numbers = items.Skip(1).Select(n => n.GetDouble()).ToArray();
        Point[] Points(int count)
        {
            if (numbers.Length != 2 * count)
                throw new InvalidDataException($"path command {op} needs {2 * count} numbers");
            return Enumerable.Range(0, count).Select(i => new Point(numbers[2 * i], numbers[2 * i + 1])).ToArray();
        }
        switch (op)
        {
            case "M":
                return new PathCommand.Move(Points(1)[0]);
            case "L":
                return new PathCommand.Line(Points(1)[0]);
            case "C":
                var p = Points(3);
                return new PathCommand.Cubic(p[0], p[1], p[2]);
            case "Z":
                Points(0);
                return new PathCommand.Close();
            default:
                throw new InvalidDataException($"unknown path command '{op}'");
        }
    }

    internal static Mood MoodNamed(string name) => Moods.Parse(name) ?? throw new InvalidDataException($"unknown mood '{name}'");

    internal static Point PointOf(double[] numbers, string what) =>
        numbers.Length == 2 ? new Point(numbers[0], numbers[1]) : throw new InvalidDataException($"{what} needs 2 numbers");

    /// <summary>Everything the rig names must exist, so <see cref="RiggedCharacter"/> never meets a missing part.</summary>
    private static void Validate(CharacterArt art)
    {
        var rig = art.Rig;
        void Part(string name, string where)
        {
            if (!art.Parts.ContainsKey(name))
                throw new InvalidDataException($"{where} names unknown part '{name}'");
        }
        void Pivot(string name, string where)
        {
            if (!art.Pivots.ContainsKey(name))
                throw new InvalidDataException($"{where} names unknown pivot '{name}'");
        }
        foreach (var name in rig.Always)
            Part(name, "always");
        foreach (var (key, group) in new[] { ("head", rig.Head), ("stem", rig.Stem), ("pulse", rig.Pulse) })
        {
            Pivot(group.Pivot, key);
            foreach (var name in group.Parts)
                Part(name, key);
        }
        Part(rig.Blink.ClosedEyes, "blink");
        Pivot(rig.Bubble.Pivot, "bubble");
        foreach (var mood in Moods.All)
        {
            if (!rig.Moods.TryGetValue(mood, out var m))
                throw new InvalidDataException($"mood '{mood.RawValue()}' is missing");
            var label = $"mood '{mood.RawValue()}'";
            if (m.Mouth.Count == 0)
                throw new InvalidDataException($"{label} needs at least one mouth");
            if (m.Mouth.Count > 1 && !(m.MouthRate > 0))
                throw new InvalidDataException($"{label} alternates mouths without a mouthRate");
            foreach (var name in new[] { m.Eyes }.Concat(m.Mouth).Concat(m.HeadExtras).Concat(m.Overlays).Concat(m.RestMouth is { } rest ? [rest] : []))
                Part(name, label);
            IReadOnlyList<string> frames = rig.Glyph.TryGetValue(mood, out var list) ? list : [];
            if (frames.Count == 0)
                throw new InvalidDataException($"{label} has no glyph frames");
            foreach (var frame in frames)
            {
                if (!art.GlyphFrames.ContainsKey(frame))
                    throw new InvalidDataException($"{label} names unknown glyph frame '{frame}'");
            }
        }
    }
}

/// <summary>
/// The shape of <c>characters/&lt;skin&gt;.json</c>. A part stays a <see cref="JsonElement"/> (an array of shapes or an
/// image), and so does each path command (the op, then its numbers).
/// </summary>
internal sealed record ArtFile(
    string Skin,
    double[] ViewBox,
    Dictionary<string, double[]> Pivots,
    Dictionary<string, JsonElement> Parts,
    RigFile Rig,
    GlyphFile Glyph
)
{
    public CharacterArt Art()
    {
        if (ViewBox.Length != 2)
            throw new InvalidDataException("viewBox needs 2 numbers");
        var rig = new Rig(
            Rig.Period,
            Rig.StillTime,
            Rig.Always,
            Rig.Head,
            Rig.Stem,
            Rig.Pulse,
            Rig.Blink,
            Rig.Moods.ToDictionary(p => CharacterArt.MoodNamed(p.Key), p => p.Value),
            Rig.Glyph.ToDictionary(p => CharacterArt.MoodNamed(p.Key), p => (IReadOnlyList<string>)p.Value),
            Rig.Bubble
        );
        return new CharacterArt(
            new SkinId(Skin),
            new Size(ViewBox[0], ViewBox[1]),
            Pivots.ToDictionary(p => p.Key, p => CharacterArt.PointOf(p.Value, $"pivot '{p.Key}'")),
            Parts.ToDictionary(p => p.Key, p => CharacterArt.Content(p.Value, p.Key)),
            rig,
            Glyph.Size,
            Glyph.Frames.ToDictionary(p => p.Key, p => (IReadOnlyList<PathShape>)p.Value.Select(s => s.Shape()).ToArray())
        );
    }
}

/// <summary>The rig as the file has it: its groups, blink, moods and bubble are the <see cref="Rig"/>'s own records.</summary>
internal sealed record RigFile(
    double Period,
    double StillTime,
    string[] Always,
    RigGroup Head,
    RigGroup Stem,
    RigGroup Pulse,
    Blink Blink,
    Dictionary<string, MoodRig> Moods,
    Dictionary<string, string[]> Glyph,
    BubbleAnchor Bubble
);

internal sealed record GlyphFile(double Size, Dictionary<string, ShapeFile[]> Frames);

internal sealed record ImageFile(string Image, double[] Frame);

internal sealed record ShapeFile(JsonElement[][] D, double StrokeWidth, double Opacity, bool EvenOdd, string? Fill = null, string? Stroke = null)
{
    public PathShape Shape() =>
        new(
            D.Select(CharacterArt.CommandOf).ToArray(),
            Fill is { } fill ? new PaintRole(fill) : (PaintRole?)null,
            Stroke is { } stroke ? new PaintRole(stroke) : (PaintRole?)null,
            StrokeWidth,
            Opacity,
            EvenOdd
        );
}

/// <summary>
/// Source-generated, so apps can compile it with Native AOT. A missing or null member fails (but <c>fill</c>,
/// <c>stroke</c> and <c>restMouth</c>, which may be left out); unknown members are ignored, as Swift's
/// <c>Decodable</c> ignores them. <see cref="RigGroup"/>, <see cref="Blink"/>, <see cref="BubbleAnchor"/> and
/// <see cref="MoodRig"/> are read as they are: their parameters are the file's member names.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true
)]
[JsonSerializable(typeof(ArtFile))]
[JsonSerializable(typeof(ShapeFile[]))]
[JsonSerializable(typeof(ImageFile))]
internal sealed partial class ArtJson : JsonSerializerContext;
