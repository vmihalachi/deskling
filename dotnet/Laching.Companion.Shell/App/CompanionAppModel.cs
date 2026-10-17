using System.ComponentModel;
using System.Runtime.CompilerServices;
using Laching.Companion.Cadence;
using Laching.Companion.Coordination;
using Laching.Companion.Desktop;
using Laching.Companion.Figures;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;
using Laching.Companion.Settings;
using Laching.Companion.Shell.Desktop;
using Laching.Companion.Shell.Performance;
using Laching.Companion.Shell.Signals;
using Laching.Companion.Shell.Speech;
using Laching.Core.Scheduling;
using Laching.Core.Store;

namespace Laching.Companion.Shell.App;

/// <summary>
/// The shell of a desk companion app around <see cref="CompanionCoordinator"/> (Swift's <c>CompanionAppModel</c>): the
/// 20 s tick, the channels through a <see cref="DeliveryPerformer"/> (voice, chime, notification), the tray icon, the
/// character on the desktop, the windows, the store's start, the speech bubble with "don't say that again"
/// and its Undo, previews and the status pill's words. Views read <see cref="Mood"/>, <see cref="Bubble"/>,
/// <see cref="StatusLine"/> and the rest (each raises <see cref="PropertyChanged"/>), and call the actions. Every side
/// effect goes through the <see cref="ICompanionHost{TWindow}"/> or an injected interface, so tests drive a whole day
/// with a mock clock, hand-run timers and fakes. Asks for the notification permission only when the user turns that
/// channel on; never touches the network. UI thread only.
/// </summary>
/// <remarks>
/// The app subclasses it and supplies what only it knows: its menus' words, its character, its desktop layout, its own
/// preferences and reactions. Its views come in on the platform side. Every word comes from the app
/// (<see cref="CompanionAppConfiguration"/>, <see cref="MenuCopy"/>); the package ships none.
/// <code>
/// public sealed class AppModel(AppSettings settings, ICompanionHost&lt;AppWindow&gt; host, /* …the base's sources… */)
///     : CompanionAppModel&lt;AppWindow, AppSettings&gt;(settings, AppBank.Standard,
///         new WindowRoles&lt;AppWindow&gt;(AppWindow.Home, AppWindow.Onboarding, AppWindow.Settings, AppWindow.Support),
///         AppConfiguration.Standard, host /* … */)
/// {
///     public override MenuCopy MenuCopy =&gt; new(Say: Loc.Get("Say something"), /* … */);
///     public override DesktopLayout DesktopLayout { get; } = new(AppArt.Standard);
///     public override RiggedCharacter GlyphCharacter =&gt; AppCharacter.Standard;
/// }
/// </code>
/// </remarks>
public abstract class CompanionAppModel<TWindow, TSettings> : INotifyPropertyChanged, ICompanionLaunching
    where TWindow : notnull
    where TSettings : CompanionSettings
{
    /// <summary>The settings the scheduler reads: a change reaches it after 300 ms of quiet.</summary>
    private static readonly HashSet<string> TimingSettings =
    [
        nameof(CompanionSettings.Frequency),
        nameof(CompanionSettings.ActiveStartMinute),
        nameof(CompanionSettings.ActiveEndMinute),
        nameof(CompanionSettings.ActiveWeekdays),
        nameof(CompanionSettings.QuietHours),
        nameof(CompanionSettings.HushInFullScreen),
    ];

    private static readonly HashSet<string> DesktopSettings =
    [
        nameof(CompanionSettings.ShowOnDesktop),
        nameof(CompanionSettings.DesktopSize),
        nameof(CompanionSettings.DesktopPosition),
    ];

    private readonly ICompanionHost<TWindow> host;
    private readonly IClock clock;
    private readonly DesktopPresence presence = new();
    private IDisposable? tickTimer;
    private IDisposable? settingsDebounce;
    private IDisposable? bubbleTimer;
    private IDisposable? undoTimer;
    private IDisposable? desktopRecheck;
    private IDesktopCompanion? desktop;

    private string? bubble;
    private string? bubblePhraseId;
    private string? justMuted;
    private Mood mood = Rules.Mood.Awake;
    private string statusLine = "";
    private bool isSpeaking;
    private bool isVoicing;
    private bool isShushed;
    private NotificationPermission notificationPermission = NotificationPermission.NotDetermined;
    private bool isOnDesktop;
    private string? desktopBubble;
    private BubbleCountdown? desktopCountdown;
    private DateTimeOffset? desktopHiddenUntil;

    /// <param name="timeZone">The user's time zone: the day the scheduler counts and the times the status shows.</param>
    /// <param name="seed">The coordinator's seed (tests pass one; null picks one at random).</param>
    protected CompanionAppModel(
        TSettings settings,
        PhraseBank bank,
        WindowRoles<TWindow> roles,
        CompanionAppConfiguration configuration,
        ICompanionHost<TWindow> host,
        IClock clock,
        IIdleTimeProvider idle,
        IBusyStateProvider busy,
        TimeZoneInfo timeZone,
        ISpeaking speaker,
        IChimePlaying chime,
        ICompanionNotifying notifier,
        SignalHub signals,
        PurchaseStore purchases,
        VoiceLibrary voices,
        ulong? seed = null)
    {
        Settings = settings;
        Roles = roles;
        Configuration = configuration;
        this.host = host;
        this.clock = clock;
        TimeZone = timeZone;
        Voices = voices;
        Chime = chime;
        Notifier = notifier;
        Signals = signals;
        Purchases = purchases;
        Coordinator = new CompanionCoordinator(settings, bank, voices.LanguageCode, clock, idle, busy, timeZone, seed);
        Performer = new DeliveryPerformer(bank, speaker, chime, voices, notifier.Post, host.Timers)
        {
            Localize = configuration.Localize,
            TuneBubbleText = configuration.TuneBubbleText,
            BlipSound = configuration.Sounds.Blip,
        };
        Performer.OnBubble = ShowBubble;
        Performer.OnSpeechStarted = SpeechStarted;
        Performer.OnSpeechFinished = SpeechFinished;
        Performer.OnVoicingChanged = VoicingChanged;
        host.Windows.OnClose = id =>
        {
            if (EqualityComparer<TWindow>.Default.Equals(id, Roles.Support))
                Purchases.AcknowledgeTip();
        };
        signals.IsSpeaking = () => Performer.IsSpeaking;
        signals.OnEvent = HandleEvent;
        signals.OnCallStarted = StopSpeaking;
        signals.OnBusyPoll = BusyPolled;
        notifier.OnAction = HandleNotificationAction;
        purchases.Changed += () => OnPropertyChanged(nameof(IsSupporter));
        RefreshVoice();
        RefreshStatus();
        settings.PropertyChanged += SettingChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public TSettings Settings { get; }

    public CompanionCoordinator Coordinator { get; }

    public SignalHub Signals { get; }

    public PurchaseStore Purchases { get; }

    public WindowRoles<TWindow> Roles { get; }

    public CompanionAppConfiguration Configuration { get; }

    public TimeZoneInfo TimeZone { get; }

    /// <summary>The recorded voices in the app's language.</summary>
    public VoiceLibrary Voices { get; }

    public IChimePlaying Chime { get; }

    public DeliveryPerformer Performer { get; }

    public ICompanionNotifying Notifier { get; }

    public IWindowHost<TWindow> Windows => host.Windows;

    /// <summary>The tray icon, once launched.</summary>
    public IStatusItem? StatusItem { get; private set; }

    public bool IsLaunched { get; private set; }

    /// <summary>The bubble's text: the line, or the part of a joke being told.</summary>
    public string? Bubble { get => bubble; private set => Set(ref bubble, value); }

    /// <summary>The phrase in the bubble, for "don't say that again"; null for a preview.</summary>
    public string? BubblePhraseId { get => bubblePhraseId; private set => Set(ref bubblePhraseId, value); }

    /// <summary>The phrase just muted, while its Undo shows.</summary>
    public string? JustMuted { get => justMuted; private set => Set(ref justMuted, value); }

    public Mood Mood { get => mood; private set => Set(ref mood, value); }

    /// <summary>The status pill's words.</summary>
    public string StatusLine { get => statusLine; private set => Set(ref statusLine, value); }

    /// <summary>From the first part until the last finished, beats included.</summary>
    public bool IsSpeaking { get => isSpeaking; private set => Set(ref isSpeaking, value); }

    /// <summary>Audio is playing right now: the mouth moves (false in a joke's beat).</summary>
    public bool IsVoicing { get => isVoicing; private set => Set(ref isVoicing, value); }

    public bool IsShushed { get => isShushed; private set => Set(ref isShushed, value); }

    public NotificationPermission NotificationPermission
    {
        get => notificationPermission;
        private set => Set(ref notificationPermission, value);
    }

    /// <summary>The character stands on the desktop right now.</summary>
    public bool IsOnDesktop { get => isOnDesktop; private set => Set(ref isOnDesktop, value); }

    /// <summary>The desktop bubble's text while it's up.</summary>
    public string? DesktopBubble { get => desktopBubble; private set => Set(ref desktopBubble, value); }

    /// <summary>The desktop bubble running down, once the line has been said.</summary>
    public BubbleCountdown? DesktopCountdown { get => desktopCountdown; private set => Set(ref desktopCountdown, value); }

    /// <summary>The character is off the desktop for a while, until then.</summary>
    public DateTimeOffset? DesktopHiddenUntil
    {
        get => desktopHiddenUntil;
        private set
        {
            if (Set(ref desktopHiddenUntil, value))
                OnPropertyChanged(nameof(DesktopBackLine));
        }
    }

    // For the app (virtual, abstract)

    /// <summary>The menus' words.</summary>
    public abstract MenuCopy MenuCopy { get; }

    /// <summary>The desktop window's sizes and the bubble's place.</summary>
    public abstract DesktopLayout DesktopLayout { get; }

    /// <summary>The character the tray glyph draws (the worn skin, for an app with skins).</summary>
    public abstract RiggedCharacter GlyphCharacter { get; }

    /// <summary>Once at launch, before the status item exists: fonts and the like.</summary>
    public virtual void WillLaunch() { }

    /// <summary>The tray icon's right-click menu; <see cref="CompanionMenus.MenuBar"/> over <see cref="MenuCopy"/> by default.</summary>
    public virtual IReadOnlyList<MenuItem> MenuBarMenu() => CompanionMenus.MenuBar(this, MenuCopy);

    /// <summary>The desktop character's right-click menu; <see cref="CompanionMenus.Desktop"/> over <see cref="MenuCopy"/> by default.</summary>
    public virtual IReadOnlyList<MenuItem> DesktopMenu() => CompanionMenus.Desktop(this, MenuCopy);

    /// <summary>The tray glyph's mood; <see cref="Mood"/> by default.</summary>
    public virtual Mood GlyphMood => Mood;

    /// <summary>The recorded voice the app prefers in its language; null takes the library's default.</summary>
    public virtual VoiceId? PreferredVoice => null;

    /// <summary>A phrase is about to be said (a delivery or a preview), before its first part reaches the bubble.</summary>
    public virtual void WillSay(Phrase phrase) { }

    /// <summary>
    /// The bubble shows <paramref name="text"/>: a line, or the part of a joke being told. Sets <see cref="Bubble"/> and
    /// puts it up on the desktop; an app with more to show sets its own state, then calls the base.
    /// </summary>
    public virtual void ShowBubble(string text)
    {
        Bubble = text;
        ShowDesktopBubble(text);
    }

    /// <summary>
    /// The words in the bubble aren't the last of what's being said (a joke's setup shown without speech): the desktop
    /// bubble stays up without running down. False by default.
    /// </summary>
    public virtual bool DesktopBubbleWaits => false;

    /// <summary>Audio started or stopped: sets <see cref="IsVoicing"/> and redraws the glyph.</summary>
    public virtual void VoicingChanged(bool on)
    {
        if (IsVoicing == on)
            return;
        IsVoicing = on;
        RefreshGlyph();
    }

    /// <summary>A notification button was pressed: its action id. Nothing by default.</summary>
    public virtual void HandleNotificationAction(string id) { }

    /// <summary>The settings were just reset to their defaults, before the scheduler resumes.</summary>
    public virtual void SettingsDidReset() { }

    // Launch

    /// <summary>
    /// Once at launch (skipped under tests). Onboarding on the first run; otherwise Home, unless the system started the
    /// app at sign-in (<paramref name="showHome"/> false).
    /// </summary>
    public void Launch(bool showHome)
    {
        if (IsLaunched)
            return;
        IsLaunched = true;
        WillLaunch();
        Notifier.Configure();
        ApplyAppearance();
        var statusItem = host.MakeStatusItem();
        statusItem.Menu = MenuBarMenu;
        StatusItem = statusItem;
        RefreshNotificationPermission();
        _ = Purchases.StartAsync();
        tickTimer = host.Timers.Every(20, Tick);
        RefreshStatus();
        if (!Settings.HasCompletedOnboarding)
            Windows.Show(Roles.Onboarding);
        else if (showHome)
            Windows.Show(Roles.Home);
    }

    /// <summary>The user opened the app again while it was running (Start, a shortcut).</summary>
    public void Reopen()
    {
        if (Windows.ShowOpenWindows())
            return;
        Windows.Show(Settings.HasCompletedOnboarding ? Roles.Home : Roles.Onboarding);
    }

    // Ticks and deliveries

    /// <summary>Every 20 s: the scheduler decides, the rules judge, and a line is delivered or not.</summary>
    public void Tick()
    {
        var snapshot = Signals.Snapshot();
        if (Coordinator.Tick(snapshot) is { } delivery)
            Perform(delivery);
        RefreshStatus(snapshot);
    }

    private void HandleEvent(Trigger trigger)
    {
        if (Coordinator.Event(trigger, Signals.Snapshot()) is { } delivery)
            Perform(delivery);
        RefreshStatus();
    }

    private void Perform(Coordination.Delivery delivery)
    {
        BubblePhraseId = delivery.Phrase.Id;
        WillSay(delivery.Phrase);
        Performer.Perform(delivery, Settings, Voice);
    }

    /// <summary>The recorded voice in use, null where the language has none (the system voice speaks).</summary>
    public VoiceId? Voice => Voices.Voice(PreferredVoice);

    /// <summary>
    /// Lines with no current recording in the voice in use are kept out of the picks (none without a recorded voice).
    /// Call it when the app's voice preference changes.
    /// </summary>
    public void RefreshVoice() => Coordinator.UnavailableIDs = Performer.UnavailableIds(Voice);

    /// <summary>The desktop bubble stays up while the line is said, then lingers.</summary>
    private void SpeechStarted()
    {
        CancelBubbleTimer();
        DesktopCountdown = null;
        IsSpeaking = true;
        Signals.SpeechStarted();
        RefreshStatus();
    }

    private void SpeechFinished()
    {
        if (!IsSpeaking)
            return;
        IsSpeaking = false;
        IsVoicing = false;
        Signals.SpeechEnded();
        LingerDesktopBubble();
        RefreshStatus();
    }

    /// <summary>Cuts the current line or joke, the parts still waiting included (a call started, Shh).</summary>
    public virtual void StopSpeaking()
    {
        Performer.Stop();
        SpeechFinished();
    }

    // Actions

    /// <summary>
    /// Say something: the panel button, a click on the desktop character, the notification's button. Mid-line (a
    /// notification's Say more comes while its line is still heard) it cuts that line, as Shh does, and says the new one.
    /// </summary>
    public void SayNow()
    {
        if (Coordinator.Poke(Signals.Snapshot()) is { } delivery)
        {
            if (Performer.IsSpeaking)
                StopSpeaking();
            Perform(delivery);
        }
        RefreshStatus();
    }

    public void Shush(ChatterCadence.Shush shush)
    {
        StopSpeaking();
        Chime.Stop();
        Coordinator.Shush(shush);
        PlayChime(Configuration.Sounds.Shh);
        RefreshStatus();
    }

    /// <summary>
    /// Pressing and holding the character in the panel: the quick hour of quiet. Does nothing while shushed, where Wake
    /// up stays the way out. Returns whether it shushed.
    /// </summary>
    public bool HoldToShush()
    {
        if (IsShushed)
            return false;
        Shush(ChatterCadence.Shush.Hour);
        return true;
    }

    /// <summary>Ends Shh early; the character says one line about it.</summary>
    public void Wake()
    {
        if (Coordinator.Wake(Signals.Snapshot()) is { } delivery)
            Perform(delivery);
        RefreshStatus();
    }

    /// <summary>
    /// "Don't say that again" on the speech bubble: the line leaves the picks for good (Settings can bring muted lines
    /// back), with an Undo for a few seconds.
    /// </summary>
    public void MuteBubbleLine()
    {
        if (BubblePhraseId is not { } id)
            return;
        Settings.MutedPhraseIDs = new HashSet<string>(Settings.MutedPhraseIDs, StringComparer.Ordinal) { id };
        JustMuted = id;
        undoTimer?.Dispose();
        undoTimer = host.Timers.After(8, () => JustMuted = null);
    }

    public void UndoMute()
    {
        if (JustMuted is not { } id)
            return;
        var muted = new HashSet<string>(Settings.MutedPhraseIDs, StringComparer.Ordinal);
        muted.Remove(id);
        Settings.MutedPhraseIDs = muted;
        JustMuted = null;
        undoTimer?.Dispose();
        undoTimer = null;
    }

    /// <summary>
    /// Onboarding's channel tiles: turning one on previews it once, the chime's blip or <paramref name="line"/> said
    /// aloud. Only on that tap, and never while busy (a call, full screen), with the output muted or mid-line.
    /// </summary>
    public void PreviewChannel(Channels channel, Phrase? line)
    {
        if (channel == Channels.Chime)
        {
            if (!CanPreview)
                return;
            PlayChime(Configuration.Sounds.Blip, always: true);
        }
        else if (channel == Channels.Voice && line is not null)
        {
            Preview(line);
        }
    }

    /// <summary>Says <paramref name="line"/> once on the user's tap (onboarding's samples), under the same guards as a channel preview.</summary>
    public void Preview(Phrase line)
    {
        if (!CanPreview || line.LocalizedParts(Configuration.Localize).Count == 0)
            return;
        Say(line);
    }

    /// <summary>Previews may play: nothing busy, the output not muted, nothing being said.</summary>
    public bool CanPreview
    {
        get
        {
            var snapshot = Signals.Snapshot();
            return snapshot.Busy.Count == 0 && snapshot.IsOutputMuted != true && !IsSpeaking;
        }
    }

    /// <summary>
    /// Says <paramref name="phrase"/> now, the way Settings plays a sample (Test voice): spoken, without a chime or a
    /// notification; the bubble follows it but it can't be muted. No guards: only on the user's tap.
    /// </summary>
    public void Say(Phrase phrase)
    {
        Performer.Stop();
        BubblePhraseId = null;
        WillSay(phrase);
        Performer.Preview(phrase, Settings, Voice);
    }

    /// <summary>Home appeared: the tray glyph waves back, with the hello sound when the chime channel is on.</summary>
    public void WaveFromHome()
    {
        StatusItem?.Wave();
        PlayChime(Configuration.Sounds.Hello);
    }

    public void OpenHome() => Windows.Show(Roles.Home);

    public void OpenSettings() => Windows.Show(Roles.Settings);

    public void OpenSupport() => Windows.Show(Roles.Support);

    public void OpenOnboarding() => Windows.Show(Roles.Onboarding);

    public void CloseWindow(TWindow id) => Windows.Close(id);

    public void Quit() => host.Quit();

    /// <summary>The supporter product is owned (<see cref="CompanionAppConfiguration.SupporterProduct"/>).</summary>
    public bool IsSupporter => Configuration.SupporterProduct is { } product && Purchases.IsOwned(product);

    /// <summary>Plays <paramref name="sound"/> when the chime channel is on (or <paramref name="always"/>, for a preview the user asked for).</summary>
    private void PlayChime(string? sound, bool always = false)
    {
        if (sound is null || !(always || Settings.Channels.Contains(Channels.Chime)))
            return;
        Chime.Play(sound, Settings.SpeechVolume, null);
    }

    // Onboarding and settings

    /// <summary>
    /// Get started: onboarding is done, the notification permission is asked for only if that channel was picked, and
    /// Home takes over.
    /// </summary>
    public void CompleteOnboarding()
    {
        Settings.HasCompletedOnboarding = true;
        if (Settings.Channels.Contains(Channels.Notification))
            RequestNotificationPermission();
        if (!IsLaunched)
            return;
        Windows.Close(Roles.Onboarding);
        Windows.Show(Roles.Home);
    }

    /// <summary>
    /// Turns a channel on or off. Turning the notification channel on is the one moment the app asks for permission
    /// (onboarding writes the channels directly and asks on Get started).
    /// </summary>
    public void SetChannel(Channels channel, bool on)
    {
        if (on)
        {
            Settings.Channels |= channel;
            if (channel == Channels.Notification)
                RequestNotificationPermission();
        }
        else
        {
            Settings.Channels = new Channels(Settings.Channels.RawValue & ~channel.RawValue);
        }
    }

    public void RequestNotificationPermission() =>
        Notifier.RequestAuthorization(granted =>
        {
            NotificationPermission = granted ? NotificationPermission.Authorized : NotificationPermission.Denied;
            Signals.NotificationsAuthorized = granted;
        });

    public void RefreshNotificationPermission() =>
        Notifier.RefreshAuthorization(permission =>
        {
            NotificationPermission = permission;
            Signals.NotificationsAuthorized = permission == NotificationPermission.Authorized;
        });

    /// <summary>The system's notification settings for the app, for when permission was denied.</summary>
    public void OpenNotificationSettings() => host.OpenNotificationSettings();

    /// <summary>Every preference back to its default (onboarding stays done) and Shh over, without a line.</summary>
    public void ResetSettings()
    {
        StopSpeaking();
        presence.ShowNow();
        Settings.ResetToDefaults();
        SettingsDidReset();
        if (Coordinator.Scheduler.PausedUntil is not null)
            Coordinator.Scheduler.Resume();
        Coordinator.SettingsChanged();
        RefreshStatus();
    }

    /// <summary>
    /// Calls <paramref name="react"/> whenever one of the settings named in <paramref name="propertyNames"/> changes:
    /// for the app's own preferences (<c>nameof(AppSettings.Voice)</c>).
    /// </summary>
    public void ObserveSettings(Action react, params string[] propertyNames)
    {
        var names = propertyNames.ToHashSet(StringComparer.Ordinal);
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is { } name && names.Contains(name))
                react();
        };
    }

    /// <summary>
    /// Watches the settings: timing changes reach the scheduler after 300 ms of quiet; appearance and the desktop
    /// apply right away.
    /// </summary>
    private void SettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case { } name when TimingSettings.Contains(name):
                TimingSettingsChanged();
                break;
            case nameof(CompanionSettings.Appearance):
                ApplyAppearance();
                break;
            case { } name when DesktopSettings.Contains(name):
                if (name == nameof(CompanionSettings.ShowOnDesktop))
                    OnPropertyChanged(nameof(DesktopBackLine));
                RefreshStatus();
                break;
        }
    }

    private void TimingSettingsChanged()
    {
        settingsDebounce?.Dispose();
        settingsDebounce = host.Timers.After(0.3, () =>
        {
            settingsDebounce = null;
            Coordinator.SettingsChanged();
            RefreshStatus();
        });
    }

    private void ApplyAppearance()
    {
        if (IsLaunched)
            host.ApplyAppearance(Settings.Appearance);
    }

    // Status

    public void RefreshStatus(Snapshot? snapshot = null)
    {
        snapshot ??= Signals.Snapshot();
        IsShushed = Coordinator.IsShushed;
        Mood = Coordinator.Mood(snapshot);
        StatusLine = StatusLineFor(
            Coordinator.Scheduler.Status, snapshot, clock.Now, TimeZone, Coordinator.Scheduler.EndOfToday, Configuration.Status);
        RefreshGlyph();
        RefreshDesktop(snapshot);
    }

    /// <summary>
    /// Redraws the tray glyph: <see cref="GlyphCharacter"/> in <see cref="GlyphMood"/>, stepping the talking frames while
    /// words are heard. Call it when the character changes (a skin).
    /// </summary>
    public void RefreshGlyph() => StatusItem?.Show(GlyphCharacter, GlyphMood, IsVoicing);

    /// <summary>The status pill's words, from the scheduler's status and the moment (Swift's <c>statusLine</c>).</summary>
    public static string StatusLineFor(
        SchedulerStatus status, Snapshot snapshot, DateTimeOffset now, TimeZoneInfo timeZone, DateTimeOffset endOfToday, StatusCopy copy)
    {
        switch (status)
        {
            case SchedulerStatus.Paused paused:
                var until = paused.Until;
                if (until >= CompanionCoordinator.DistantFuture)
                    return copy.ShhUntilYouSay();
                if (until >= endOfToday && until <= endOfToday.AddSeconds(60))
                    return copy.ShhRestOfToday();
                var local = TimeZoneInfo.ConvertTime(until, timeZone);
                return local.Date == TimeZoneInfo.ConvertTime(now, timeZone).Date ? copy.ShhUntil(local) : copy.ShhUntilDay(local);
            case SchedulerStatus.Postponed { Reason: BusyReason.Call }:
                return copy.QuietCall();
            case SchedulerStatus.Postponed { Reason: BusyReason.FullScreen }:
                return copy.QuietFullScreen();
            case SchedulerStatus.OutsideActiveHours:
                return copy.OffHours();
            case SchedulerStatus.Away:
                return copy.Asleep();
            case SchedulerStatus.Scheduled scheduled:
                if (snapshot.Busy.Contains(BusyReason.Call))
                    return copy.QuietCall();
                if (snapshot.Busy.Contains(BusyReason.FullScreen))
                    return copy.QuietFullScreen();
                if (snapshot.IsAway)
                    return copy.Asleep();
                return copy.NextInMinutes(Math.Max(1, (int)Math.Ceiling((scheduled.Date - now).TotalMinutes)));
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }

    // Desktop

    public void SetShowOnDesktop(bool on)
    {
        if (on)
            presence.ShowNow();
        Settings.ShowOnDesktop = on;
        RefreshStatus();
    }

    public void HideDesktop(DesktopHide span)
    {
        switch (span)
        {
            case DesktopHide.Hour:
                presence.Hide(clock.Now.AddSeconds(DesktopPresence.HourHideSeconds));
                break;
            case DesktopHide.RestOfToday:
                presence.Hide(Coordinator.Scheduler.EndOfToday);
                break;
            case DesktopHide.UntilShown:
                presence.ShowNow();
                Settings.ShowOnDesktop = false;
                break;
        }
        RefreshStatus();
    }

    /// <summary>Ends a hide for a while (and turns the desktop character on if it was off).</summary>
    public void ShowDesktopNow()
    {
        presence.ShowNow();
        Settings.ShowOnDesktop = true;
        RefreshStatus();
    }

    /// <summary>A pinch on the character: one size bigger or smaller.</summary>
    public void StepDesktopSize(int step) => Settings.DesktopSize = Settings.DesktopSize.Stepped(step);

    public void MoveDesktop(DesktopPosition position) => Settings.DesktopPosition = position;

    /// <summary>Back to the bottom right of the main screen.</summary>
    public void ResetDesktopPosition() => Settings.DesktopPosition = null;

    public void DismissDesktopBubble()
    {
        ClearDesktopBubble();
        desktop?.HideBubble(animated: !host.ReduceMotion);
    }

    /// <summary>Drops the bubble's words, its countdown and its timer; the window follows on its next update.</summary>
    private void ClearDesktopBubble()
    {
        CancelBubbleTimer();
        DesktopBubble = null;
        DesktopCountdown = null;
    }

    /// <summary>The panel row's words while the character is off the desktop for a while.</summary>
    public string? DesktopBackLine
    {
        get
        {
            if (!Settings.ShowOnDesktop || DesktopHiddenUntil is not { } until)
                return null;
            if (until >= Coordinator.Scheduler.EndOfToday)
                return Configuration.Desktop.BackTomorrow();
            return Configuration.Desktop.BackAt(TimeZoneInfo.ConvertTime(until, TimeZone));
        }
    }

    /// <summary>Words were delivered: up in the desktop bubble, lingering once they're said (or right away when nothing is spoken).</summary>
    private void ShowDesktopBubble(string text)
    {
        DesktopBubble = text;
        LingerDesktopBubble();
        UpdateDesktopWindow();
    }

    private void LingerDesktopBubble()
    {
        CancelBubbleTimer();
        DesktopCountdown = null;
        if (DesktopBubble is not { } text || IsSpeaking || DesktopBubbleWaits)
            return;
        var seconds = DesktopPresence.BubbleSeconds(text);
        DesktopCountdown = new BubbleCountdown(clock.Now, seconds);
        desktop?.SetCountdown(DesktopCountdown);
        bubbleTimer = host.Timers.After(seconds, DismissDesktopBubble);
    }

    private void CancelBubbleTimer()
    {
        bubbleTimer?.Dispose();
        bubbleTimer = null;
    }

    /// <summary>
    /// Every poll of the busy watch: a change refreshes everything; while busy, the presence learns the moment, so the
    /// character comes back 10 s after the call really ended.
    /// </summary>
    private void BusyPolled(IReadOnlySet<BusyReason> reasons, bool changed)
    {
        if (changed)
            RefreshStatus();
        else if (reasons.Count > 0)
            presence.Update(Settings.ShowOnDesktop, new Snapshot { Busy = reasons }, clock.Now);
    }

    private void RefreshDesktop(Snapshot snapshot)
    {
        var shown = presence.Update(Settings.ShowOnDesktop, snapshot, clock.Now);
        IsOnDesktop = shown;
        DesktopHiddenUntil = presence.HiddenUntil;
        if (!shown && DesktopBubble is not null)
            ClearDesktopBubble();
        Signals.WatchForDesktop(IsLaunched && Settings.ShowOnDesktop && presence.HiddenUntil is null);
        ScheduleDesktopRecheck();
        UpdateDesktopWindow();
    }

    /// <summary>
    /// Refreshes again when a hide for a while ends or the reappear delay after a call runs out, instead of waiting for
    /// the next tick.
    /// </summary>
    private void ScheduleDesktopRecheck()
    {
        desktopRecheck?.Dispose();
        desktopRecheck = null;
        if (!IsLaunched || !Settings.ShowOnDesktop)
            return;
        var now = clock.Now;
        DateTimeOffset?[] moments = [presence.HiddenUntil, presence.LastBusy?.AddSeconds(DesktopPresence.ReappearDelaySeconds)];
        var next = moments.OfType<DateTimeOffset>().Where(m => m > now).DefaultIfEmpty().Min();
        if (next == default)
            return;
        desktopRecheck = host.Timers.After((next - now).TotalSeconds + 0.1, () => RefreshStatus());
    }

    /// <summary>Makes the desktop window the first time it's needed, then matches it to the state.</summary>
    private void UpdateDesktopWindow()
    {
        if (!IsLaunched)
            return;
        if (desktop is null)
        {
            if (!Settings.ShowOnDesktop)
                return;
            desktop = MakeDesktop();
        }
        desktop.SetSize(Settings.DesktopSize);
        desktop.SetPosition(Settings.DesktopPosition);
        var animated = !host.ReduceMotion;
        if (IsOnDesktop)
        {
            desktop.Show(animated);
            if (DesktopBubble is { } text)
                desktop.ShowBubble(text, DesktopCountdown, animated);
            else
                desktop.HideBubble(animated);
        }
        else
        {
            desktop.Hide(animated);
        }
    }

    private IDesktopCompanion MakeDesktop()
    {
        var made = host.MakeDesktop(DesktopLayout, Settings.DesktopSize, Settings.DesktopPosition, Configuration.Desktop.SayActionName());
        made.OnSay = SayNow;
        made.OnMoved = MoveDesktop;
        made.OnPinch = StepDesktopSize;
        made.OnBubbleDismissed = DismissDesktopBubble;
        made.Menu = DesktopMenu;
        return made;
    }

    /// <summary>Sets a property's field and raises <see cref="PropertyChanged"/> when the value changed.</summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
