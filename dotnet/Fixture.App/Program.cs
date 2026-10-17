using Laching.Windows.Companion.Screenshots;
using Laching.Windows.Companion.Shell;
using Laching.Windows.Shell.Localization;

namespace FixtureApp;

/// <summary>
/// Laching's fixture app: the companion shell over the fixture bank, run by hand to check what no test reaches (the tray
/// icon and its panel, speech, Shh, calls and full screen, the taskbar's light and dark, notifications). Unpackaged and
/// never packed. <c>--first-run</c> starts it as if for the first time (onboarding). A screenshot run
/// (<c>scripts/fixture-screenshots.ps1</c>) claims no single instance, so it works beside a copy in use. The language
/// picked in Settings is kept as an unpackaged app keeps it, in <c>%LOCALAPPDATA%\Laching Fixture\settings.json</c>.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main() =>
        CompanionApp.Start(
            ScreenshotCapture.IsEnabled ? null : "laching.fixture",
            () => new App(),
            LanguageSetting.ForApp("Laching Fixture", App.Languages)
        );
}
