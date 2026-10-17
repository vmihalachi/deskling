using Laching.Companion.Rules;

namespace Laching.Companion.Figures;

/// <summary>
/// Where a character is at one moment: plain numbers and part ids, so a pose can be stored, compared, replayed from
/// the vectors and blended (the 0.6 s spring when the character wakes from Shh). Equal when every field is.
/// </summary>
/// <param name="HeadAngle">Degrees, clockwise, around the head's pivot: droop plus sway.</param>
/// <param name="HeadExtras">Parts that ride on the head under the eyes and mouth, such as cheeks.</param>
/// <param name="Overlays">Parts drawn last and never moved, such as the sleeping "zz".</param>
public sealed record Pose(
    Mood Mood,
    double HeadAngle,
    double Sink,
    double Pulse,
    string Eyes,
    string Mouth,
    IReadOnlyList<string> HeadExtras,
    IReadOnlyList<string> Overlays,
    bool Blinking
)
{
    /// <summary>Numbers interpolate; parts and flags switch to <paramref name="target"/>'s at the halfway point.</summary>
    public Pose Blended(Pose target, double progress)
    {
        var t = Math.Clamp(progress, 0, 1);
        if (t == 1)
            return target; // exactly, not 14 + (target − 14) × 1
        var pose = t < 0.5 ? this : target;
        return pose with
        {
            HeadAngle = HeadAngle + (target.HeadAngle - HeadAngle) * t,
            Sink = Sink + (target.Sink - Sink) * t,
            Pulse = Pulse + (target.Pulse - Pulse) * t,
        };
    }

    public bool Equals(Pose? other) =>
        other is not null
        && Mood == other.Mood
        && HeadAngle.Equals(other.HeadAngle)
        && Sink.Equals(other.Sink)
        && Pulse.Equals(other.Pulse)
        && Eyes == other.Eyes
        && Mouth == other.Mouth
        && HeadExtras.SequenceEqual(other.HeadExtras)
        && Overlays.SequenceEqual(other.Overlays)
        && Blinking == other.Blinking;

    public override int GetHashCode() => HashCode.Combine(Mood, HeadAngle, Sink, Pulse, Eyes, Mouth, Blinking);
}
