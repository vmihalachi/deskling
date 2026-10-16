using Deskling.Windows.Companion.Views;

namespace Deskling.Windows.Companion.Tests.Views;

public sealed class BubbleCountdownDialTests
{
    [Theory]
    [InlineData(10, 0, 10, 1)]
    [InlineData(10, 4, 6, 0.6)]
    [InlineData(10, 10, 0, 0)]
    [InlineData(10, 25, 0, 0)]
    // Stamped by a clock hours ahead of the wall clock (the fixture's skip), or the wall clock moved back: a full dial,
    // never a share above 1, which the compositor refuses as a TrimStart below 0.
    [InlineData(10, -7200, 10, 1)]
    [InlineData(10, -0.5, 10, 1)]
    [InlineData(0, 3, 0, 0)]
    [InlineData(-1, 3, 0, 0)]
    public void TheDialStaysBetweenEmptyAndFull(double seconds, double elapsed, double left, double remaining)
    {
        var dial = SpeechBubble.DialLeft(seconds, TimeSpan.FromSeconds(elapsed));
        Assert.Equal(left, dial.Left, 9);
        Assert.Equal(remaining, dial.Remaining, 9);
        Assert.InRange(1 - dial.Remaining, 0, 1);
    }
}
