using System.Globalization;
using Deskling.Companion.Shell.App;
using Deskling.Windows.Shell.Localization;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// <see cref="IStrings"/> over <see cref="Loc"/>: the app's <c>.resw</c> in the language <c>Loc.UseLanguage</c> set at
/// launch. What a Windows app passes to the model and copy it shares with its tests.
/// </summary>
public sealed class LocStrings : IStrings
{
    public static LocStrings Shared { get; } = new();

    private LocStrings() { }

    public string Language => Loc.Language;

    public CultureInfo Culture => Loc.Culture;

    public string Get(string key) => Loc.Get(key);

    public string Format(string key, params object[] args) => Loc.Format(key, args);

    public string Plural(string key, int count, params object[] args) => Loc.Plural(key, count, args);

    public string Localize(string key, string source) => Loc.Localize(key, source);
}
