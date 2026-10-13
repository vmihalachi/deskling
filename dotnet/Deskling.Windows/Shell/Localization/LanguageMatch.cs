namespace Deskling.Windows.Shell.Localization;

/// <summary>
/// <see cref="Loc.Choose"/> over given preferences, apart from <see cref="Loc"/> (whose resources need the Windows App
/// SDK) so tests reach it.
/// </summary>
internal static class LanguageMatch
{
    /// <summary>
    /// The first of <paramref name="picked"/> and then <paramref name="preferred"/> that the app has, by its tag, else
    /// by its base language ("it-CH" runs an app that has "it" in Italian, "pt-PT" one that has only "pt-BR" in
    /// Portuguese); else the app's first language.
    /// </summary>
    public static string Choose(IReadOnlyList<string> languages, string? picked, IEnumerable<string> preferred)
    {
        var tags = string.IsNullOrEmpty(picked) ? preferred : preferred.Prepend(picked);
        foreach (var tag in tags)
        {
            var match =
                languages.FirstOrDefault(language => string.Equals(language, tag, StringComparison.OrdinalIgnoreCase))
                ?? languages.FirstOrDefault(language => string.Equals(Base(language), Base(tag), StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }
        return languages.Count > 0 ? languages[0] : "en";
    }

    private static string Base(string tag) => tag.Split('-')[0];
}
