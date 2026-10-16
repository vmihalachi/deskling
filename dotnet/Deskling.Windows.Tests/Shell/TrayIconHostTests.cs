using Deskling.Windows.Shell;
using Windows.Graphics;

namespace Deskling.Windows.Tests.Shell;

/// <summary>The tray menu's words and how the host tells a click from the keyboard.</summary>
public sealed class TrayIconHostTests
{
    [Fact]
    public void WithoutAccessKeysEveryAmpersandShowsAsWritten()
    {
        Assert.Equal("Say something", TrayIconHost.MenuText("Say something", accessKeys: false));
        Assert.Equal("Salt && pepper", TrayIconHost.MenuText("Salt & pepper", accessKeys: false));
        Assert.Equal("&&&&Say", TrayIconHost.MenuText("&&Say", accessKeys: false));
    }

    [Fact]
    public void WithAccessKeysTheWordsGoToTheMenuAsWritten()
    {
        // A single & marks the key; && shows one &.
        Assert.Equal("&Say something", TrayIconHost.MenuText("&Say something", accessKeys: true));
        Assert.Equal("S&hh", TrayIconHost.MenuText("S&hh", accessKeys: true));
        Assert.Equal("Salt && &pepper", TrayIconHost.MenuText("Salt && &pepper", accessKeys: true));
    }

    [Fact]
    public void TheWordsWithoutTheirMarksReadAsTheMenuShowsThem()
    {
        Assert.Equal("Say something", TrayIconHost.WithoutAccessKeys("&Say something"));
        Assert.Equal("Shh", TrayIconHost.WithoutAccessKeys("S&hh"));
        Assert.Equal("Salt & pepper", TrayIconHost.WithoutAccessKeys("Salt && &pepper"));
        Assert.Equal("&&", TrayIconHost.WithoutAccessKeys("&&&&"));
        Assert.Equal("Quit", TrayIconHost.WithoutAccessKeys("Quit&"));
        Assert.Equal("Plain", TrayIconHost.WithoutAccessKeys("Plain"));
    }

    [Fact]
    public void APointerOnTheIconIsAClickAndOneElsewhereIsTheKeyboard()
    {
        var icon = new RectInt32(1800, 1040, 24, 24);
        var anchor = new PointInt32(1812, 1052);

        Assert.True(TrayIconHost.IsPointerOn(icon, anchor, new PointInt32(1810, 1050)));
        // A few pixels off the edge still counts, as the pointer may have moved since the click.
        Assert.True(TrayIconHost.IsPointerOn(icon, anchor, new PointInt32(1830, 1068)));
        Assert.False(TrayIconHost.IsPointerOn(icon, anchor, new PointInt32(900, 500)));
        Assert.False(TrayIconHost.IsPointerOn(icon, anchor, new PointInt32(1812, 1000)));
    }

    [Fact]
    public void WithoutTheIconsBoundsTheClicksPointDecides()
    {
        var anchor = new PointInt32(1812, 1052);

        Assert.True(TrayIconHost.IsPointerOn(null, anchor, new PointInt32(1815, 1049)));
        Assert.False(TrayIconHost.IsPointerOn(null, anchor, new PointInt32(1700, 1052)));
    }
}
