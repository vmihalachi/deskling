using Deskling.Companion.Desktop;
using Deskling.Companion.Phrases;

namespace Deskling.Companion.Tests;

/// <summary>The pause before a sequence's next part: a beat after a spoken part, the bubble's time after a shown one.</summary>
public sealed class PhraseTimingTests
{
    [Fact]
    public void TheFirstPartHasNoPreviousOneSoItWaitsTheFloorOrTheBeat()
    {
        Assert.Equal(2, PhraseTiming.PartDelaySeconds(false, ""));
        Assert.Equal(PhraseTiming.PartBeatSeconds, PhraseTiming.PartDelaySeconds(true, ""));
    }

    [Fact]
    public void ASpokenPartWaitsTheBeat()
    {
        Assert.Equal(1.2, PhraseTiming.PartBeatSeconds);
        Assert.Equal(1.2, PhraseTiming.PartDelaySeconds(true, new string('a', 100)));
    }

    [Fact]
    public void AShownPartStaysAsLongAsItsBubble()
    {
        var setup = new string('a', 50);
        Assert.Equal(DesktopPresence.BubbleSeconds(setup), PhraseTiming.PartDelaySeconds(false, setup));
        Assert.Equal(2.5, PhraseTiming.PartDelaySeconds(false, setup));
    }

    [Fact]
    public void AShownPartFollowedByASpokenOneWaitsTheBubbleThenTheBeat()
    {
        var longPart = new string('a', 113);
        Assert.Equal(5, PhraseTiming.PartDelaySeconds(false, longPart));
        Assert.Equal(PhraseTiming.PartBeatSeconds, PhraseTiming.PartDelaySeconds(true, longPart));
    }

    [Fact]
    public void AZeroLengthBubbleStillHoldsTheFloor()
    {
        Assert.Equal(2, DesktopPresence.BubbleSeconds(""));
        Assert.Equal(DesktopPresence.BubbleSeconds(""), PhraseTiming.PartDelaySeconds(false, ""));
    }
}
