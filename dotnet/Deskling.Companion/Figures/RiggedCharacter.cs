using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Figures;

/// <summary>
/// Any character drawn from compiled art: the rig picks parts per mood and moves the head. A new mascot or skin is a
/// new art folder run through <c>tools/character.py</c>, not new code. Pure; <c>conformance/companion/characters</c>
/// pins it. Never reads assets: image parts are placed, and the app draws them.
/// </summary>
public sealed class RiggedCharacter(CharacterArt art) : IDrawableCharacter
{
    public CharacterArt Art { get; } = art;

    public SkinId Skin => Art.Skin;

    public Size ViewBox => Art.ViewBox;

    public double GlyphSize => Art.GlyphSize;

    public Pose Pose(Mood mood, double time)
    {
        var rig = Art.Rig;
        var m = MoodRig(mood);
        var swayPhase = Wrapped(time, m.SwayPeriod) / m.SwayPeriod;
        var angle = m.Droop + m.Sway * Math.Sin(2 * Math.PI * swayPhase);
        var mouthIndex = m.Mouth.Count > 1 ? (int)Math.Floor(Wrapped(time, m.Mouth.Count / m.MouthRate) * m.MouthRate) : 0;
        var pulse = m.Pulse == 0 ? 0 : m.Pulse * Math.Abs(Math.Sin(Math.PI * Wrapped(time, 2 / m.MouthRate) * m.MouthRate));
        var blinking = m.Blink && IsBlinking(time);
        return new Pose(
            mood,
            angle,
            m.Sink,
            pulse,
            blinking ? rig.Blink.ClosedEyes : m.Eyes,
            m.Mouth[mouthIndex % m.Mouth.Count],
            m.HeadExtras,
            m.Overlays,
            blinking
        );
    }

    public Pose StillPose(Mood mood)
    {
        var m = MoodRig(mood);
        return Pose(mood, Art.Rig.StillTime) with { Blinking = false, Eyes = m.Eyes, Mouth = m.Mouth[0] };
    }

    public IReadOnlyList<PlacedPart> Layers(Pose pose)
    {
        var rig = Art.Rig;
        var head = Transform.Rotation(pose.HeadAngle, Pivot(rig.Head.Pivot)).Then(Transform.Translation(0, pose.Sink));
        var stem = Transform.Rotation(pose.HeadAngle * rig.Stem.Follow, Pivot(rig.Stem.Pivot));
        var pulse = Transform.Scale(1 + pose.Pulse, Pivot(rig.Pulse.Pivot)).Then(head);
        var layers = new List<PlacedPart>();
        foreach (var name in rig.Always)
        {
            var transform =
                rig.Pulse.Parts.Contains(name) ? pulse
                : rig.Head.Parts.Contains(name) ? head
                : rig.Stem.Parts.Contains(name) ? stem
                : Transform.Identity;
            layers.Add(new PlacedPart(name, transform, Part(name)));
        }
        foreach (var name in pose.HeadExtras.Concat([pose.Eyes, pose.Mouth]))
            layers.Add(new PlacedPart(name, head, Part(name)));
        foreach (var name in pose.Overlays)
            layers.Add(new PlacedPart(name, Transform.Identity, Part(name)));
        return layers;
    }

    public IReadOnlyList<PathShape> Shapes(Pose pose) =>
        Layers(pose)
            .SelectMany(layer =>
                layer.Content is PartContent.Vector vector ? vector.Shapes.Select(s => s.Transformed(layer.Transform)) : []
            )
            .ToArray();

    public IReadOnlyList<PathShape> Glyph(Mood mood, int frame)
    {
        if (!Art.Rig.Glyph.TryGetValue(mood, out var frames) || frames.Count == 0)
            return [];
        var index = ((frame % frames.Count) + frames.Count) % frames.Count;
        return Art.GlyphFrames.TryGetValue(frames[index], out var shapes) ? shapes : [];
    }

    public int GlyphFrameCount(Mood mood) => Art.Rig.Glyph.TryGetValue(mood, out var frames) ? frames.Count : 0;

    private MoodRig MoodRig(Mood mood) =>
        Art.Rig.Moods.TryGetValue(mood, out var m) ? m : throw new InvalidOperationException($"{Art.Skin} has no rig for {mood}");

    private PartContent Part(string name) =>
        Art.Parts.TryGetValue(name, out var content) ? content : throw new InvalidOperationException($"{Art.Skin} has no part {name}");

    private Point Pivot(string name) =>
        Art.Pivots.TryGetValue(name, out var point) ? point : throw new InvalidOperationException($"{Art.Skin} has no pivot {name}");

    /// <summary>Whether <paramref name="time"/> falls in a blink: the last seconds of each interval, cycling.</summary>
    private bool IsBlinking(double time)
    {
        var blink = Art.Rig.Blink;
        var t = Wrapped(time, blink.Intervals.Sum());
        var end = 0.0;
        foreach (var interval in blink.Intervals)
        {
            end += interval;
            if (t >= end - blink.Duration && t < end)
                return true;
        }
        return false;
    }

    /// <summary><paramref name="value"/> modulo <paramref name="period"/>, in [0, period), so long running times stay exact.</summary>
    internal static double Wrapped(double value, double period)
    {
        if (!(period > 0) || double.IsInfinity(period))
            return value;
        var r = value % period;
        return r < 0 ? r + period : r;
    }
}
