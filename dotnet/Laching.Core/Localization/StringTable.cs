using System.Globalization;

namespace Laching.Core.Localization;

/// <summary>
/// Text by catalog key over a table of <c>.resw</c>-named strings: names through <see cref="ReswName.For"/>, plural
/// variants as <c>name__one</c>, <c>name__other</c> and so on, picked with <see cref="PluralRules"/>, and a missing key
/// as English from the key (<see cref="ReswName.FallbackText"/>), so nothing ever shows up blank. A subclass gives the
/// lookup, the language and the culture: the Windows app's <c>Loc</c> over MRT Core, and <c>Laching.Testing</c>'s
/// <c>ReswStrings</c> over the same files, so the test double reads exactly as the app does. C# only (the Mac has
/// String Catalogs); never touches the file system itself.
/// </summary>
public abstract class StringTable
{
    /// <summary>The language the strings are in, which picks a plural variant.</summary>
    public abstract string Language { get; }

    /// <summary>The culture placeholders are filled in.</summary>
    public abstract CultureInfo Culture { get; }

    /// <summary>The string named <paramref name="name"/> (a <see cref="ReswName"/>), or null when the table lacks it.</summary>
    protected abstract string? Lookup(string name);

    /// <summary>Called with each key the table doesn't have, before it falls back to the key's English.</summary>
    protected virtual void OnMissing(string key) { }

    /// <summary>The text for a key; English from the key itself when the table doesn't have it.</summary>
    public string Get(string key) => Lookup(ReswName.For(key)) ?? Fallback(key);

    /// <summary>
    /// A phrase's text: its translation under <paramref name="key"/> (<c>phrase.&lt;id&gt;</c>), else
    /// <paramref name="source"/>, the line as the bank has it. Unlike <see cref="Get"/> it never shows the key.
    /// </summary>
    public string Localize(string key, string source) => Lookup(ReswName.For(key)) ?? source;

    /// <summary><see cref="Get"/> with its placeholders filled.</summary>
    public string Format(string key, params object[] args) => string.Format(Culture, Get(key), args);

    /// <summary>A plural string: <paramref name="count"/> picks the variant; pass it in <paramref name="args"/> too.</summary>
    public string Plural(string key, int count, params object[] args)
    {
        var name = ReswName.For(key);
        var text = Lookup($"{name}__{PluralRules.Category(Language, count)}") ?? Lookup($"{name}__other") ?? Fallback(key);
        return string.Format(Culture, text, args);
    }

    private string Fallback(string key)
    {
        OnMissing(key);
        return ReswName.FallbackText(key);
    }
}
