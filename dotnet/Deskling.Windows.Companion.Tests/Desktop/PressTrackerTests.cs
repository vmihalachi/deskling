using Deskling.Windows.Companion.Desktop;

namespace Deskling.Windows.Companion.Tests.Desktop;

/// <summary>How the desktop window tells a click, a drag and a pinch apart, from each pointer's own positions.</summary>
public sealed class PressTrackerTests
{
    private const double Threshold = 6;

    [Fact]
    public void ATapIsAClick()
    {
        var press = new PressTracker();
        Assert.Equal(PressTracker.Start.Press, press.Down(7, 100, 100));
        Assert.Null(press.Move(7, 102, 101, Threshold));
        Assert.Equal(PressTracker.End.Click, press.Up(7));
        Assert.False(press.IsPressed);
    }

    [Fact]
    public void OneFingerPastTheThresholdDragsFromWhereItWentDown()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        Assert.Null(press.Move(7, 103, 100, Threshold));
        Assert.Equal((-40, 10), press.Move(7, 60, 110, Threshold));
        // Once dragging, every move counts, even back inside the threshold.
        Assert.Equal((1, 0), press.Move(7, 101, 100, Threshold));
        Assert.Equal(PressTracker.End.Drag, press.Up(7));
    }

    [Fact]
    public void ASecondFingerMakesAPinchThatNeverClicksOrDrags()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        Assert.Equal(PressTracker.Start.Joined, press.Down(8, 130, 100));
        Assert.Null(press.Move(7, 60, 100, Threshold));
        Assert.Null(press.Move(8, 170, 100, Threshold));
        Assert.Equal(PressTracker.End.None, press.Up(7));
        Assert.True(press.IsPressed);
        Assert.Equal(PressTracker.End.None, press.Up(8));
        Assert.False(press.IsPressed);
    }

    [Fact]
    public void APinchsScaleEndsADragAndTheClick()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Move(7, 120, 100, Threshold);
        Assert.Equal(PressTracker.End.Drag, press.Pinch());
        Assert.Null(press.Move(7, 140, 100, Threshold));
        Assert.Equal(PressTracker.End.None, press.Up(7));

        press.Down(9, 100, 100);
        Assert.Equal(PressTracker.End.None, press.Pinch());
        Assert.Equal(PressTracker.End.None, press.Up(9));
    }

    [Fact]
    public void ASecondFingerMidDragEndsTheDrag()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Move(7, 120, 100, Threshold);
        Assert.Equal(PressTracker.Start.EndsDrag, press.Down(8, 200, 100));
        Assert.Equal(PressTracker.End.None, press.Up(8));
        Assert.Equal(PressTracker.End.None, press.Up(7));
    }

    [Fact]
    public void ALostCaptureEndsADragAndNeverClicks()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Move(7, 100, 130, Threshold);
        Assert.Equal(PressTracker.End.Drag, press.Lost(7));
        Assert.False(press.IsPressed);

        press.Down(7, 100, 100);
        Assert.Equal(PressTracker.End.None, press.Lost(7));
        Assert.Equal(PressTracker.End.None, press.Up(7));
    }

    [Fact]
    public void AnotherPointersMovesAndAnUnknownPointerAreIgnored()
    {
        var press = new PressTracker();
        Assert.Null(press.Move(7, 200, 200, Threshold));
        Assert.Equal(PressTracker.End.None, press.Up(7));
        press.Down(7, 100, 100);
        Assert.Null(press.Move(3, 300, 300, Threshold));
        Assert.Equal(PressTracker.End.Click, press.Up(7));
    }

    [Fact]
    public void ANewPressAfterAPinchStartsClean()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Down(8, 120, 100);
        press.Up(7);
        press.Up(8);
        Assert.Equal(PressTracker.Start.Press, press.Down(9, 50, 50));
        Assert.Equal(PressTracker.End.Click, press.Up(9));
    }
}
