using Laching.Windows.Shell;

namespace Laching.Windows.Tests.Shell;

public sealed class FileSettingsStoreTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "laching-settings-" + Guid.NewGuid().ToString("N"));

    private string File => Path.Combine(folder, "App Dev", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public void RoundTripsEveryTypeThroughTheFile()
    {
        var store = new FileSettingsStore(File);
        Assert.Null(store.GetBool("b"));
        store.SetBool("b", true);
        store.SetInteger("i", 3);
        store.SetDouble("d", 0.25);
        store.SetString("s", "Ciao");
        store.SetStrings("ss", ["a", "b"]);
        store.SetIntegers("is", [3, 1]);

        var reopened = new FileSettingsStore(File);
        Assert.True(reopened.GetBool("b"));
        Assert.Equal(3, reopened.GetInteger("i"));
        Assert.Equal(0.25, reopened.GetDouble("d"));
        Assert.Equal("Ciao", reopened.GetString("s"));
        Assert.Equal(["a", "b"], reopened.GetStrings("ss"));
        Assert.Equal([3, 1], reopened.GetIntegers("is"));
    }

    [Fact]
    public void AnotherTypeUnderTheKeyReadsAsNullAndAWholeNumberStaysADouble()
    {
        var store = new FileSettingsStore(File);
        store.SetBool("b", true);
        store.SetInteger("i", 1);
        store.SetDouble("d", 2);
        store.SetIntegers("is", [1]);
        var reopened = new FileSettingsStore(File);
        Assert.Equal(2, reopened.GetDouble("d"));
        Assert.Null(reopened.GetInteger("d"));
        Assert.Null(reopened.GetBool("d"));
        Assert.Null(reopened.GetInteger("b"));
        Assert.Null(reopened.GetDouble("b"));
        Assert.Null(reopened.GetBool("i"));
        Assert.Null(reopened.GetDouble("i"));
        Assert.Null(reopened.GetStrings("is"));
    }

    [Fact]
    public void AValueThatDoesntMatchItsTypeOrANullEntryReadsAsNullInsteadOfThrowing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(File)!);
        global::System.IO.File.WriteAllText(
            File,
            """
            {
              "b": { "Type": "bool", "Value": "yes" },
              "i": { "Type": "int", "Value": 2.5 },
              "big": { "Type": "int", "Value": 9999999999 },
              "d": { "Type": "double", "Value": "1" },
              "s": { "Type": "string", "Value": 3 },
              "ss": { "Type": "string[]", "Value": ["a", null] },
              "sx": { "Type": "string[]", "Value": [1, 2] },
              "is": { "Type": "int[]", "Value": "1,2" },
              "gone": null,
              "laching.language": { "Type": "string", "Value": { "tag": "de" } }
            }
            """);

        var store = new FileSettingsStore(File);
        Assert.Null(store.GetBool("b"));
        Assert.Null(store.GetInteger("i"));
        Assert.Null(store.GetInteger("big"));
        Assert.Null(store.GetDouble("d"));
        Assert.Null(store.GetString("s"));
        Assert.Null(store.GetStrings("ss"));
        Assert.Null(store.GetStrings("sx"));
        Assert.Null(store.GetIntegers("is"));
        Assert.Null(store.GetString("gone"));
        Assert.Null(store.GetString("laching.language"));
    }

    [Fact]
    public void RemoveForgetsAndAnUnreadableFileStartsEmpty()
    {
        var store = new FileSettingsStore(File);
        store.SetString("s", "x");
        store.Remove("s");
        Assert.Null(new FileSettingsStore(File).GetString("s"));

        global::System.IO.File.WriteAllText(File, "{ not json");
        Assert.Null(new FileSettingsStore(File).GetString("s"));
    }
}
