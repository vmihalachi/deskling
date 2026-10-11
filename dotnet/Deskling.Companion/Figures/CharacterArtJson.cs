using System.Text.Json;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Figures;

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
        var viewBox = Numbers(root.GetProperty("viewBox"));
        if (viewBox.Length != 2)
            throw new InvalidDataException("viewBox needs 2 numbers");
        var pivots = root.GetProperty("pivots").EnumerateObject().ToDictionary(p => p.Name, p => PointOf(p.Value, $"pivot '{p.Name}'"));
        var parts = root.GetProperty("parts").EnumerateObject().ToDictionary(p => p.Name, p => Content(p.Value, p.Name));
        var glyph = root.GetProperty("glyph");
        return new CharacterArt(
            new SkinId(root.GetProperty("skin").GetString()!),
            new Size(viewBox[0], viewBox[1]),
            pivots,
            parts,
            RigOf(root.GetProperty("rig")),
            glyph.GetProperty("size").GetDouble(),
            glyph.GetProperty("frames").EnumerateObject().ToDictionary(p => p.Name, p => (IReadOnlyList<PathShape>)ShapeList(p.Value))
        );
    }

    private static PartContent Content(JsonElement e, string name)
    {
        if (e.ValueKind == JsonValueKind.Array)
            return new PartContent.Vector(ShapeList(e));
        var frame = Numbers(e.GetProperty("frame"));
        if (frame.Length != 4)
            throw new InvalidDataException($"part '{name}': frame needs 4 numbers");
        return new PartContent.Image(e.GetProperty("image").GetString()!, new Rect(frame[0], frame[1], frame[2], frame[3]));
    }

    private static PathShape[] ShapeList(JsonElement e) => e.EnumerateArray().Select(ShapeOf).ToArray();

    private static PathShape ShapeOf(JsonElement e) =>
        new(
            e.GetProperty("d").EnumerateArray().Select(CommandOf).ToArray(),
            Role(e, "fill"),
            Role(e, "stroke"),
            e.GetProperty("strokeWidth").GetDouble(),
            e.GetProperty("opacity").GetDouble(),
            e.GetProperty("evenOdd").GetBoolean()
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
        var blink = e.GetProperty("blink");
        var bubble = e.GetProperty("bubble");
        return new Rig(
            e.GetProperty("period").GetDouble(),
            e.GetProperty("stillTime").GetDouble(),
            Strings(e.GetProperty("always")),
            Group(e.GetProperty("head")),
            Group(e.GetProperty("stem")),
            Group(e.GetProperty("pulse")),
            new Blink(blink.GetProperty("duration").GetDouble(), Numbers(blink.GetProperty("intervals")), blink.GetProperty("closedEyes").GetString()!),
            e.GetProperty("moods").EnumerateObject().ToDictionary(p => MoodNamed(p.Name), p => MoodRigOf(p.Value)),
            e.GetProperty("glyph").EnumerateObject().ToDictionary(p => MoodNamed(p.Name), p => (IReadOnlyList<string>)Strings(p.Value)),
            new BubbleAnchor(bubble.GetProperty("pivot").GetString()!, bubble.GetProperty("tuck").GetDouble())
        );
    }

    private static MoodRig MoodRigOf(JsonElement e) =>
        new(
            e.GetProperty("eyes").GetString()!,
            Strings(e.GetProperty("mouth")),
            e.GetProperty("mouthRate").GetDouble(),
            Strings(e.GetProperty("headExtras")),
            Strings(e.GetProperty("overlays")),
            e.GetProperty("blink").GetBoolean(),
            e.GetProperty("droop").GetDouble(),
            e.GetProperty("sink").GetDouble(),
            e.GetProperty("sway").GetDouble(),
            e.GetProperty("swayPeriod").GetDouble(),
            e.GetProperty("pulse").GetDouble()
        );

    private static RigGroup Group(JsonElement e) =>
        new(e.GetProperty("pivot").GetString()!, Strings(e.GetProperty("parts")), e.GetProperty("follow").GetDouble());

    private static Mood MoodNamed(string name) => Moods.Parse(name) ?? throw new InvalidDataException($"unknown mood '{name}'");

    private static Point PointOf(JsonElement e, string what)
    {
        var numbers = Numbers(e);
        return numbers.Length == 2 ? new Point(numbers[0], numbers[1]) : throw new InvalidDataException($"{what} needs 2 numbers");
    }

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
            foreach (var name in new[] { m.Eyes }.Concat(m.Mouth).Concat(m.HeadExtras).Concat(m.Overlays))
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
