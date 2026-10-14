using Deskling.Companion.Cadence;
using Deskling.Companion.Desktop;
using Deskling.Companion.Figures;
using Deskling.Companion.Phrases;
using Deskling.Companion.Settings;
using Deskling.Companion.Shell.App;
using Deskling.Companion.Shell.Desktop;
using Deskling.Companion.Shell.Signals;
using Deskling.Companion.Shell.Speech;
using Deskling.Core.Scheduling;
using Deskling.Core.Store;
using Deskling.Testing.Fixtures;
using Deskling.Windows.Companion.Shell;
using Deskling.Windows.Companion.Views;
using Size = Windows.Foundation.Size;

namespace FixtureApp;

/// <summary>The fixture app's windows.</summary>
public enum FixtureWindow
{
    Home,
    Onboarding,
    Settings,
    Support,
}

/// <summary>
/// The fixture app's model: Deskling's fixture bank and its vector character in the tray, placeholder words, and what the
/// hand checks need besides (a line long enough to start a call during, a clock that skips to the next line).
/// </summary>
public sealed class FixtureModel : CompanionAppModel<FixtureWindow, CompanionSettings>
{
    public const string Supporter = "fixture.supporter";

    public const string SayMore = "SAY_MORE";

    public const string ShhHour = "SHH_HOUR";

    /// <summary>
    /// The fixture's defaults, made for checking by hand: every hour of every day is active.
    /// </summary>
    public static readonly CompanionDefaults Defaults = FixtureDefaults.Value with
    {
        ActiveStartMinute = 0,
        ActiveEndMinute = 24 * 60,
        ActiveWeekdays = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 },
    };

    public static new readonly CompanionAppConfiguration Configuration = new(
        StatusItemTooltip: "Deskling fixture",
        AccessibilityLabel: "Fixture",
        Sounds: new CompanionSounds(Blip: FixtureSounds.Blip, Shh: FixtureSounds.Shh, Hello: FixtureSounds.Hello),
        Status: new StatusCopy(
            ShhUntilYouSay: () => "Shh until you say",
            ShhRestOfToday: () => "Shh for the rest of today",
            ShhUntil: time => $"Shh until {time:t}",
            ShhUntilDay: day => $"Shh until {day:ddd} {day:t}",
            QuietCall: () => "Quiet: you're on a call",
            QuietFullScreen: () => "Quiet: full screen",
            OffHours: () => "Off hours",
            Asleep: () => "Asleep",
            NextInMinutes: minutes => $"Next line in about {minutes} min"),
        Desktop: new DesktopCopy(SayActionName: () => "Say something", BackTomorrow: () => "Back tomorrow", BackAt: time => $"Back at {time:t}"),
        TuneBubbleText: () => "♪",
        Localize: (_, source) => source,
        SupporterProduct: Supporter);

    /// <summary>Long enough to start a call while it plays.</summary>
    private static readonly Phrase LongLine = new(
        "fixture-long",
        new PhraseKind.Line(
            "This is the fixture's long line, so there is time to start a call while it plays. Open the Camera app or "
                + "join a call now: the line should stop within a second, and the status should say you're on a call. If "
                + "you can still hear this sentence, the call wasn't noticed."),
        [Tag.General]);

    private readonly IClock clock;
    private readonly ITimers timers;
    private string clockLine = "";

    public FixtureModel(
        CompanionSettings settings,
        PhraseBank bank,
        CharacterArt art,
        CharacterArt rasterArt,
        WindowsCompanionHost<FixtureWindow> host,
        IClock clock,
        IIdleTimeProvider idle,
        IBusyStateProvider busy,
        ISpeaking speaker,
        IChimePlaying chime,
        ICompanionNotifying notifier,
        SignalHub signals,
        PurchaseStore purchases)
        : base(
            settings,
            bank,
            new WindowRoles<FixtureWindow>(FixtureWindow.Home, FixtureWindow.Onboarding, FixtureWindow.Settings, FixtureWindow.Support),
            Configuration,
            host,
            clock,
            idle,
            busy,
            TimeZoneInfo.Local,
            speaker,
            chime,
            notifier,
            signals,
            purchases,
            new VoiceLibrary(null, "en", allowTesting: false))
    {
        this.clock = clock;
        timers = host.Timers;
        DesktopLayout = new DesktopLayout(art);
        GlyphCharacter = new RiggedCharacter(art);
        RasterCharacter = new RiggedCharacter(rasterArt);
    }

    /// <summary>The fixture's paints (<c>Tests/Fixtures/characters/palette.json</c>) over Windows' colors.</summary>
    public static CompanionPalette Palette { get; } =
        CompanionPalette.System with
        {
            Paints = role =>
                role.RawValue switch
                {
                    "body" => ThemedColor.Fixed(0xFFD25A),
                    "edge" => ThemedColor.Fixed(0xEBAA2E),
                    "eye" => ThemedColor.Fixed(0x2B1A0E),
                    "blush" => ThemedColor.Fixed(0xF4A08C),
                    "stem" => ThemedColor.Fixed(0x6E9E45),
                    "inkSoft" => ThemedColor.Hex(0x7A6A55, 0xB8A890),
                    _ => ThemedColor.Hex(0x000000, 0xFFFFFF),
                },
        };

    public override MenuCopy MenuCopy { get; } = new(
        Say: "Say something",
        WakeUp: "Wake up",
        Shh: "Shh",
        ForAnHour: "For an hour",
        ForTheRestOfToday: "For the rest of today",
        UntilISay: "Until I say",
        DesktopSubmenu: "On the desktop",
        Show: "Show",
        ShowNow: "Show now",
        HideForAnHour: "Hide for an hour",
        HideForTheRestOfToday: "Hide for the rest of today",
        Size: "Size",
        Hide: "Hide",
        UntilIShowItAgain: "Until I show it again",
        Settings: "Settings…",
        Support: "Support…",
        ThanksForSupporting: "Thanks for supporting ♥…",
        Quit: "Quit",
        SizeLabel: size => size.RawValue());

    public override DesktopLayout DesktopLayout { get; }

    public override RiggedCharacter GlyphCharacter { get; }

    /// <summary>The raster fixture, drawn on Home from its PNGs.</summary>
    public RiggedCharacter RasterCharacter { get; }

    /// <summary>How far the fixture's clock runs ahead of the wall clock, once skipped.</summary>
    public string ClockLine { get => clockLine; private set => Set(ref clockLine, value); }

    /// <summary>Each window's title and size.</summary>
    public static WindowSpec Spec(FixtureWindow id) =>
        id switch
        {
            FixtureWindow.Home => new WindowSpec("Deskling fixture", new Size(440, 620)),
            FixtureWindow.Onboarding => new WindowSpec("Welcome", new Size(420, 320), IsExclusive: true),
            FixtureWindow.Settings => new WindowSpec("Settings", new Size(420, 600)),
            FixtureWindow.Support => new WindowSpec("Support", new Size(460, 580)),
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
        };

    public override void HandleNotificationAction(string id)
    {
        switch (id)
        {
            case SayMore:
                SayNow();
                break;
            case ShhHour:
                Shush(ChatterCadence.Shush.Hour);
                break;
        }
    }

    public void ShushForAnHour() => Shush(ChatterCadence.Shush.Hour);

    public void SayLongLine() => Say(LongLine);

    /// <summary>Skips the clock to the next line's time, then ticks: the line comes now, unless something holds it.</summary>
    public void MakeLineDue()
    {
        if (clock is SkippingClock skipping && Coordinator.Scheduler.NextFireDate is { } next && next > skipping.Now)
        {
            skipping.Skip(next - skipping.Now + TimeSpan.FromSeconds(1));
            ClockLine = $"The clock runs {skipping.Skipped:h\\:mm\\:ss} ahead.";
        }
        Tick();
    }

    /// <summary><see cref="MakeLineDue"/> in 10 s: time to go full screen.</summary>
    public void MakeLineDueSoon() => timers.After(10, MakeLineDue);

    /// <summary>The character on the desktop, on or off (the tray menu's "On the desktop" has the rest).</summary>
    public void ToggleDesktop() => SetShowOnDesktop(!Settings.ShowOnDesktop);

    /// <summary>The tray panel, as a click on the icon opens it (for when the icon sits in the overflow).</summary>
    public void OpenPanel() => StatusItem?.OpenPanel();
}

/// <summary>The wall clock, skipped ahead by hand (the scheduler knows the time only through it).</summary>
public sealed class SkippingClock : IClock
{
    public TimeSpan Skipped { get; private set; }

    public DateTimeOffset Now => DateTimeOffset.Now + Skipped;

    public void Skip(TimeSpan span) => Skipped += span;
}
