using System.Globalization;
using System.Xml.Linq;
using Laching.Companion.Shell.App;
using Laching.Core.Localization;

namespace Laching.Testing;

/// <summary>
/// <see cref="IStrings"/> over an app's generated <c>Strings/&lt;language&gt;/Resources.resw</c>, read by the same
/// <see cref="StringTable"/> as the Windows app's <c>Loc</c> (names through <see cref="ReswName"/>, plural variants as
/// <c>name__one</c>, <c>name__other</c>, a missing key as English from the key), so an app's model tests see its real
/// words without Windows' resource system.
/// </summary>
public sealed class ReswStrings : StringTable, IStrings
{
    private readonly IReadOnlyDictionary<string, string> values;

    private ReswStrings(string language, IReadOnlyDictionary<string, string> values)
    {
        Language = language;
        Culture = CultureInfo.GetCultureInfo(language);
        this.values = values;
    }

    public override string Language { get; }

    public override CultureInfo Culture { get; }

    /// <summary>The keys asked for that the file doesn't have, for a test that every string an app uses exists.</summary>
    public ISet<string> Missing { get; } = new SortedSet<string>(StringComparer.Ordinal);

    /// <summary>Reads <paramref name="path"/>; the language is its folder's name (<c>Strings/it/Resources.resw</c>).</summary>
    public static ReswStrings Load(string path, string? language = null)
    {
        var document = XDocument.Load(path);
        var values = document
            .Root!.Elements("data")
            .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? "", StringComparer.Ordinal);
        return new ReswStrings(language ?? new DirectoryInfo(Path.GetDirectoryName(path)!).Name, values);
    }

    /// <summary>The names in the file, as <see cref="ReswName.For"/> writes them.</summary>
    public IReadOnlyCollection<string> Names => values.Keys.ToArray();

    /// <summary>Whether the file has <paramref name="key"/>, as a plain string or with plural variants.</summary>
    public bool Has(string key) => Lookup(ReswName.For(key)) is not null || Lookup(ReswName.For(key) + "__other") is not null;

    protected override string? Lookup(string name) => values.TryGetValue(name, out var value) ? value : null;

    protected override void OnMissing(string key) => Missing.Add(key);
}
