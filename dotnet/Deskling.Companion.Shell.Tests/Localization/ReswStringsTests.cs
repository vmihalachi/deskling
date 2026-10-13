using Deskling.Core.Localization;

namespace Deskling.Companion.Shell.Tests.Localization;

public sealed class ReswStringsTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "deskling-resw-" + Guid.NewGuid().ToString("N"), "it");

    public ReswStringsTests()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "Resources.resw"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="{ReswName.For("Tell me a joke")}" xml:space="preserve"><value>Raccontami una barzelletta</value></data>
              <data name="{ReswName.For("phrase.general-x")}" xml:space="preserve"><value>Una battuta</value></data>
              <data name="{ReswName.For("%lld jokes muted")}__one" xml:space="preserve"><value>{"{0}"} battuta silenziata</value></data>
              <data name="{ReswName.For("%lld jokes muted")}__other" xml:space="preserve"><value>{"{0}"} battute silenziate</value></data>
            </root>
            """
        );
    }

    public void Dispose() => Directory.Delete(Path.GetDirectoryName(folder)!, recursive: true);

    [Fact]
    public void ReadsTheAppsWordsInTheFoldersLanguage()
    {
        var strings = ReswStrings.Load(Path.Combine(folder, "Resources.resw"));
        Assert.Equal("it", strings.Language);
        Assert.Equal("Raccontami una barzelletta", strings.Get("Tell me a joke"));
        Assert.Equal("1 battuta silenziata", strings.Plural("%lld jokes muted", 1, 1));
        Assert.Equal("3 battute silenziate", strings.Plural("%lld jokes muted", 3, 3));
        Assert.True(strings.Has("%lld jokes muted"));
        Assert.Empty(strings.Missing);
    }

    [Fact]
    public void APhraseFallsBackToItsSourceAndAMissingKeyToItsEnglish()
    {
        var strings = ReswStrings.Load(Path.Combine(folder, "Resources.resw"));
        Assert.Equal("Una battuta", strings.Localize("phrase.general-x", "A joke"));
        Assert.Equal("Another joke", strings.Localize("phrase.general-y", "Another joke"));
        Assert.Equal("Shh until 3", strings.Format("Shh until %@", 3));
        Assert.Equal(["Shh until %@"], strings.Missing);
    }
}
