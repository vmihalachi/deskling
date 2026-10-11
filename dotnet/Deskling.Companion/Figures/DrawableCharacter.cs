using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Figures;

/// <summary>
/// A character the app can draw, whichever skin (Swift's <c>DrawableCharacter</c>). Geometry only: the app fills and
/// strokes shapes with its palette and draws image parts from its assets. A pose comes from the mood and the time in
/// seconds (or the still pose under reduced motion); the layers come from the pose. The tray / menu bar glyph is a
/// vector template in a <see cref="GlyphSize"/> square for every skin.
/// </summary>
public interface IDrawableCharacter
{
    SkinId Skin { get; }

    Size ViewBox { get; }

    double GlyphSize { get; }

    Pose Pose(Mood mood, double time);

    /// <summary>The pose shown when nothing moves: no blink, the first mouth.</summary>
    Pose StillPose(Mood mood);

    /// <summary>
    /// The parts to draw, in order: <c>always</c> parts under the pulse, head or stem transform (or none), then the
    /// head extras, eyes and mouth under the head, then the overlays untransformed.
    /// </summary>
    IReadOnlyList<PlacedPart> Layers(Pose pose);

    /// <summary>The vector convenience: every shape part flattened through its transform; image parts are skipped.</summary>
    IReadOnlyList<PathShape> Shapes(Pose pose);

    /// <summary>Glyph frames: the mood's frames cycle (two while talking).</summary>
    IReadOnlyList<PathShape> Glyph(Mood mood, int frame);

    int GlyphFrameCount(Mood mood);
}

/// <summary>One part placed for drawing: its content in view-box units and the transform the rig gives it.</summary>
public sealed record PlacedPart(string Name, Transform Transform, PartContent Content);
