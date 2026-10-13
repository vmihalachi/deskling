using Deskling.Companion.Settings;
using Deskling.Companion.Shell.App;
using Deskling.Companion.Shell.Signals;
using Deskling.Core.Settings;
using Deskling.Core.Store;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Shell;
using Deskling.Windows.Companion.Signals;
using Deskling.Windows.Companion.Speech;
using Deskling.Windows.System;
using FixtureApp.Views;

namespace FixtureApp;

/// <summary>
/// The fixture app's <c>App.xaml</c>: Deskling's <see cref="CompanionApp"/> makes one copy run and launches the model.
/// Settings live in memory, so every launch starts fresh: onboarding done, unless <c>--first-run</c>.
/// </summary>
public partial class App : CompanionApp
{
    /// <summary>The tray icon's identity: Windows keeps its place in the notification area by it.</summary>
    private static readonly Guid TrayIconId = new("5b0f6c1e-3d2a-4f87-9e4b-7a1c2d3e4f50");

    public App()
    {
        InitializeComponent();
    }

    protected override ICompanionLaunching MakeCompanion()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "conformance");
        FixtureModel? made = null;
        FixtureModel Model() => made ?? throw new InvalidOperationException("The views open after the model is made.");
        var host = new WindowsCompanionHost<FixtureWindow>(
            new CompanionViews<FixtureWindow>(FixtureModel.Spec, id => Content(Model(), id), () => new PanelView(Model())),
            TrayIconId,
            FixtureModel.Configuration.StatusItemTooltip
        );
        var notifier = new CompanionNotifier("Fixture", [(FixtureModel.SayMore, "Say more"), (FixtureModel.ShhHour, "Shh for an hour")]);
        host.OnQuit = notifier.Dispose;

        var store = new InMemorySettingsStore();
        var settings = new CompanionSettings(store, FixtureModel.Defaults);
        settings.HasCompletedOnboarding = !Environment.GetCommandLineArgs().Contains("--first-run");
        var idle = new SystemIdleTimeProvider();
        var busy = new SystemBusyStateProvider();
        var signals = new SignalHub(
            idle,
            busy,
            new SystemAudioOutput(),
            new SystemScreenLock("Deskling.Fixture.SessionLock"),
            new SystemPowerSource(),
            new SystemAppearance(),
            host.Timers
        );
        var catalog = new ProductCatalog([FixtureModel.Supporter], ["fixture.tip"]);
        made = new FixtureModel(
            settings,
            FixtureBank.Load(root),
            FixtureArt.Load(root, FixtureIds.VectorSkin.RawValue),
            host,
            new SkippingClock(),
            idle,
            busy,
            new Speaker("en"),
            new ChimePlayer(FixtureSounds.Write()),
            notifier,
            signals,
            new PurchaseStore(catalog, new MockStoreBackend(catalog), store)
        );
        return made;
    }

    private static Microsoft.UI.Xaml.UIElement Content(FixtureModel model, FixtureWindow id) =>
        id switch
        {
            FixtureWindow.Home => new HomeView(model),
            FixtureWindow.Onboarding => new OnboardingView(model),
            FixtureWindow.Settings => new SettingsView(model),
            FixtureWindow.Support => new SupportView(),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };
}
