using Deskling.Core.Settings;
using Deskling.Windows.Companion.Settings;
using Deskling.Windows.Shell.Localization;

namespace Deskling.Windows.Companion.Tests.Settings;

/// <summary>The language row's logic: when the restart note shows, which entry a Settings window shows, what is stored.</summary>
public sealed class LanguagePickTests
{
    private static readonly string[] App = ["en", "de", "ro"];

    [Fact]
    public void NothingPickedYetShowsNoNoteAndTheStartLanguage()
    {
        Assert.False(new LanguagePick("", null).ChangesAtRestart);
        Assert.False(new LanguagePick("de", null).ChangesAtRestart);
        Assert.Equal(0, new LanguagePick("", null).Entry(App));
        Assert.Equal(2, new LanguagePick("de", null).Entry(App));
    }

    [Fact]
    public void PickingAnotherLanguageShowsTheNote()
    {
        var fromWindows = new LanguagePick("", null);
        fromWindows.Pick("de");
        Assert.True(fromWindows.ChangesAtRestart);

        var fromGerman = new LanguagePick("de", null);
        fromGerman.Pick("ro");
        Assert.True(fromGerman.ChangesAtRestart);
        Assert.Equal(3, fromGerman.Entry(App));
    }

    [Fact]
    public void PickingTheStartLanguageBackHidesTheNote()
    {
        var pick = new LanguagePick("de", null);
        pick.Pick("ro");
        pick.Pick("de");
        Assert.False(pick.ChangesAtRestart);

        var differentCase = new LanguagePick("pt-BR", null);
        differentCase.Pick("PT-br");
        Assert.False(differentCase.ChangesAtRestart);
    }

    [Fact]
    public void WindowsLanguageAfterAnOverrideShowsTheNoteAndTheFirstEntry()
    {
        var pick = new LanguagePick("de", null);
        pick.Pick("");
        Assert.True(pick.ChangesAtRestart);
        Assert.Equal(0, pick.Entry(App));
    }

    [Fact]
    public void WindowsLanguageWithoutAnOverrideHidesTheNote()
    {
        var pick = new LanguagePick("", null);
        pick.Pick("de");
        pick.Pick("");
        Assert.False(pick.ChangesAtRestart);
        Assert.Equal(0, pick.Entry(App));
    }

    [Fact]
    public void AStartLanguageTheAppDoesntHaveShowsWindowsLanguageWithoutANote()
    {
        var pick = new LanguagePick("it", null);
        Assert.Equal(0, pick.Entry(App));
        Assert.False(pick.ChangesAtRestart);
    }

    [Fact]
    public void WithoutAPackageEveryPickIsStoredAndWindowsLanguageClearsIt()
    {
        var store = new InMemorySettingsStore();
        var setting = new LanguageSetting(store, App);
        var pick = new LanguagePick("", setting);

        pick.Pick("de");
        Assert.Equal("de", setting.Stored);
        pick.Pick("ro");
        Assert.Equal("ro", setting.Stored);
        pick.Pick("");
        Assert.Null(setting.Stored);
        Assert.Null(store.GetString(LanguageSetting.Key));
    }

    [Fact]
    public void AStoredTagWindowsTakesIsTheStartLanguage()
    {
        var setting = new LanguageSetting(new InMemorySettingsStore(), App);
        setting.Store("de");
        var applied = new List<string>();

        var pick = LanguagePick.Start(setting, "", tag =>
        {
            applied.Add(tag);
            return true;
        });

        // After a restart into German the row reads German, with no note.
        Assert.Equal(["de"], applied);
        Assert.Equal("de", pick.StartedIn);
        Assert.Equal(2, pick.Entry(App));
        Assert.False(pick.ChangesAtRestart);
        Assert.Same(setting, pick.Setting);
    }

    [Fact]
    public void AStoredTagWindowsRefusesLeavesTheOverride()
    {
        var setting = new LanguageSetting(new InMemorySettingsStore(), App);
        setting.Store("de");

        var pick = LanguagePick.Start(setting, "", _ => false);

        Assert.Equal("", pick.StartedIn);
        Assert.Equal(0, pick.Entry(App));
        Assert.False(pick.ChangesAtRestart);
    }

    [Fact]
    public void NoTagOrOneTheAppNoLongerHasIsNeverApplied()
    {
        var store = new InMemorySettingsStore();
        var setting = new LanguageSetting(store, App);
        var applied = new List<string>();
        bool Apply(string tag)
        {
            applied.Add(tag);
            return true;
        }

        Assert.Equal("", LanguagePick.Start(setting, "", Apply).StartedIn);
        store.SetString(LanguageSetting.Key, "it");
        Assert.Equal("", LanguagePick.Start(setting, "", Apply).StartedIn);
        Assert.Empty(applied);
    }

    [Fact]
    public void TheProcessTakesTheTagThenItsSpecificCulture()
    {
        Assert.Equal(["de", "de-DE"], LanguagePick.ProcessLanguages("de"));
        Assert.Equal(["ro", "ro-RO"], LanguagePick.ProcessLanguages("ro"));
        Assert.Equal(["pt-BR"], LanguagePick.ProcessLanguages("pt-BR"));
        Assert.Equal(["xx-nope"], LanguagePick.ProcessLanguages("xx-nope"));
    }
}
