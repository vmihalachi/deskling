namespace Laching.Companion.Settings;

/// <summary>System, light or dark. The raw values are stored; never rename one.</summary>
public enum AppearanceSetting
{
    System,
    Light,
    Dark,
}

/// <summary>The appearance table and raw values (Swift's <c>AppearanceSetting.allCases</c>, <c>rawValue</c>).</summary>
public static class Appearances
{
    public static IReadOnlyList<AppearanceSetting> All { get; } = Enum.GetValues<AppearanceSetting>();

    public static string RawValue(this AppearanceSetting appearance) =>
        appearance switch
        {
            AppearanceSetting.System => "system",
            AppearanceSetting.Light => "light",
            AppearanceSetting.Dark => "dark",
            _ => throw new ArgumentOutOfRangeException(nameof(appearance), appearance, "unknown appearance"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static AppearanceSetting? Parse(string? rawValue) =>
        rawValue switch
        {
            "system" => AppearanceSetting.System,
            "light" => AppearanceSetting.Light,
            "dark" => AppearanceSetting.Dark,
            _ => null,
        };
}

/// <summary>How big the character stands on the desktop. The raw values are stored; never rename one.</summary>
public enum DesktopSize
{
    Small,
    Medium,
    Large,
}

/// <summary>The desktop size table and raw values (Swift's <c>DesktopSize.allCases</c>, <c>rawValue</c>, <c>height</c>).</summary>
public static class DesktopSizes
{
    public static IReadOnlyList<DesktopSize> All { get; } = Enum.GetValues<DesktopSize>();

    public static string RawValue(this DesktopSize size) =>
        size switch
        {
            DesktopSize.Small => "small",
            DesktopSize.Medium => "medium",
            DesktopSize.Large => "large",
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, "unknown desktop size"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static DesktopSize? Parse(string? rawValue) =>
        rawValue switch
        {
            "small" => DesktopSize.Small,
            "medium" => DesktopSize.Medium,
            "large" => DesktopSize.Large,
            _ => null,
        };

    /// <summary>The character's height on the desktop, in points (device-independent pixels on Windows); the width follows the art.</summary>
    public static double Height(this DesktopSize size) =>
        size switch
        {
            DesktopSize.Small => 64,
            DesktopSize.Medium => 96,
            DesktopSize.Large => 140,
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, "unknown desktop size"),
        };

    /// <summary><paramref name="step"/> sizes bigger (or smaller, when negative), stopping at Small and Large: a trackpad pinch on the character.</summary>
    public static DesktopSize Stepped(this DesktopSize size, int step)
    {
        var index = Math.Clamp(All.ToList().IndexOf(size) + step, 0, All.Count - 1);
        return All[index];
    }
}

/// <summary>
/// Where the character stands on the desktop: the middle of the bottom edge of its desktop window (the contact
/// shadow under the character), in points (device-independent pixels) from the primary screen's top left with y down,
/// so the same value means the same spot on both platforms.
/// </summary>
public readonly record struct DesktopPosition(int X, int Y);
