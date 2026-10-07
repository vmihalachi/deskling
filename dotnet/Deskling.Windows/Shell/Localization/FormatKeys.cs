namespace Deskling.Windows.Shell.Localization;

/// <summary>
/// The catalog keys <see cref="Formats"/> needs for durations and lists, since the package ships no strings. The
/// values are keys in the app's catalog with Apple-style placeholders, as the Mac String Catalog writes them.
/// </summary>
/// <param name="Minutes">A plural keyed on the minutes, e.g. <c>%lld min</c>.</param>
/// <param name="Hours">A plural keyed on whole hours, e.g. <c>%lld hr</c>.</param>
/// <param name="HoursAndMinutes">A plural keyed on the hours with the minutes second, e.g. <c>%lld hr, %lld min</c>.</param>
/// <param name="Pair">Two items, e.g. <c>%1$@ and %2$@</c>.</param>
/// <param name="ListEnd">The joined head of a longer list and its last item, e.g. <c>%1$@, and %2$@</c>.</param>
/// <param name="ListMiddle">Two items inside a longer list, e.g. <c>%1$@, %2$@</c>.</param>
public sealed record FormatKeys(
    string Minutes,
    string Hours,
    string HoursAndMinutes,
    string Pair,
    string ListEnd,
    string ListMiddle
);
