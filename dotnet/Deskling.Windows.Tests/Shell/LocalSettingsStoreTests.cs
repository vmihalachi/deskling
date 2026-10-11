using Deskling.Windows.Shell;

namespace Deskling.Windows.Tests.Shell;

public sealed class LocalSettingsStoreTests
{
    [Fact]
    public void RoundTripsEveryTypeAndReadsMissingAsNull()
    {
        var values = new Dictionary<string, object>();
        var store = new LocalSettingsStore(values);
        Assert.Null(store.GetBool("b"));
        Assert.Null(store.GetStrings("ss"));

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

        // Another type under the key reads as null, lists included.
        Assert.Null(store.GetInteger("s"));
        Assert.Null(store.GetStrings("is"));
        Assert.Null(store.GetIntegers("ss"));

        store.Remove("ss");
        Assert.Null(store.GetStrings("ss"));
        Assert.Equal(5, values.Count);
    }
}
