using Deskling.Core.Settings;

namespace Deskling.Core.Tests.Settings;

public sealed class SettingsStoreTests
{
    [Fact]
    public void InMemoryRoundTripsAndReadsMissingAsNull()
    {
        var store = new InMemorySettingsStore();
        Assert.Null(store.GetBool("b"));
        Assert.Null(store.GetInteger("i"));
        Assert.Null(store.GetDouble("d"));
        Assert.Null(store.GetString("s"));
        Assert.Null(store.GetStrings("ss"));
        Assert.Null(store.GetIntegers("is"));

        store.SetBool("b", false);
        store.SetInteger("i", 0);
        store.SetDouble("d", 0.25);
        store.SetString("s", "");
        store.SetStrings("ss", ["a", "b"]);
        store.SetIntegers("is", [3, 1]);
        Assert.False(store.GetBool("b"));
        Assert.Equal(0, store.GetInteger("i"));
        Assert.Equal(0.25, store.GetDouble("d"));
        Assert.Equal("", store.GetString("s"));
        Assert.Equal(["a", "b"], store.GetStrings("ss"));
        Assert.Equal([3, 1], store.GetIntegers("is"));

        store.Remove("b");
        Assert.Null(store.GetBool("b"));
        Assert.Equal(["d", "i", "is", "s", "ss"], store.Keys.Order(StringComparer.Ordinal));
        // Another type under the key reads as null.
        Assert.Null(store.GetInteger("s"));
        Assert.Null(store.GetStrings("is"));
    }
}
