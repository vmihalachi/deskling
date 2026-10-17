namespace Laching.Companion;

/// <summary>
/// The other direction of the string-backed enums' <c>RawValue()</c> switches, which Swift gets from
/// <c>enum: String</c>. A C#-only helper: no Swift twin.
/// </summary>
internal static class RawValues
{
    /// <summary>The case of <paramref name="all"/> whose raw value is <paramref name="value"/>, or null for an unknown name.</summary>
    public static T? Parse<T>(IReadOnlyList<T> all, Func<T, string> raw, string? value)
        where T : struct
    {
        foreach (var item in all)
        {
            if (raw(item) == value)
                return item;
        }
        return null;
    }
}
