namespace Deskling.Companion.Shell.Desktop;

/// <summary>
/// The speech bubble's outline numbers (Swift's <c>BubbleShape</c> statics), shared by the desktop layout and the
/// bubble control: a rounded card with a triangular tail; a side tail is centered <see cref="TailDrop"/> below the top,
/// or halfway down a shorter card.
/// </summary>
public static class BubbleMetrics
{
    public const double Radius = 14;

    public const double TailLength = 10;

    public const double TailBase = 14;

    /// <summary>The side tail's center below the card's top, at most.</summary>
    public const double TailDrop = 25;

    /// <summary>Where a side tail's tip sits below the top of a card <paramref name="cardHeight"/> tall.</summary>
    public static double TailY(double cardHeight) => Math.Min(TailDrop, cardHeight / 2);
}
