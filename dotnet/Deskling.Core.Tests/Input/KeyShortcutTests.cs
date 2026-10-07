using Deskling.Core.Input;

namespace Deskling.Core.Tests;

public sealed class KeyShortcutTests
{
    [Fact]
    public void TestDisplayStringOrdersModifiersLikeTheMenuBar()
    {
        var shortcut = new KeyShortcut(11, KeyShortcut.Control | KeyShortcut.Option | KeyShortcut.Command, "B");
        Assert.Equal("⌃⌥⌘B", shortcut.DisplayString);
        Assert.Equal("⇧⌘S", new KeyShortcut(1, KeyShortcut.Shift | KeyShortcut.Command, "S").DisplayString);
        Assert.Equal("F5", new KeyShortcut(96, 0, "F5").DisplayString);
    }

    [Fact]
    public void TestValidityNeedsCommandControlOrOption()
    {
        Assert.True(new KeyShortcut(11, KeyShortcut.Command, "B").IsValid);
        Assert.True(new KeyShortcut(11, KeyShortcut.Control, "B").IsValid);
        Assert.True(new KeyShortcut(11, KeyShortcut.Option | KeyShortcut.Shift, "B").IsValid);
        Assert.False(new KeyShortcut(11, KeyShortcut.Shift, "B").IsValid);
        Assert.False(new KeyShortcut(11, 0, "B").IsValid);
    }

    [Fact]
    public void TestModifierMasksAreCarbons()
    {
        Assert.Equal(0x0100u, KeyShortcut.Command);
        Assert.Equal(0x0200u, KeyShortcut.Shift);
        Assert.Equal(0x0800u, KeyShortcut.Option);
        Assert.Equal(0x1000u, KeyShortcut.Control);
    }

    [Fact]
    public void TestRoundTripsThroughText()
    {
        var shortcut = new KeyShortcut(96, KeyShortcut.Option, "F5");
        Assert.Equal("96:2048:F5", shortcut.Serialize());
        Assert.Equal(shortcut, KeyShortcut.Parse(shortcut.Serialize()));
        // The key label is last, so it may contain the separator.
        var colon = new KeyShortcut(41, KeyShortcut.Command | KeyShortcut.Shift, ":");
        Assert.Equal(colon, KeyShortcut.Parse(colon.Serialize()));
        Assert.Equal(new KeyShortcut(11, 4352, ""), KeyShortcut.Parse("11:4352:"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("11:4352")]
    [InlineData("11:512:B")]
    [InlineData("11:0:B")]
    [InlineData("-11:4352:B")]
    [InlineData("B:4352:B")]
    [InlineData("11:B:B")]
    public void TestInvalidInputParsesToNull(string? text) => Assert.Null(KeyShortcut.Parse(text));
}
