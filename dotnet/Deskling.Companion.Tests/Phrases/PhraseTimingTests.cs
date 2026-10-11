using Deskling.Companion.Desktop;
using Deskling.Companion.Phrases;

namespace Deskling.Companion.Tests;

public sealed class PhraseTimingTests
{
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
        Assert.Equal(2, PhraseTiming.PartDelaySeconds(false, ""));
    }
}
