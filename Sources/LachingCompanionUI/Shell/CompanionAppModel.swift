#if os(macOS)
    import AppKit
    import Foundation
    import LachingCompanion
    import LachingCore
    import LachingShell
    import LachingStore
    import Observation
    import SwiftUI

    /// The shell of a desk companion app around `CompanionCoordinator`: the 20 s tick, the channels through a
    /// `DeliveryPerformer` (voice, chime, notification), the menu bar item, the character on the desktop, the windows,
    /// the store's start, the speech bubble with "don't say that again" and its Undo, previews and the status
    /// pill's words. Views read `mood`, `bubble`, `statusLine` and the rest, and call the actions. Every side effect goes
    /// through an injected protocol or provider, so tests drive a whole day with a mock clock and fake channels. Asks
    /// for the notification permission only when the user turns that channel on; never touches the network.
    ///
    /// The app subclasses it, `@Observable` too, and supplies what only it knows: its windows' and panel's views, its
    /// menus' words, its character, its own preferences and reactions. Every word comes from the app
    /// (`CompanionAppConfiguration`, `menuCopy`); the package ships none.
    ///
    ///     @MainActor @Observable
    ///     final class AppModel: CompanionAppModel<AppWindow, AppSettings> {
    ///         init(settings: AppSettings, clock: any Clock, /* …the base's providers… */) {
    ///             super.init(settings: settings, bank: .app, roles: WindowRoles(home: .home, onboarding: .onboarding,
    ///                 settings: .settings, support: .support), configuration: .app, clock: clock /* … */)
    ///             observe { _ = self.settings.voice } then: { self.refreshVoice() }
    ///         }
    ///
    ///         override func windowContent(for id: AppWindow) -> AnyView { AnyView(AppWindowRoot { … }) }
    ///         override func panelContent() -> AnyView { AnyView(AppWindowRoot { MenuPanel(model: self) }) }
    ///         override var menuCopy: MenuCopy { MenuCopy(say: String(localized: "Say something"), …) }
    ///         override func desktopCharacter() -> AnyView { AnyView(DesktopCharacter(model: self)) }
    ///         override var desktopLayout: DesktopLayout { DesktopView.layout }
    ///         override var glyphCharacter: RiggedCharacter { .app }
    ///     }
    ///
    /// The members marked *required* trap until overridden.
    @MainActor
    @Observable
    open class CompanionAppModel<Window: WindowID, Settings: CompanionSettings> {
        public let settings: Settings
        public let coordinator: CompanionCoordinator
        public let signals: SignalHub
        public let purchases: PurchaseStore
        public let roles: WindowRoles<Window>
        public let configuration: CompanionAppConfiguration

        /// The latest line (or the part of a joke being told), shown in the panel's speech bubble.
        public private(set) var bubble: String?
        /// The phrase behind `bubble`, for "Don't say that again"; nil after a preview.
        public private(set) var bubblePhraseID: String?
        /// The line just muted, while its Undo is offered.
        public private(set) var justMuted: String?
        public private(set) var mood: Mood = .awake
        /// "Next in about 20 min", "Quiet: you're on a call", "Shh until 3 PM"…
        public private(set) var statusLine = ""
        /// From the first part of a line until the last finished, beats included.
        public private(set) var isSpeaking = false
        /// Audio is playing: the mouth moves. False in the beat between a setup and its punchline.
        public private(set) var isVoicing = false
        /// The recorded clip's mouth level (0–3) right now, or nil for the mood's timer: pass `{ model.mouthLevel }` to
        /// `CharacterView`, which reads it every frame (not observed).
        public var mouthLevel: Int? { performer.mouthLevel }
        public private(set) var isShushed = false
        public private(set) var notificationPermission: NotificationPermission = .notDetermined
        /// The character stands on the desktop right now: the setting is on, it isn't hidden for a while, and no call
        /// or full-screen app holds it back.
        public private(set) var isOnDesktop = false
        /// The words in the desktop bubble while it's up.
        public private(set) var desktopBubble: String?
        /// The desktop bubble running down once its words are said, so its dial shows how long it stays.
        public private(set) var desktopCountdown: BubbleCountdown?
        /// The character is off the desktop for a while, until then ("Hide for an hour", "for the rest of today").
        public private(set) var desktopHiddenUntil: Date?

        @ObservationIgnored public let clock: any Clock
        @ObservationIgnored public let calendar: Calendar
        /// The character's recorded voices in the app's language (none: the system voice speaks).
        @ObservationIgnored public let voices: VoiceLibrary
        @ObservationIgnored public let chime: any ChimePlaying
        /// Says each delivery: the bubble, the blip, the words (a joke in beats) and the banner.
        @ObservationIgnored public let performer: DeliveryPerformer
        @ObservationIgnored public let notifier: any CompanionNotifying
        @ObservationIgnored public private(set) var windows: WindowManager<Window>!
        /// The menu bar item, from launch on.
        @ObservationIgnored public private(set) var statusItem: StatusItemController?
        /// `launch(showHome:)` ran (never under tests).
        @ObservationIgnored public private(set) var isLaunched = false
        @ObservationIgnored private var timer: Timer?
        @ObservationIgnored private var settingsDebounce: Timer?
        @ObservationIgnored private var presence = DesktopPresence()
        @ObservationIgnored private var desktop: DesktopCompanion?
        @ObservationIgnored private var bubbleTimer: Timer?
        @ObservationIgnored private var undoTimer: Timer?
        @ObservationIgnored private var desktopRecheck: Timer?

        /// `schedule` runs the silent parts' and beats' timers (tests pass their own clock).
        public init(
            settings: Settings, bank: PhraseBank, roles: WindowRoles<Window>, configuration: CompanionAppConfiguration,
            clock: any Clock, idle: any IdleTimeProvider, busy: any BusyStateProvider, calendar: Calendar, speaker: any Speaking,
            chime: any ChimePlaying, notifier: any CompanionNotifying, signals: SignalHub, purchases: PurchaseStore,
            voices: VoiceLibrary = VoiceLibrary(), seed: UInt64 = UInt64.random(in: 0...UInt64.max),
            schedule: SpeechSequencer.Schedule? = nil
        ) {
            self.settings = settings
            self.roles = roles
            self.configuration = configuration
            self.clock = clock
            self.calendar = calendar
            self.voices = voices
            self.chime = chime
            self.notifier = notifier
            self.signals = signals
            self.purchases = purchases
            coordinator = CompanionCoordinator(
                settings: settings, bank: bank, language: voices.languageCode, clock: clock, idle: idle, busy: busy,
                calendar: calendar, seed: seed)
            performer = DeliveryPerformer(
                bank: bank, speaker: speaker, chime: chime, voices: voices, post: { [notifier] line in notifier.post(line: line) },
                schedule: schedule)
            windows = WindowManager(backgroundColor: configuration.paper) { [unowned self] id in self.windowContent(for: id) }
            windows.onClose = { [weak self] id in
                guard let self, id == self.roles.support else { return }
                self.purchases.acknowledgeTip()
            }
            performer.localize = configuration.localize
            performer.tuneBubbleText = configuration.tuneBubbleText
            performer.blipSound = configuration.sounds.blip
            performer.onBubble = { [weak self] text in self?.showBubble(text) }
            performer.onSpeechStarted = { [weak self] in self?.speechStarted() }
            performer.onSpeechFinished = { [weak self] in self?.speechFinished() }
            performer.onVoicingChanged = { [weak self] on in self?.voicingChanged(on) }
            signals.isSpeaking = { [unowned self] in self.performer.isSpeaking }
            signals.onEvent = { [weak self] trigger in self?.handleEvent(trigger) }
            signals.onCallStarted = { [weak self] in self?.stopSpeaking() }
            signals.onBusyPoll = { [weak self] reasons, changed in self?.busyPolled(reasons, changed: changed) }
            notifier.onAction = { [weak self] action in self?.handleNotificationAction(action) }
            refreshVoice()
            refreshStatus()
            observeSettings()
        }

        // MARK: For the app (open)

        /// Once at launch, before the status item exists: register fonts and the like.
        open func willLaunch() {}

        /// *Required.* A window's whole content, the app's window root included.
        open func windowContent(for id: Window) -> AnyView {
            fatalError("\(type(of: self)) must override windowContent(for:)")
        }

        /// *Required.* The menu bar item's panel, the app's window root included.
        open func panelContent() -> AnyView {
            fatalError("\(type(of: self)) must override panelContent()")
        }

        /// *Required.* The menus' words.
        open var menuCopy: MenuCopy {
            fatalError("\(type(of: self)) must override menuCopy")
        }

        /// The menu bar item's right-click menu; `CompanionMenus.menuBar` over `menuCopy` by default.
        open func menuBarMenu() -> NSMenu {
            CompanionMenus.menuBar(self, copy: menuCopy)
        }

        /// The desktop character's right-click menu; `CompanionMenus.desktop` over `menuCopy` by default.
        open func desktopMenu() -> NSMenu {
            CompanionMenus.desktop(self, copy: menuCopy)
        }

        /// *Required.* The character standing on the desktop (a `CharacterView` bound to the model).
        open func desktopCharacter() -> AnyView {
            fatalError("\(type(of: self)) must override desktopCharacter()")
        }

        /// *Required.* The desktop window's sizes and the bubble's place.
        open var desktopLayout: DesktopLayout {
            fatalError("\(type(of: self)) must override desktopLayout")
        }

        /// *Required.* The character the menu bar glyph draws (the worn skin, for an app with skins).
        open var glyphCharacter: RiggedCharacter {
            fatalError("\(type(of: self)) must override glyphCharacter")
        }

        /// The menu bar glyph's mood; `mood` by default.
        open var glyphMood: Mood { mood }

        /// The recorded voice the app prefers in its language; nil takes the library's default.
        open var preferredVoice: VoiceID? { nil }

        /// A phrase is about to be said (a delivery or a preview), before its first part reaches the bubble.
        open func willSay(_ phrase: Phrase) {}

        /// The bubble shows `text`: a line, or the part of a joke being told. Sets `bubble` and puts it up on the
        /// desktop; an app with more to show (a joke's setup above its punchline) sets its own state, then calls super.
        open func showBubble(_ text: String) {
            bubble = text
            showDesktopBubble(text)
        }

        /// The words in the bubble aren't the last of what's being said (a joke's setup shown without speech): the
        /// desktop bubble stays up without running down. False by default.
        open var desktopBubbleWaits: Bool { false }

        /// Audio started or stopped: sets `isVoicing` and redraws the glyph.
        open func voicingChanged(_ on: Bool) {
            guard isVoicing != on else { return }
            isVoicing = on
            refreshGlyph()
        }

        /// A notification button was pressed: its action id. Nothing by default.
        open func handleNotificationAction(_ id: String) {}

        /// The settings were just reset to their defaults, before the scheduler resumes.
        open func settingsDidReset() {}

        // MARK: Launch

        /// Once at launch (skipped under tests). Onboarding on the first run; otherwise Home, unless macOS started the
        /// app at login.
        public func launch(showHome: Bool) {
            guard !isLaunched else { return }
            isLaunched = true
            willLaunch()
            notifier.configure()
            applyAppearance()
            let statusItem = StatusItemController(
                panel: panelContent(), tooltip: configuration.statusItemTooltip, accessibilityLabel: configuration.accessibilityLabel,
                image: { [unowned self] animating, frame in
                    GlyphRenderer.image(
                        character: self.glyphCharacter, mood: animating ? .talking : self.glyphMood, frame: frame,
                        accessibilityDescription: self.configuration.accessibilityLabel)
                })
            statusItem.menu = { [unowned self] in self.menuBarMenu() }
            self.statusItem = statusItem
            refreshNotificationPermission()
            Task { await purchases.start() }
            timer = Timer.scheduledTimer(withTimeInterval: 20, repeats: true) { [weak self] _ in
                MainActor.assumeIsolated { self?.tick() }
            }
            refreshStatus()
            if !settings.hasCompletedOnboarding {
                windows.show(roles.onboarding)
            } else if showHome {
                windows.show(roles.home)
            }
        }

        /// The user opened the app again while it was running (Finder, Launchpad, Spotlight).
        public func reopen() {
            if windows.showOpenWindows() { return }
            windows.show(settings.hasCompletedOnboarding ? roles.home : roles.onboarding)
        }

        // MARK: Ticks and deliveries

        /// Every 20 s: the scheduler decides, the rules judge, and a line is delivered or not.
        public func tick() {
            let snapshot = signals.snapshot()
            if let delivery = coordinator.tick(snapshot: snapshot) { perform(delivery) }
            refreshStatus(snapshot: snapshot)
        }

        private func handleEvent(_ trigger: Trigger) {
            if let delivery = coordinator.event(trigger, snapshot: signals.snapshot()) { perform(delivery) }
            refreshStatus()
        }

        private func perform(_ delivery: Delivery) {
            bubblePhraseID = delivery.phrase.id
            willSay(delivery.phrase)
            performer.perform(delivery, settings: settings, voice: voice)
        }

        /// The recorded voice in use, nil where the language has none (the system voice speaks).
        public var voice: VoiceID? { voices.voice(preferred: preferredVoice) }

        /// Lines with no current recording in the voice in use are kept out of the picks (none without a recorded
        /// voice). Call it when the app's voice preference changes.
        public func refreshVoice() {
            coordinator.unavailableIDs = performer.unavailableIDs(voice: voice)
            if VoiceLibrary.isDebugBuild, let voice, !coordinator.unavailableIDs.isEmpty {
                print("voice: \(voices.languageCode)/\(voice.rawValue) pending \(coordinator.unavailableIDs.sorted())")
            }
        }

        /// The desktop bubble stays up while the line is said, then lingers.
        private func speechStarted() {
            bubbleTimer?.invalidate()
            desktopCountdown = nil
            isSpeaking = true
            signals.speechStarted()
            refreshStatus()
        }

        private func speechFinished() {
            guard isSpeaking else { return }
            isSpeaking = false
            isVoicing = false
            signals.speechEnded()
            lingerDesktopBubble()
            refreshStatus()
        }

        /// Cuts the current line or joke, the parts still waiting included (a call started, Shh).
        open func stopSpeaking() {
            performer.stop()
            speechFinished()
        }

        // MARK: Actions

        /// Say something: the panel button, a click on the desktop character, the notification's button. Mid-line (a
        /// notification's Say more comes while its line is still heard) it cuts that line, as Shh does, and says the new one.
        public func sayNow() {
            if let delivery = coordinator.poke(snapshot: signals.snapshot()) {
                if performer.isSpeaking { stopSpeaking() }
                perform(delivery)
            }
            refreshStatus()
        }

        public func shush(_ shush: ChatterCadence.Shush) {
            stopSpeaking()
            chime.stop()
            coordinator.shush(shush)
            playChime(configuration.sounds.shh)
            refreshStatus()
        }

        /// Pressing and holding the character in the panel (or VoiceOver's "Shh for an hour"): the quick hour of quiet.
        /// Does nothing while shushed, where Wake up stays the way out. Returns whether it shushed, for the haptic.
        @discardableResult
        public func holdToShush() -> Bool {
            guard !isShushed else { return false }
            shush(.hour)
            return true
        }

        /// Ends Shh early; the character says one line about it.
        public func wake() {
            if let delivery = coordinator.wake(snapshot: signals.snapshot()) { perform(delivery) }
            refreshStatus()
        }

        /// "Don't say that again" on the speech bubble: the line leaves the picks for good (Settings can bring muted
        /// lines back), with an Undo for a few seconds.
        public func muteBubbleLine() {
            guard let id = bubblePhraseID else { return }
            settings.mutedPhraseIDs.insert(id)
            justMuted = id
            undoTimer?.invalidate()
            undoTimer = Timer.scheduledTimer(withTimeInterval: 8, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated { self?.justMuted = nil }
            }
        }

        public func undoMute() {
            guard let id = justMuted else { return }
            settings.mutedPhraseIDs.remove(id)
            justMuted = nil
            undoTimer?.invalidate()
        }

        /// Onboarding's channel tiles: turning one on previews it once, the chime's blip or `line` said aloud. Only on
        /// that tap, and never while busy (a call, full screen), with the output muted or mid-line.
        public func previewChannel(_ channel: Channels, line: Phrase?) {
            if channel == .chime {
                guard canPreview else { return }
                playChime(configuration.sounds.blip, always: true)
            } else if channel == .voice, let line {
                preview(line)
            }
        }

        /// Says `line` once on the user's tap (onboarding's samples), under the same guards as a channel preview.
        public func preview(_ line: Phrase) {
            guard canPreview, !line.localizedParts().isEmpty else { return }
            say(line)
        }

        /// Previews may play: nothing busy, the output not muted, nothing being said.
        public var canPreview: Bool {
            let snapshot = signals.snapshot()
            return snapshot.busy.isEmpty && snapshot.isOutputMuted != true && !isSpeaking
        }

        /// Says `phrase` now, the way Settings plays a sample (Test voice): spoken, without a chime or a notification;
        /// the bubble follows it but it can't be muted. No guards: only on the user's tap.
        public func say(_ phrase: Phrase) {
            performer.stop()
            bubblePhraseID = nil
            willSay(phrase)
            performer.preview(phrase, settings: settings, voice: voice)
        }

        /// Home appeared: the menu bar glyph waves back, with the hello sound when the chime channel is on.
        public func waveFromHome() {
            statusItem?.wave()
            playChime(configuration.sounds.hello)
        }

        public func openHome() { windows.show(roles.home) }
        public func openSettings() { windows.show(roles.settings) }
        public func openSupport() { windows.show(roles.support) }
        public func openOnboarding() { windows.show(roles.onboarding) }
        public func closeWindow(_ id: Window) { windows.close(id) }

        public func quit() {
            NSApp.terminate(nil)
        }

        /// The supporter product is owned (`CompanionAppConfiguration.supporterProduct`).
        public var isSupporter: Bool {
            configuration.supporterProduct.map(purchases.isOwned) ?? false
        }

        /// Plays `sound` when the chime channel is on (or `always`, for a preview the user asked for).
        private func playChime(_ sound: String?, always: Bool = false) {
            guard let sound, always || settings.channels.contains(.chime) else { return }
            chime.play(sound, volume: settings.speechVolume, completion: nil)
        }

        // MARK: Onboarding and settings

        /// Get started: onboarding is done, the notification permission is asked for only if that channel was picked,
        /// and Home takes over.
        public func completeOnboarding() {
            settings.hasCompletedOnboarding = true
            if settings.channels.contains(.notification) { requestNotificationPermission() }
            guard isLaunched else { return }
            windows.close(roles.onboarding)
            windows.show(roles.home)
        }

        /// Turns a channel on or off. Turning the notification channel on is the one moment the app asks macOS for
        /// permission (onboarding writes `settings.channels` directly and asks on Get started).
        public func setChannel(_ channel: Channels, on: Bool) {
            if on {
                settings.channels.insert(channel)
                if channel == .notification { requestNotificationPermission() }
            } else {
                settings.channels.remove(channel)
            }
        }

        public func requestNotificationPermission() {
            notifier.requestAuthorization { [weak self] granted in
                self?.notificationPermission = granted ? .authorized : .denied
                self?.signals.notificationsAuthorized = granted
            }
        }

        public func refreshNotificationPermission() {
            notifier.refreshAuthorization { [weak self] permission in
                self?.notificationPermission = permission
                self?.signals.notificationsAuthorized = permission == .authorized
            }
        }

        /// System Settings → Notifications on the app's page, for when permission was denied.
        public func openNotificationSettings() {
            NotificationPoster.openSystemSettings(bundleIdentifier: Bundle.main.bundleIdentifier ?? "")
        }

        /// Every preference back to its default (onboarding stays done) and Shh over, without a line.
        public func resetSettings() {
            stopSpeaking()
            presence.showNow()
            settings.resetToDefaults()
            settingsDidReset()
            if coordinator.scheduler.pausedUntil != nil { coordinator.scheduler.resume() }
            coordinator.settingsChanged()
            refreshStatus()
        }

        /// Watches the settings: timing changes reach the scheduler after 300 ms of quiet; appearance and the
        /// desktop apply right away.
        private func observeSettings() {
            observe {
                _ = self.settings.frequency
                _ = self.settings.activeStartMinute
                _ = self.settings.activeEndMinute
                _ = self.settings.activeWeekdays
                _ = self.settings.quietHours
                _ = self.settings.hushInFullScreen
            } then: {
                self.timingSettingsChanged()
            }
            observe {
                _ = self.settings.appearance
            } then: {
                self.applyAppearance()
            }
            observe {
                _ = self.settings.showOnDesktop
                _ = self.settings.desktopSize
                _ = self.settings.desktopPosition
            } then: {
                self.refreshStatus()
            }
        }

        /// Calls `react` whenever something `read` reads changes, for good. Observation fires once per change, so the
        /// watcher re-arms itself. For the app's own preferences.
        public func observe(_ read: @escaping @MainActor () -> Void, then react: @escaping @MainActor () -> Void) {
            withObservationTracking {
                read()
            } onChange: { [weak self] in
                Task { @MainActor in
                    guard let self else { return }
                    react()
                    self.observe(read, then: react)
                }
            }
        }

        private func timingSettingsChanged() {
            settingsDebounce?.invalidate()
            settingsDebounce = Timer.scheduledTimer(withTimeInterval: 0.3, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated {
                    guard let self else { return }
                    self.coordinator.settingsChanged()
                    self.refreshStatus()
                }
            }
        }

        private func applyAppearance() {
            guard isLaunched else { return }
            switch settings.appearance {
            case .system: NSApp.appearance = nil
            case .light: NSApp.appearance = NSAppearance(named: .aqua)
            case .dark: NSApp.appearance = NSAppearance(named: .darkAqua)
            }
        }

        // MARK: Status

        public func refreshStatus(snapshot: Snapshot? = nil) {
            let snapshot = snapshot ?? signals.snapshot()
            isShushed = coordinator.isShushed
            mood = coordinator.mood(snapshot: snapshot)
            statusLine = Self.statusLine(
                status: coordinator.scheduler.status, snapshot: snapshot, now: clock.now, calendar: calendar,
                endOfToday: coordinator.scheduler.endOfToday, copy: configuration.status)
            refreshGlyph()
            refreshDesktop(snapshot: snapshot)
        }

        /// Redraws the menu bar glyph: `glyphCharacter` in `glyphMood`, stepping the talking frames while words are
        /// heard. Call it when the character changes (a skin).
        public func refreshGlyph() {
            guard let statusItem else { return }
            statusItem.show(
                glyph: glyphCharacter, mood: glyphMood, talking: isVoicing, accessibilityDescription: configuration.accessibilityLabel)
        }

        /// The status pill's words, from the scheduler's status and the moment.
        public static func statusLine(
            status: SchedulerStatus, snapshot: Snapshot, now: Date, calendar: Calendar, endOfToday: Date, copy: StatusCopy
        ) -> String {
            switch status {
            case .paused(let until):
                if until == .distantFuture { return copy.shhUntilYouSay() }
                if until >= endOfToday && until <= endOfToday.addingTimeInterval(60) { return copy.shhRestOfToday() }
                if calendar.isDate(until, inSameDayAs: now) {
                    return copy.shhUntil(until.formatted(date: .omitted, time: .shortened))
                }
                return copy.shhUntilDay(until.formatted(.dateTime.weekday().hour().minute()))
            case .postponed(.call):
                return copy.quietCall()
            case .postponed(.fullScreen):
                return copy.quietFullScreen()
            case .outsideActiveHours:
                return copy.offHours()
            case .away:
                return copy.asleep()
            case .scheduled(let date):
                if snapshot.busy.contains(.call) { return copy.quietCall() }
                if snapshot.busy.contains(.fullScreen) { return copy.quietFullScreen() }
                if snapshot.isScreenLocked || snapshot.idleSeconds >= QuietRules.awayThreshold { return copy.asleep() }
                let minutes = max(1, Int((date.timeIntervalSince(now) / 60).rounded(.up)))
                return copy.nextIn(Duration.seconds(minutes * 60).formatted(.units(allowed: [.hours, .minutes], width: .abbreviated)))
            }
        }

        // MARK: Desktop

        public func setShowOnDesktop(_ on: Bool) {
            if on { presence.showNow() }
            settings.showOnDesktop = on
            refreshStatus()
        }

        public func hideDesktop(_ span: DesktopHide) {
            switch span {
            case .hour: presence.hide(until: clock.now.addingTimeInterval(DesktopPresence.hourHide))
            case .restOfToday: presence.hide(until: coordinator.scheduler.endOfToday)
            case .untilShown:
                presence.showNow()
                settings.showOnDesktop = false
            }
            refreshStatus()
        }

        /// Ends a hide for a while (and turns the desktop character on if it was off).
        public func showDesktopNow() {
            presence.showNow()
            settings.showOnDesktop = true
            refreshStatus()
        }

        /// A pinch on the character: one size bigger or smaller.
        public func stepDesktopSize(_ step: Int) {
            settings.desktopSize = settings.desktopSize.stepped(by: step)
        }

        public func moveDesktop(to position: DesktopPosition) {
            settings.desktopPosition = position
        }

        /// Back to the bottom right of the main screen.
        public func resetDesktopPosition() {
            settings.desktopPosition = nil
        }

        public func dismissDesktopBubble() {
            bubbleTimer?.invalidate()
            bubbleTimer = nil
            desktopBubble = nil
            desktopCountdown = nil
            desktop?.hideBubble(animated: !reduceMotion)
        }

        /// The panel row's words while the character is off the desktop for a while.
        public var desktopBackLine: String? {
            guard settings.showOnDesktop, let until = desktopHiddenUntil else { return nil }
            if until >= coordinator.scheduler.endOfToday { return configuration.desktop.backTomorrow() }
            return configuration.desktop.backAt(until.formatted(date: .omitted, time: .shortened))
        }

        /// Words were delivered: up in the desktop bubble, lingering once they're said (or right away when nothing is
        /// spoken).
        private func showDesktopBubble(_ text: String) {
            desktopBubble = text
            lingerDesktopBubble()
            updateDesktopWindow()
        }

        private func lingerDesktopBubble() {
            bubbleTimer?.invalidate()
            bubbleTimer = nil
            desktopCountdown = nil
            guard let text = desktopBubble, !isSpeaking, !desktopBubbleWaits else { return }
            let seconds = DesktopPresence.bubbleSeconds(for: text)
            desktopCountdown = BubbleCountdown(start: Date(), seconds: seconds)
            desktop?.setCountdown(desktopCountdown)
            bubbleTimer = Timer.scheduledTimer(withTimeInterval: seconds, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated { self?.dismissDesktopBubble() }
            }
        }

        /// Every poll of the busy watch: a change refreshes everything; while busy, the presence learns the moment, so
        /// the character comes back 10 s after the call really ended.
        private func busyPolled(_ reasons: Set<BusyReason>, changed: Bool) {
            if changed {
                refreshStatus()
            } else if !reasons.isEmpty {
                presence.update(enabled: settings.showOnDesktop, snapshot: Snapshot(busy: reasons), now: clock.now)
            }
        }

        private func refreshDesktop(snapshot: Snapshot) {
            let shown = presence.update(enabled: settings.showOnDesktop, snapshot: snapshot, now: clock.now)
            if isOnDesktop != shown { isOnDesktop = shown }
            if desktopHiddenUntil != presence.hiddenUntil { desktopHiddenUntil = presence.hiddenUntil }
            if !shown, desktopBubble != nil {
                bubbleTimer?.invalidate()
                bubbleTimer = nil
                desktopBubble = nil
                desktopCountdown = nil
            }
            signals.watchForDesktop(isLaunched && settings.showOnDesktop && presence.hiddenUntil == nil)
            scheduleDesktopRecheck()
            updateDesktopWindow()
        }

        /// Refreshes again when a hide for a while ends or the reappear delay after a call runs out, instead of waiting
        /// for the next tick.
        private func scheduleDesktopRecheck() {
            desktopRecheck?.invalidate()
            desktopRecheck = nil
            guard isLaunched, settings.showOnDesktop else { return }
            let now = clock.now
            let moments = [presence.hiddenUntil, presence.lastBusy?.addingTimeInterval(DesktopPresence.reappearDelay)]
            guard let next = moments.compactMap({ $0 }).filter({ $0 > now }).min() else { return }
            desktopRecheck = Timer.scheduledTimer(withTimeInterval: next.timeIntervalSince(now) + 0.1, repeats: false) { [weak self] _ in
                MainActor.assumeIsolated { self?.refreshStatus() }
            }
        }

        private var reduceMotion: Bool { NSWorkspace.shared.accessibilityDisplayShouldReduceMotion }

        /// Makes the desktop window the first time it's needed, then matches it to the state.
        private func updateDesktopWindow() {
            guard isLaunched else { return }
            if desktop == nil {
                guard settings.showOnDesktop else { return }
                desktop = makeDesktop()
            }
            guard let desktop else { return }
            desktop.setSize(settings.desktopSize)
            desktop.setPosition(settings.desktopPosition)
            let animated = !reduceMotion
            if isOnDesktop {
                desktop.show(animated: animated)
                if let desktopBubble {
                    desktop.showBubble(desktopBubble, countdown: desktopCountdown, animated: animated)
                } else {
                    desktop.hideBubble(animated: animated)
                }
            } else {
                desktop.hide(animated: animated)
            }
        }

        private func makeDesktop() -> DesktopCompanion {
            let desktop = DesktopCompanion(
                character: desktopCharacter(), layout: desktopLayout, size: settings.desktopSize, position: settings.desktopPosition,
                sayActionName: configuration.desktop.sayActionName(), palette: configuration.palette,
                displayFont: configuration.displayFont)
            desktop.onSay = { [weak self] in self?.sayNow() }
            desktop.onMoved = { [weak self] position in self?.moveDesktop(to: position) }
            desktop.onPinch = { [weak self] step in self?.stepDesktopSize(step) }
            desktop.onBubbleDismissed = { [weak self] in self?.dismissDesktopBubble() }
            desktop.menu = { [unowned self] in self.desktopMenu() }
            return desktop
        }
    }
#endif
