using Deskling.Windows.Companion.Views;

namespace Deskling.Windows.Companion.Tests.Views;

/// <summary>Where content sits in a window: under the caption buttons' band, which the window keeps above the view.</summary>
public sealed class WindowInsetTests
{
    [Fact]
    public void TheViewsInsetStartsUnderTheBandAsTheMacsUnderItsTitleBar()
    {
        // The band is the window's (WindowFrame's first row), outside the view's scroller, so it isn't in the margin.
        Assert.Equal(WindowInset.Top, WindowInset.Content.Top);
        Assert.Equal(WindowInset.Top, WindowInset.ContentOverActionBar.Top);
        Assert.Equal(32, WindowInset.TitleBar);
    }

    [Fact]
    public void TheSidesAndTheBottomAreTheSharedFrame()
    {
        Assert.Equal(WindowInset.Side, WindowInset.Content.Left);
        Assert.Equal(WindowInset.Side, WindowInset.Content.Right);
        Assert.Equal(WindowInset.Bottom, WindowInset.Content.Bottom);
        Assert.Equal(Space.Group, WindowInset.ContentOverActionBar.Bottom);
    }
}
