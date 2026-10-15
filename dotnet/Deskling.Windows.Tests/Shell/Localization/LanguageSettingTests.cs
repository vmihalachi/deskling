using Deskling.Core.Settings;
using Deskling.Windows.Shell;
using Deskling.Windows.Shell.Localization;

namespace Deskling.Windows.Tests.Shell.Localization;

public sealed class LanguageSettingTests : IDisposable
{
    private static readonly string[] App = ["en", "de", "pt-BR"];

    private readonly string folder = Path.Combine(Path.GetTempPath(), "deskling-language-" + Guid.NewGuid().ToString("N"));

    private string File => Path.Combine(folder, "App Dev", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Fact]
    public void APickIsStoredUnderThePackagesKeyAndReadAtTheNextStart()
    {
        var store = new FileSettingsStore(File);
        Assert.Null(new LanguageSetting(store, App).Stored);

        new LanguageSetting(store, App).Store("de");

        var reopened = new FileSettingsStore(File);
        Assert.Equal("de", reopened.GetString(LanguageSetting.Key));
        Assert.Equal("de", new LanguageSetting(reopened, App).Stored);
    }

    [Fact]
    public void WindowsLanguageClearsTheStoredTag()
    {
        var store = new FileSettingsStore(File);
        store.SetBool("appSetting", true);
        new LanguageSetting(store, App).Store("de");

        new LanguageSetting(store, App).Store("");

        var reopened = new FileSettingsStore(File);
        Assert.Null(reopened.GetString(LanguageSetting.Key));
        Assert.Null(new LanguageSetting(reopened, App).Stored);
        // The app's own settings in the same file stay.
        Assert.True(reopened.GetBool("appSetting"));
    }

    [Fact]
    public void ATagTheAppNoLongerHasIsIgnored()
    {
        var store = new InMemorySettingsStore();
        store.SetString(LanguageSetting.Key, "ro");
        Assert.Null(new LanguageSetting(store, App).Stored);
        // Still there, for a build that has it.
        Assert.Equal("ro", store.GetString(LanguageSetting.Key));

        store.SetString(LanguageSetting.Key, "");
        Assert.Null(new LanguageSetting(store, App).Stored);
        store.SetInteger(LanguageSetting.Key, 3);
        Assert.Null(new LanguageSetting(store, App).Stored);
    }

    [Fact]
    public void AStoredTagReadsAsTheAppSpellsIt()
    {
        var store = new InMemorySettingsStore();
        store.SetString(LanguageSetting.Key, "PT-br");
        Assert.Equal("pt-BR", new LanguageSetting(store, App).Stored);
    }

    [Fact]
    public void WithoutAPackageTheSettingAndTheAppsSettingsShareOneStore()
    {
        // The test host has no package identity. Nothing is written, so the folder is never made.
        var appFolder = "deskling-tests-" + Guid.NewGuid().ToString("N");
        Assert.False(PackageIdentity.IsPackaged);
        Assert.Same(LocalSettingsStore.ForApp(appFolder), LocalSettingsStore.ForApp(appFolder));
        Assert.NotSame(LocalSettingsStore.ForApp(appFolder), LocalSettingsStore.ForApp(appFolder + "-other"));
        Assert.NotNull(LanguageSetting.ForApp(appFolder, App));
    }
}
