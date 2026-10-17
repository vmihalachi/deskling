using System.Globalization;

namespace Laching.Companion.Shell.App;

/// <summary>
/// The app's words by catalog key (the Mac's String Catalog keys, whose English is the key itself), for the parts of a
/// Windows app that compile without WinUI: its model and the copy records it builds, so their tests run on any OS. The
/// app passes <c>Laching.Windows.Companion</c>'s <c>LocStrings</c> (its <c>.resw</c> through <c>Loc</c>); tests pass
/// <c>Laching.Testing</c>'s <c>ReswStrings</c> over the same generated files. Placeholders are .NET's (<c>{0}</c>).
/// </summary>
public interface IStrings
{
    /// <summary>The language the words are in, which picks a plural variant.</summary>
    string Language { get; }

    /// <summary>The culture numbers and dates are written in.</summary>
    CultureInfo Culture { get; }

    /// <summary>The text for a key; English from the key itself when the strings don't have it.</summary>
    string Get(string key);

    /// <summary><see cref="Get"/> with its placeholders filled.</summary>
    string Format(string key, params object[] args);

    /// <summary>A plural string: <paramref name="count"/> picks the variant; pass it in <paramref name="args"/> too.</summary>
    string Plural(string key, int count, params object[] args);

    /// <summary>
    /// A phrase's text under its catalog key (<c>phrase.&lt;id&gt;</c>, <c>.1</c>, <c>.2</c>), else
    /// <paramref name="source"/>, the line as the bank has it: <see cref="CompanionAppConfiguration.Localize"/>.
    /// </summary>
    string Localize(string key, string source);
}
