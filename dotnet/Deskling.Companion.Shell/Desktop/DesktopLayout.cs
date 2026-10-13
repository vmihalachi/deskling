using Deskling.Companion.Figures;
using Deskling.Companion.Settings;

namespace Deskling.Companion.Shell.Desktop;

/// <summary>
/// Where things go when a character stands on the desktop, from its art: the window size per <see cref="DesktopSize"/>
/// (the width follows the view box, with room under it for a contact shadow), and where the speech bubble's tail meets
/// the character (<see cref="HeadHeightRatio"/>, the bubble pivot's height in the view box; <see cref="BubbleTuck"/>,
/// how far the bubble tucks in past the character's empty sides). Pure numbers, in screen coordinates with y down (the
/// Mac's twin has y up).
/// </summary>
public sealed record DesktopLayout
{
    public DesktopLayout(
        CharacterArt art,
        double shadowHeight = 4,
        double bubbleMaxWidth = 240,
        double bubbleMargin = 4,
        double shadowWidthRatio = 0.42)
    {
        Aspect = art.ViewBox.Width / art.ViewBox.Height;
        HeadHeightRatio = art.BubblePoint.Y / art.ViewBox.Height;
        BubbleTuck = art.Rig.Bubble.Tuck;
        ShadowHeight = shadowHeight;
        BubbleMaxWidth = bubbleMaxWidth;
        BubbleMargin = bubbleMargin;
        ShadowWidthRatio = shadowWidthRatio;
    }

    /// <summary>Width over height of the art's view box.</summary>
    public double Aspect { get; }

    /// <summary>The bubble pivot's distance from the top, as a fraction of the character's height.</summary>
    public double HeadHeightRatio { get; }

    /// <summary>A fraction of the window's width the bubble overlaps it by.</summary>
    public double BubbleTuck { get; }

    /// <summary>Room under the character for the shadow.</summary>
    public double ShadowHeight { get; }

    /// <summary>The widest a bubble gets, tail included; longer lines wrap.</summary>
    public double BubbleMaxWidth { get; }

    /// <summary>Around the bubble's card, so its shadow isn't clipped by the window.</summary>
    public double BubbleMargin { get; }

    /// <summary>The contact shadow's width, as a share of the window's.</summary>
    public double ShadowWidthRatio { get; }

    /// <summary>The desktop window's size for <paramref name="size"/>.</summary>
    public Size WindowSize(DesktopSize size) => new(Math.Round(size.Height() * Aspect, MidpointRounding.AwayFromZero), size.Height() + ShadowHeight);

    /// <summary>The height (y down) of the bubble pivot in a character window at <paramref name="frame"/>.</summary>
    public double HeadY(Rect frame) => frame.Origin.Y + (frame.Size.Height - ShadowHeight) * HeadHeightRatio;

    /// <summary>The gap between the bubble and the character window <paramref name="width"/> wide: negative, so the bubble tucks in.</summary>
    public double BubbleGap(double width) => -width * BubbleTuck;

    /// <summary>The tail's tip below the top of a bubble window <paramref name="height"/> tall.</summary>
    public double BubbleTailY(double height) => BubbleMargin + BubbleMetrics.TailY(height - 2 * BubbleMargin);
}
