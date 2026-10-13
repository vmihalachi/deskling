using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;

namespace Deskling.Companion.Figures;

/// <summary>
/// A character's compiled art and rig: what <c>tools/character.py</c> makes of a skin folder. The Swift app's value is
/// generated code; a .NET app loads the same data from <c>characters/&lt;skin&gt;.json</c> with <see cref="Parse"/>.
/// Vector parts are shapes; raster parts are images from the app's assets.
/// </summary>
/// <param name="GlyphFrames">The tray / menu bar frames: a vector line drawing in <see cref="PaintRole.Ink"/> for every skin.</param>
public sealed partial record CharacterArt(
    SkinId Skin,
    Size ViewBox,
    IReadOnlyDictionary<string, Point> Pivots,
    IReadOnlyDictionary<string, PartContent> Parts,
    Rig Rig,
    double GlyphSize,
    IReadOnlyDictionary<string, IReadOnlyList<PathShape>> GlyphFrames
)
{
    /// <summary>
    /// Where a speech bubble's tail aims, in view-box units: the rig's bubble pivot (the origin if the art lacks it,
    /// which <see cref="Parse"/> and the compiler both refuse).
    /// </summary>
    public Point BubblePoint => Pivots.TryGetValue(Rig.Bubble.Pivot, out var point) ? point : new Point(0, 0);
}

/// <summary>How a character moves: which parts each mood shows and how its head droops, sways, pulses and blinks.</summary>
/// <param name="Always">Parts every mood draws, in drawing order.</param>
/// <param name="Head">Droops and sways around its pivot; the mood's head extras, eyes and mouth ride on it.</param>
/// <param name="Stem">Follows the head by <see cref="RigGroup.Follow"/> of its angle, around its own pivot.</param>
/// <param name="Pulse">Pulses (scales) around its pivot while talking, then moves with the head.</param>
/// <param name="Glyph">Glyph frame ids per mood.</param>
public sealed record Rig(
    double Period,
    double StillTime,
    IReadOnlyList<string> Always,
    RigGroup Head,
    RigGroup Stem,
    RigGroup Pulse,
    Blink Blink,
    IReadOnlyDictionary<Mood, MoodRig> Moods,
    IReadOnlyDictionary<Mood, IReadOnlyList<string>> Glyph,
    BubbleAnchor Bubble
);

public sealed record RigGroup(string Pivot, IReadOnlyList<string> Parts, double Follow);

/// <summary>The eyes close for <see cref="Duration"/> seconds at the end of each interval, cycling through the intervals.</summary>
public sealed record Blink(double Duration, IReadOnlyList<double> Intervals, string ClosedEyes);

/// <summary>
/// Where a speech bubble aims (a pivot) and how far it tucks in past the character's empty sides, as a fraction of the
/// character's width.
/// </summary>
public sealed record BubbleAnchor(string Pivot, double Tuck);

/// <param name="Mouth">One mouth, or several that alternate <paramref name="MouthRate"/> times a second.</param>
/// <param name="HeadExtras">Parts that ride on the head under the eyes and mouth, such as cheeks.</param>
/// <param name="Overlays">Parts drawn last and never moved, such as the sleeping "zz".</param>
/// <param name="Droop">Degrees, clockwise, around the head's pivot.</param>
/// <param name="Sway">Sway amplitude in degrees over <paramref name="SwayPeriod"/> seconds.</param>
/// <param name="Pulse">Pulse amplitude (0.03 = 3 %).</param>
/// <param name="RestMouth">
/// The closed mouth between words when a recorded clip's envelope drives the mouth (level 0); null uses the first of
/// <paramref name="Mouth"/>. The talking mouths are listed from the least open to the most.
/// </param>
public sealed record MoodRig(
    string Eyes,
    IReadOnlyList<string> Mouth,
    double MouthRate,
    IReadOnlyList<string> HeadExtras,
    IReadOnlyList<string> Overlays,
    bool Blink,
    double Droop,
    double Sink,
    double Sway,
    double SwayPeriod,
    double Pulse,
    string? RestMouth = null
);
