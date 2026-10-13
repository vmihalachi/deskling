using System.Globalization;
using System.Text;
using System.Text.Json;
using Deskling.Companion.Figures;
using Deskling.Companion.Rules;

namespace Deskling.Testing.Conformance;

/// <summary>
/// Replays a <c>characters/&lt;skin&gt;.json</c> or <c>characters/&lt;skin&gt;-poses.json</c> (Swift's
/// <c>CharacterPoseVector.verify</c>) against the character of that skin. The art file must parse
/// (<see cref="CharacterArt.Parse"/>) to the same art; the poses file's poses, still poses, glyph frames, resolved
/// <c>shapes</c> (when the skin has vector parts) and placed <c>layers</c> (when it has image parts) must come out of
/// <see cref="RiggedCharacter"/> within the vector's 6-decimal rounding, and so must its <c>voiced</c> poses (present
/// exactly when the talking rig has a rest mouth).
/// </summary>
public static class CharacterReplay
{
    /// <summary>The vectors round to 6 decimals; this leaves room for that and for the math library.</summary>
    public const double Tolerance = 1.5e-6;

    /// <summary>The problems found, empty when the character agrees with the file.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file, IReadOnlyList<RiggedCharacter> characters)
    {
        if (!Path.GetFileNameWithoutExtension(file).EndsWith("-poses", StringComparison.Ordinal))
        {
            var art = CharacterArt.Parse(json);
            var match = characters.FirstOrDefault(c => c.Skin == art.Skin);
            if (match is null)
                return [$"{file}: no character for skin {art.Skin}"];
            return Describe(art) == Describe(match.Art) ? [] : [$"{file}: the art differs from {art.Skin}'s"];
        }
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var skin = root.GetProperty("skin").GetString();
        var character = characters.FirstOrDefault(c => c.Skin.RawValue == skin);
        if (character is null)
            return [$"{file}: no character for skin {skin}"];
        var problems = new List<string>();
        var poses = root.GetProperty("poses").EnumerateArray().ToArray();
        if (poses.Length != Moods.All.Count * 4 + 1)
            problems.Add($"{file}: {poses.Length} poses, expected {Moods.All.Count * 4 + 1}");
        foreach (var c in poses)
        {
            var (mood, time) = (MoodOf(c, file), c.GetProperty("time").GetDouble());
            if (!SamePose(c.GetProperty("pose"), character.Pose(mood, time)))
                problems.Add($"{file}: {mood.RawValue()} at {Number(time)} differs");
        }
        foreach (var c in root.GetProperty("still").EnumerateArray())
        {
            var mood = MoodOf(c, file);
            if (!SamePose(c.GetProperty("pose"), character.StillPose(mood)))
                problems.Add($"{file}: the still pose of {mood.RawValue()} differs");
        }
        var hasShapes = character.Art.Parts.Values.Any(p => p is PartContent.Vector);
        var hasImages = character.Art.Parts.Values.Any(p => p is PartContent.Image);
        if (VectorSupport.Has(root, "shapes", out var shapes) != hasShapes)
            problems.Add($"{file}: shapes should be {(hasShapes ? "present" : "absent")}");
        else if (hasShapes)
        {
            foreach (var c in shapes.EnumerateArray())
            {
                var (mood, time) = (MoodOf(c, file), c.GetProperty("time").GetDouble());
                if (!SameShapes(c.GetProperty("shapes"), character.Shapes(character.Pose(mood, time))))
                    problems.Add($"{file}: the shapes of {mood.RawValue()} at {Number(time)} differ");
            }
        }
        if (VectorSupport.Has(root, "layers", out var layers) != hasImages)
            problems.Add($"{file}: layers should be {(hasImages ? "present" : "absent")}");
        else if (hasImages)
        {
            foreach (var c in layers.EnumerateArray())
            {
                var (mood, time) = (MoodOf(c, file), c.GetProperty("time").GetDouble());
                if (!SameLayers(c.GetProperty("layers"), character.Layers(character.Pose(mood, time))))
                    problems.Add($"{file}: the layers of {mood.RawValue()} at {Number(time)} differ");
            }
        }
        var hasRest = character.Art.Rig.Moods.TryGetValue(Mood.Talking, out var talking) && talking.RestMouth is not null;
        if (VectorSupport.Has(root, "voiced", out var voiced) != hasRest)
            problems.Add($"{file}: voiced poses should be {(hasRest ? "present" : "absent")}");
        else if (hasRest)
        {
            if (voiced.GetArrayLength() != 4)
                problems.Add($"{file}: {voiced.GetArrayLength()} voiced poses, expected 4");
            foreach (var c in voiced.EnumerateArray())
            {
                var (level, time) = (c.GetProperty("level").GetInt32(), c.GetProperty("time").GetDouble());
                if (!SamePose(c.GetProperty("pose"), character.Pose(Mood.Talking, time, level)))
                    problems.Add($"{file}: the voiced pose at level {level} differs");
            }
        }
        var glyphs = root.GetProperty("glyph").EnumerateArray().ToArray();
        if (glyphs.Length != Moods.All.Sum(character.GlyphFrameCount))
            problems.Add($"{file}: {glyphs.Length} glyph frames, expected {Moods.All.Sum(character.GlyphFrameCount)}");
        foreach (var c in glyphs)
        {
            var (mood, frame) = (MoodOf(c, file), c.GetProperty("frame").GetInt32());
            if (!SameShapes(c.GetProperty("shapes"), character.Glyph(mood, frame)))
                problems.Add($"{file}: glyph {mood.RawValue()} frame {frame} differs");
        }
        return problems;
    }

    private static Mood MoodOf(JsonElement e, string file) =>
        Moods.Parse(e.GetProperty("mood").GetString()) ?? throw new InvalidDataException($"{file}: unknown mood {e.GetProperty("mood")}");

    private static bool Close(double expected, double actual) => Math.Abs(expected - actual) <= Tolerance;

    private static bool SamePose(JsonElement e, Pose pose) =>
        Close(e.GetProperty("headAngle").GetDouble(), pose.HeadAngle)
        && Close(e.GetProperty("sink").GetDouble(), pose.Sink)
        && Close(e.GetProperty("pulse").GetDouble(), pose.Pulse)
        && e.GetProperty("eyes").GetString() == pose.Eyes
        && e.GetProperty("mouth").GetString() == pose.Mouth
        && VectorSupport.Strings(e.GetProperty("headExtras")).SequenceEqual(pose.HeadExtras)
        && VectorSupport.Strings(e.GetProperty("overlays")).SequenceEqual(pose.Overlays)
        && e.GetProperty("blinking").GetBoolean() == pose.Blinking;

    private static bool SameLayers(JsonElement e, IReadOnlyList<PlacedPart> layers)
    {
        var expected = e.EnumerateArray().ToArray();
        if (expected.Length != layers.Count)
            return false;
        for (var i = 0; i < expected.Length; i++)
        {
            var t = layers[i].Transform.Affine;
            var numbers = expected[i].GetProperty("transform").EnumerateArray().Select(n => n.GetDouble()).ToArray();
            var same =
                expected[i].GetProperty("name").GetString() == layers[i].Name
                && expected[i].GetProperty("kind").GetString() == layers[i].Content.Kind
                && numbers.Length == 6
                && Close(numbers[0], t.A)
                && Close(numbers[1], t.B)
                && Close(numbers[2], t.C)
                && Close(numbers[3], t.D)
                && Close(numbers[4], t.Tx)
                && Close(numbers[5], t.Ty);
            if (!same)
                return false;
        }
        return true;
    }

    /// <summary>Shapes as the poses file writes them: each path one string, "M x y C x1 y1 x2 y2 x y … Z".</summary>
    private static bool SameShapes(JsonElement e, IReadOnlyList<PathShape> shapes)
    {
        var expected = e.EnumerateArray().ToArray();
        if (expected.Length != shapes.Count)
            return false;
        for (var i = 0; i < expected.Length; i++)
        {
            var (x, a) = (expected[i], shapes[i]);
            var same =
                VectorSupport.OptionalString(x, "fill") == a.Fill?.RawValue
                && VectorSupport.OptionalString(x, "stroke") == a.Stroke?.RawValue
                && Close(x.GetProperty("strokeWidth").GetDouble(), a.StrokeWidth)
                && Close(x.GetProperty("opacity").GetDouble(), a.Opacity)
                && x.GetProperty("evenOdd").GetBoolean() == a.EvenOdd
                && SamePath(x.GetProperty("d").GetString()!, a.Path);
            if (!same)
                return false;
        }
        return true;
    }

    private static bool SamePath(string d, IReadOnlyList<PathCommand> path)
    {
        var tokens = d.Length == 0 ? [] : d.Split(' ');
        var position = 0;
        foreach (var command in path)
        {
            var op = command switch
            {
                PathCommand.Move => "M",
                PathCommand.Line => "L",
                PathCommand.Cubic => "C",
                _ => "Z",
            };
            if (position >= tokens.Length || tokens[position++] != op)
                return false;
            foreach (var point in command.Points)
            {
                if (position + 1 >= tokens.Length)
                    return false;
                var px = double.Parse(tokens[position++], CultureInfo.InvariantCulture);
                var py = double.Parse(tokens[position++], CultureInfo.InvariantCulture);
                if (!Close(px, point.X) || !Close(py, point.Y))
                    return false;
            }
        }
        return position == tokens.Length;
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>The art as one canonical text (keys sorted, numbers round-tripped), so two arts compare by value.</summary>
    internal static string Describe(CharacterArt art)
    {
        var text = new StringBuilder();
        void Line(string s) => text.Append(s).Append('\n');
        string N(double v) => Number(v);
        string L(IEnumerable<string> items) => "[" + string.Join(",", items) + "]";
        string Shape(PathShape s) =>
            $"{s.Fill?.RawValue}|{s.Stroke?.RawValue}|{N(s.StrokeWidth)}|{N(s.Opacity)}|{s.EvenOdd}|"
            + string.Join(" ", s.Path.Select(c => c.GetType().Name + string.Concat(c.Points.Select(p => $" {N(p.X)} {N(p.Y)}"))));
        Line($"skin {art.Skin} viewBox {N(art.ViewBox.Width)} {N(art.ViewBox.Height)} glyph {N(art.GlyphSize)}");
        foreach (var (name, p) in art.Pivots.OrderBy(p => p.Key, StringComparer.Ordinal))
            Line($"pivot {name} {N(p.X)} {N(p.Y)}");
        foreach (var (name, content) in art.Parts.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            Line(
                content switch
                {
                    PartContent.Vector v => $"part {name} " + string.Join(";", v.Shapes.Select(Shape)),
                    PartContent.Image i => $"part {name} image {i.Asset} {N(i.Frame.Origin.X)} {N(i.Frame.Origin.Y)} {N(i.Frame.Size.Width)} {N(i.Frame.Size.Height)}",
                    _ => $"part {name} ?",
                }
            );
        }
        foreach (var (name, shapes) in art.GlyphFrames.OrderBy(p => p.Key, StringComparer.Ordinal))
            Line($"frame {name} " + string.Join(";", shapes.Select(Shape)));
        var rig = art.Rig;
        string G(RigGroup g) => $"{g.Pivot} {L(g.Parts)} {N(g.Follow)}";
        Line($"rig {N(rig.Period)} {N(rig.StillTime)} {L(rig.Always)} head {G(rig.Head)} stem {G(rig.Stem)} pulse {G(rig.Pulse)}");
        Line($"blink {N(rig.Blink.Duration)} {L(rig.Blink.Intervals.Select(N))} {rig.Blink.ClosedEyes} bubble {rig.Bubble.Pivot} {N(rig.Bubble.Tuck)}");
        foreach (var (mood, m) in rig.Moods.OrderBy(p => p.Key))
        {
            Line(
                $"mood {mood.RawValue()} {m.Eyes} {L(m.Mouth)} {N(m.MouthRate)} {L(m.HeadExtras)} {L(m.Overlays)} {m.Blink} "
                    + $"{N(m.Droop)} {N(m.Sink)} {N(m.Sway)} {N(m.SwayPeriod)} {N(m.Pulse)} {m.RestMouth}"
            );
        }
        foreach (var (mood, frames) in rig.Glyph.OrderBy(p => p.Key))
            Line($"glyph {mood.RawValue()} {L(frames)}");
        return text.ToString();
    }
}
