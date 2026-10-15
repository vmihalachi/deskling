namespace Deskling.Testing.Tests;

/// <summary><see cref="ScriptedRandom"/> plays its values in order and repeats the last one.</summary>
public sealed class ScriptedRandomTests
{
    [Fact]
    public void PlaysItsValuesThenRepeatsTheLast()
    {
        var random = new ScriptedRandom([0.1, 0.2, 0.3]);
        Assert.Equal([0.1, 0.2, 0.3], [random.NextUnit(), random.NextUnit(), random.NextUnit()]);
        Assert.Equal(0.3, random.NextUnit());
        Assert.Equal(0.3, random.NextUnit());
        Assert.Equal(5, random.Reads);
    }

    [Fact]
    public void ASingleValueRepeatsFromTheStart()
    {
        var random = new ScriptedRandom([0.9]);
        Assert.Equal(0.9, random.NextUnit());
        Assert.Equal(0.9, random.NextUnit());
        Assert.Equal(2, random.Reads);
    }
}
