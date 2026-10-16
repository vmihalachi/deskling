using Deskling.Windows.Companion.Desktop;

namespace Deskling.Windows.Companion.Tests.Desktop;

/// <summary>How the desktop window tells a click, a drag, a hold and a pinch apart, from each pointer's own positions.</summary>
public sealed class PressTrackerTests
{
    private const double Threshold = 6;

    /// <summary>Pixels per DIP, at 200 %.</summary>
    private const double Scale = 2;

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
    public void AHeldPressIsAHoldNotAClick()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        Assert.Null(press.Move(7, 102, 101, Threshold));
        press.Hold();
        Assert.Null(press.Move(7, 101, 102, Threshold));
        Assert.Equal(PressTracker.End.Hold, press.Up(7));
        Assert.False(press.IsPressed);
    }

    [Fact]
    public void ADragBeatsAHoldBeforeOrAfterIt()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Move(7, 120, 100, Threshold);
        press.Hold();
        Assert.Equal(PressTracker.End.Drag, press.Up(7));

        press.Down(7, 100, 100);
        press.Hold();
        Assert.Equal((0, 20), press.Move(7, 100, 120, Threshold));
        Assert.Equal(PressTracker.End.Drag, press.Up(7));
    }

    [Fact]
    public void APinchBeatsAHoldBeforeOrAfterIt()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Down(8, 130, 100);
        press.Hold();
        Assert.Equal(PressTracker.End.None, press.Up(8));
        Assert.Equal(PressTracker.End.None, press.Up(7));

        press.Down(7, 100, 100);
        press.Hold();
        press.Pinch();
        Assert.Equal(PressTracker.End.None, press.Up(7));
    }

    [Fact]
    public void ALostCaptureAfterAHoldIsNothing()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Hold();
        Assert.Equal(PressTracker.End.None, press.Lost(7));
        Assert.False(press.IsPressed);
    }

    [Fact]
    public void AHoldIsForgottenByTheNextPressAndIgnoredWithNoPress()
    {
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Hold();
        press.Up(7);
        press.Hold();
        Assert.Equal(PressTracker.Start.Press, press.Down(9, 50, 50));
        Assert.Equal(PressTracker.End.Click, press.Up(9));
    }

    [Fact]
    public void AFingerOrAPenGetsMoreRoomThanTheMouse()
    {
        Assert.Equal(DesktopWindow.DragThreshold, DesktopWindow.DragThresholdFor(mouse: true));
        Assert.Equal(DesktopWindow.TouchDragThreshold, DesktopWindow.DragThresholdFor(mouse: false));
        Assert.True(DesktopWindow.TouchDragThreshold > DesktopWindow.DragThreshold);
    }

    [Fact]
    public void AFingerThatDriftsWithinItsThresholdStillHoldsOrTaps()
    {
        var finger = DesktopWindow.DragThresholdFor(mouse: false) * Scale;
        var mouse = DesktopWindow.DragThresholdFor(mouse: true) * Scale;
        var press = new PressTracker();
        press.Down(7, 100, 100);
        // About 7.5 DIPs at 200 %: a drag for the mouse, a finger resting.
        Assert.Null(press.Move(7, 112, 109, finger));
        press.Hold();
        Assert.Null(press.Move(7, 106, 114, finger));
        Assert.Equal(PressTracker.End.Hold, press.Up(7));

        press.Down(7, 100, 100);
        Assert.Null(press.Move(7, 112, 109, finger));
        Assert.Equal(PressTracker.End.Click, press.Up(7));

        press.Down(7, 100, 100);
        Assert.Equal((12, 9), press.Move(7, 112, 109, mouse));
        Assert.Equal(PressTracker.End.Drag, press.Up(7));
    }

    [Fact]
    public void AFingerPastItsThresholdDragsEvenAfterAHold()
    {
        var finger = DesktopWindow.DragThresholdFor(mouse: false) * Scale;
        var press = new PressTracker();
        press.Down(7, 100, 100);
        press.Hold();
        Assert.Null(press.Move(7, 119, 100, finger));
        Assert.Equal((21, 0), press.Move(7, 121, 100, finger));
        Assert.Equal(PressTracker.End.Drag, press.Up(7));
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
