namespace Deskling.Windows.Companion.Screenshots;

/// <summary>
/// The app's screenshot run (Swift's <c>ScreenshotCapture</c>): the app's run script sets <see cref="ModeVariable"/> (a
/// comma-separated list of shot name prefixes, or <c>all</c>). While it is set, the character holds its still pose, so
/// shots are the same every run.
/// </summary>
public static class ScreenshotCapture
{
    public const string ModeVariable = "DESKLING_SCREENSHOT_MODE";

    public static bool IsEnabled => Environment.GetEnvironmentVariable(ModeVariable) is not null;
}
