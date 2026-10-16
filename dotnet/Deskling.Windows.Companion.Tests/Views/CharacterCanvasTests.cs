using Deskling.Windows.Companion.Views;

namespace Deskling.Windows.Companion.Tests.Views;

/// <summary>What Narrator reads for the character.</summary>
public sealed class CharacterCanvasTests
{
    [Fact]
    public void TheCanvasIsNamedByItsLineElseItsLabel()
    {
        Assert.Equal("Hello there", CharacterCanvas.AccessibleName("Hello there", "The character"));
        Assert.Equal("The character", CharacterCanvas.AccessibleName(null, "The character"));
        // x:Bind hands a null line over as "": still no line.
        Assert.Equal("The character", CharacterCanvas.AccessibleName("", "The character"));
        Assert.Equal("", CharacterCanvas.AccessibleName("", null));
    }
}
