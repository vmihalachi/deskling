using System.Text.Json;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;

namespace Laching.Companion.Figures;

public sealed partial record CharacterArt
{
    /// <summary>
    /// Reads the <c>characters/&lt;skin&gt;.json</c> format (Swift's <c>CharacterArtJSON.decode</c>; what
    /// <c>tools/character.py --out</c> writes): a part is an array of shapes or an image
    /// <c>{"image": "&lt;skin&gt;.&lt;part&gt;", "frame": [x, y, w, h]}</c>. Refuses art whose rig names a part, pivot,
    /// glyph frame or mood that isn't there, so <see cref="RiggedCharacter"/> never meets a missing part. Reads with
    /// <see cref="JsonDocument"/>: no reflection, so Native AOT is happy. Throws <see cref="InvalidDataException"/>.
    /// </summary>
    public static CharacterArt Parse(Stream json)
    {
        CharacterArt art;
        try
        {
            using var doc = JsonDocument.Parse(json);
            art = ArtFile(doc.RootElement);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new InvalidDataException($"not character art JSON: {error.Message}", error);
        }
        Validate(art);
        return art;
    }

    private static CharacterArt ArtFile(JsonElement root)
    {
        var viewBox = Numbers(Get(root, "viewBox"));
        if (viewBox.Length != 2)
            throw new InvalidDataException("viewBox needs 2 numbers");
        var pivots = Get(root, "pivots").EnumerateObject().ToDictionary(p => p.Name, p => PointOf(p.Value, $"pivot '{p.Name}'"));
        var parts = Get(root, "parts").EnumerateObject().ToDictionary(p => p.Name, p => Content(p.Value, p.Name));
        var glyph = Get(root, "glyph");
        return new CharacterArt(
            new SkinId(Get(root, "skin").GetString()!),
            new Size(viewBox[0], viewBox[1]),
            pivots,
            parts,
            RigOf(Get(root, "rig")),
            Get(glyph, "size").GetDouble(),
            Get(glyph, "frames").EnumerateObject().ToDictionary(p => p.Name, p => (IReadOnlyList<PathShape>)ShapeList(p.Value))
        );
    }

    private static PartContent Content(JsonElement e, string name)
    {
        if (e.ValueKind == JsonValueKind.Array)
            return new PartContent.Vector(ShapeList(e));
        var frame = Numbers(Get(e, "frame"));
        if (frame.Length != 4)
            throw new InvalidDataException($"part '{name}': frame needs 4 numbers");
        return new PartContent.Image(Get(e, "image").GetString()!, new Rect(frame[0], frame[1], frame[2], frame[3]));
    }

    private static PathShape[] ShapeList(JsonElement e) => e.EnumerateArray().Select(ShapeOf).ToArray();

    private static PathShape ShapeOf(JsonElement e) =>
        new(
            Get(e, "d").EnumerateArray().Select(CommandOf).ToArray(),
            Role(e, "fill"),
            Role(e, "stroke"),
            Get(e, "strokeWidth").GetDouble(),
            Get(e, "opacity").GetDouble(),
            Get(e, "evenOdd").GetBoolean()
        );

    private static PathCommand CommandOf(JsonElement e)
    {
        var items = e.EnumerateArray().ToArray();
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

    private static PaintRole? Role(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? new PaintRole(value.GetString()!) : (PaintRole?)null;

    private static Rig RigOf(JsonElement e)
    {
        var blink = Get(e, "blink");
        var bubble = Get(e, "bubble");
        return new Rig(
            Get(e, "period").GetDouble(),
            Get(e, "stillTime").GetDouble(),
            Strings(Get(e, "always")),
            Group(Get(e, "head")),
            Group(Get(e, "stem")),
            Group(Get(e, "pulse")),
            new Blink(Get(blink, "duration").GetDouble(), Numbers(Get(blink, "intervals")), Get(blink, "closedEyes").GetString()!),
            Get(e, "moods").EnumerateObject().ToDictionary(p => MoodNamed(p.Name), p => MoodRigOf(p.Value)),
            Get(e, "glyph").EnumerateObject().ToDictionary(p => MoodNamed(p.Name), p => (IReadOnlyList<string>)Strings(p.Value)),
            new BubbleAnchor(Get(bubble, "pivot").GetString()!, Get(bubble, "tuck").GetDouble())
        );
    }

    private static MoodRig MoodRigOf(JsonElement e) =>
        new(
            Get(e, "eyes").GetString()!,
            Strings(Get(e, "mouth")),
            Get(e, "mouthRate").GetDouble(),
            Strings(Get(e, "headExtras")),
            Strings(Get(e, "overlays")),
            Get(e, "blink").GetBoolean(),
            Get(e, "droop").GetDouble(),
            Get(e, "sink").GetDouble(),
            Get(e, "sway").GetDouble(),
            Get(e, "swayPeriod").GetDouble(),
            Get(e, "pulse").GetDouble(),
            e.TryGetProperty("restMouth", out var rest) && rest.ValueKind != JsonValueKind.Null ? rest.GetString() : null
        );

    private static RigGroup Group(JsonElement e) =>
        new(Get(e, "pivot").GetString()!, Strings(Get(e, "parts")), Get(e, "follow").GetDouble());

    private static Mood MoodNamed(string name) => Moods.Parse(name) ?? throw new InvalidDataException($"unknown mood '{name}'");

    private static Point PointOf(JsonElement e, string what)
    {
        var numbers = Numbers(e);
        return numbers.Length == 2 ? new Point(numbers[0], numbers[1]) : throw new InvalidDataException($"{what} needs 2 numbers");
    }

    /// <summary>The member <paramref name="name"/>, or an error that names it.</summary>
    private static JsonElement Get(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value)
            ? value
            : throw new InvalidDataException($"missing '{name}'");

    private static double[] Numbers(JsonElement e) => e.EnumerateArray().Select(n => n.GetDouble()).ToArray();

    private static string[] Strings(JsonElement e) => e.EnumerateArray().Select(s => s.GetString()!).ToArray();

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
