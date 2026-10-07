using Microsoft.Win32;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Deskling.Windows.System;

/// <summary>
/// Light or dark, from <c>UISettings</c>: the app mode (what windows follow) from the system background color,
/// and the system mode (what the taskbar and Start follow) from the Personalize registry key. Reads settings;
/// never prompts. <see cref="Changed"/> is raised on a background thread when the colors change; dispose to stop
/// listening.
/// </summary>
public sealed class AppearanceWatcher : IDisposable
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private readonly UISettings settings = new();
    private readonly TypedEventHandler<UISettings, object> onChanged;

    public AppearanceWatcher()
    {
        onChanged = (_, _) => Changed?.Invoke();
        settings.ColorValuesChanged += onChanged;
    }

    /// <summary>The app mode or the accent color changed.</summary>
    public event Action? Changed;

    /// <summary>Apps are in dark mode (Settings › Personalization › Colors › Choose your default app mode).</summary>
    public bool IsDark => IsDarkColor(settings.GetColorValue(UIColorType.Background));

    /// <summary>Whether a background color reads as dark: the WinUI luminance rule.</summary>
    public static bool IsDarkColor(Color color) => 5 * color.G + 2 * color.R + color.B <= 8 * 128;

    /// <summary>
    /// The taskbar is light (Choose your default Windows mode). Separate from the app mode, so a tray glyph
    /// should follow this one.
    /// </summary>
    public static bool IsTaskbarLight
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("SystemUsesLightTheme") is int light && light != 0;
        }
    }

    public void Dispose() => settings.ColorValuesChanged -= onChanged;
}
