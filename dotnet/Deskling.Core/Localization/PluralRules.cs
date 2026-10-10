using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Deskling.Core.Localization;

/// <summary>
/// CLDR plural categories for integers in the languages the apps ship (cs, da, de, en, es, fr, it, ja, ko, nb, nl,
/// pl, pt-BR, ro, sv, tr, zh), so a count picks the same variant of a string as Foundation does on the Mac. Unknown
/// languages use the English rule.
/// </summary>
public static class PluralRules
{
    /// <summary>
    /// The category ("one", "few", "many" or "other") of <paramref name="n"/> in <paramref name="language"/>, a BCP 47 tag.
    /// </summary>
    public static string Category(string language, int n)
    {
        var lang = language.ToLowerInvariant();
        if (lang.StartsWith("pt-br", StringComparison.Ordinal))
            return Million(n) ? "many" : OneOrOther(n, zeroIsOne: true);
        switch (lang.Split('-')[0])
        {
            case "en":
            case "da":
            case "de":
            case "nb":
            case "nl":
            case "no":
            case "sv":
            case "tr":
                return OneOrOther(n, zeroIsOne: false);
            case "es":
            case "it":
                return Million(n) ? "many" : OneOrOther(n, zeroIsOne: false);
            case "fr":
                return Million(n) ? "many" : OneOrOther(n, zeroIsOne: true);
            case "ro":
                return Romanian(n);
            case "pl":
                return Polish(n);
            case "cs":
                return n == 1 ? "one" : n is >= 2 and <= 4 ? "few" : "other";
            case "ja":
            case "ko":
            case "zh":
                return "other";
            default:
                return OneOrOther(n, zeroIsOne: false);
        }
    }

    /// <summary>
    /// The resource name for <paramref name="category"/> of a plural string: <c>name__category</c>, falling back to
    /// <c>name__other</c> when the string has no variant for that category.
    /// </summary>
    public static string PluralKey(string reswName, string category, IReadOnlySet<string> availableCategories)
    {
        var chosen = availableCategories.Contains(category) ? category : "other";
        return $"{reswName}__{chosen}";
    }

    private static string OneOrOther(int n, bool zeroIsOne)
    {
        if (n == 1 || (zeroIsOne && n == 0))
            return "one";
        return "other";
    }

    private static bool Million(int n) => n != 0 && n % 1_000_000 == 0;

    private static string Romanian(int n)
    {
        if (n == 1)
            return "one";
        var mod100 = n % 100;
        return n == 0 || (mod100 >= 1 && mod100 <= 19) ? "few" : "other";
    }

    private static string Polish(int n)
    {
        if (n == 1)
            return "one";
        var mod10 = n % 10;
        var mod100 = n % 100;
        return mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) ? "few" : "many";
    }
}

/// <summary>
/// The <c>.resw</c> name of a catalog key, as the apps' <c>l10n.py</c> writes it (<c>resw_name</c>): lower-case ASCII
/// letters and digits stay, every other UTF-16 code unit becomes <c>_</c> plus 4 upper-case hex digits. Upper-case
/// letters are escaped because MRT names ignore case. MakePri silently drops names over 255 characters, so a name
/// over <see cref="MaxLength"/> keeps its first <see cref="PrefixLength"/> characters plus <c>_h</c> and 16 hex digits
/// of the key's SHA-256, leaving room for a <c>__other</c> plural suffix.
/// </summary>
public static class ReswName
{
    public const int MaxLength = 240;
    public const int PrefixLength = 200;

    public static string For(string key)
    {
        var sb = new StringBuilder();
        foreach (var ch in key)
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                sb.Append(ch);
            else
                sb.Append('_').Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
        }
        if (sb.Length <= MaxLength)
            return sb.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return sb.ToString(0, PrefixLength) + "_h" + Convert.ToHexStringLower(hash)[..16];
    }
}
