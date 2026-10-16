using Deskling.Windows.Companion.Screenshots;
using Deskling.Windows.Companion.Shell;
using Deskling.Windows.Shell.Localization;

namespace FixtureApp;

/// <summary>
/// Deskling's fixture app: the companion shell over the fixture bank, run by hand to check what no test reaches (the tray
/// icon and its panel, speech, Shh, calls and full screen, the taskbar's light and dark, notifications). Unpackaged and
/// never packed. <c>--first-run</c> starts it as if for the first time (onboarding). A screenshot run
/// (<c>scripts/fixture-screenshots.ps1</c>) claims no single instance, so it works beside a copy in use. The language
/// picked in Settings is kept as an unpackaged app keeps it, in <c>%LOCALAPPDATA%\Deskling Fixture\settings.json</c>.
/// </summary>
public static class Program
{
    [STAThread]
    private static void Main() =>
        CompanionApp.Start(
            ScreenshotCapture.IsEnabled ? null : "deskling.fixture",
            () => new App(),
            LanguageSetting.ForApp("Deskling Fixture", App.Languages)
        );
}
