using Laching.Core.Settings;

namespace Laching.Windows.Shell.Localization;

/// <summary>
/// The language picked in the app's Settings, kept in the app's own settings for an app without package identity (an
/// unpackaged build). With a package, Windows keeps the pick: <c>ApplicationLanguages.PrimaryLanguageOverride</c> outlives
/// the process. Without one the override lasts only for the process and can't be cleared (setting <c>""</c> throws), so
/// the companion shell's language row stores the tag here, under <see cref="Key"/>, and <c>CompanionApp.Start</c> sets it
/// again at the next start. Needs the app's settings store and its languages; never sets the override or the process's
/// languages itself, and never keeps a tag with a package.
/// </summary>
public sealed class LanguageSetting
{
    /// <summary>
    /// The key the tag is stored under: the package's own, beside the app's settings and out of their reset (as Windows
    /// keeps a packaged app's pick through a reset).
    /// </summary>
    public const string Key = "laching.language";

    private readonly ISettingsStore store;

    /// <summary>The setting in <paramref name="store"/>, for an app with <paramref name="languages"/>.</summary>
    /// <param name="store">The app's settings, the same store its other settings live in.</param>
    /// <param name="languages">The languages the app has, as BCP-47 tags ("en", "de", "pt-BR").</param>
    public LanguageSetting(ISettingsStore store, IReadOnlyList<string> languages)
    {
        this.store = store;
        Languages = languages;
    }

    /// <summary>
    /// The setting in the app's settings file (<see cref="LocalSettingsStore.ForApp"/> with the same
    /// <paramref name="appFolder"/>, which gives the app the same store for its other settings) when the app runs without
    /// package identity; null with a package, where Windows keeps the pick.
    /// </summary>
    public static LanguageSetting? ForApp(string appFolder, IReadOnlyList<string> languages) =>
        PackageIdentity.IsPackaged ? null : new LanguageSetting(LocalSettingsStore.ForApp(appFolder), languages);

    /// <summary>The languages the app has; a stored tag outside them is ignored.</summary>
    public IReadOnlyList<string> Languages { get; }

    /// <summary>
    /// The tag stored, spelled as the app has it, or null: none stored (Windows' language), or one the app no longer has.
    /// </summary>
    public string? Stored =>
        store.GetString(Key) is { Length: > 0 } tag
            ? Languages.FirstOrDefault(language => string.Equals(language, tag, StringComparison.OrdinalIgnoreCase))
            : null;

    /// <summary>Keeps <paramref name="tag"/> for the next start; <c>""</c> (Windows' language) removes the stored tag.</summary>
    public void Store(string tag)
    {
        if (tag.Length == 0)
            store.Remove(Key);
        else
            store.SetString(Key, tag);
    }
}
