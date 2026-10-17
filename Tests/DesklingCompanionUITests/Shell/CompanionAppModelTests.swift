#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingCompanionUI
    import DesklingCore
    import DesklingShell
    import DesklingStore
    import DesklingTesting
    import Observation
    import SwiftUI
    import XCTest

    /// The fixture app's windows.
    enum FixtureWindow: String, WindowID {
        case home, onboarding, settings, support

        var title: String { rawValue }
        var size: CGSize { CGSize(width: 400, height: 400) }
    }

    /// An app's model in another module: `@Observable` over the base, with a property of its own and the hooks an app
    /// overrides.
    @MainActor
    @Observable
    final class FixtureModel: CompanionAppModel<FixtureWindow, CompanionSettings> {
        /// Every phrase about to be said, in order (the app's own observable state).
        private(set) var said: [String] = []

        override func willSay(_ phrase: Phrase) {
            said.append(phrase.id)
        }

        override func handleNotificationAction(_ id: String) {
            switch id {
            case "SAY_MORE": sayNow()
            case "SHH_HOUR": shush(.hour)
            default: break
            }
        }

        override func windowContent(for id: FixtureWindow) -> AnyView { AnyView(Text(verbatim: id.title)) }
        override func panelContent() -> AnyView { AnyView(Text(verbatim: "panel")) }
        override func desktopCharacter() -> AnyView { AnyView(Color.green) }
        override var desktopLayout: DesktopLayout { DesktopLayout(art: FixtureArt.vector) }
        override var glyphCharacter: RiggedCharacter { SkinCatalog.fixture.character(for: FixtureIDs.vectorSkin) }

        override var menuCopy: MenuCopy {
            MenuCopy(
                say: "Say something", wakeUp: "Wake up", shh: "Shh", forAnHour: "For an hour", forTheRestOfToday: "For the rest of today",
                untilISay: "Until I say", desktopSubmenu: "On the desktop", show: "Show", showNow: "Show now",
                hideForAnHour: "Hide for an hour", hideForTheRestOfToday: "Hide for the rest of today", size: "Size", hide: "Hide",
                untilIShowItAgain: "Until I show it again", settings: "Settings…", support: "Support…",
                thanksForSupporting: "Thanks for supporting ♥…", quit: "Quit", sizeLabel: { "\($0)" })
        }

        static let configuration = CompanionAppConfiguration(
            statusItemTooltip: "Fixture", accessibilityLabel: "Fixture", paper: .white, palette: .system,
            displayFont: { .system(size: $0) },
            sounds: CompanionSounds(blip: "blip", shh: "shh", hello: "hello"),
            status: StatusCopy(
                shhUntilYouSay: { "Shh until you say" }, shhRestOfToday: { "Shh for the rest of today" }, shhUntil: { "Shh until \($0)" },
                shhUntilDay: { "Shh until \($0)" }, quietCall: { "Quiet: you're on a call" }, quietFullScreen: { "Quiet: full screen" },
                offHours: { "Off hours" }, asleep: { "Asleep" }, nextIn: { "Next in about \($0)" }),
            desktop: DesktopCopy(sayActionName: { "Say something" }, backTomorrow: { "Back tomorrow" }, backAt: { "Back at \($0)" }),
            tuneBubbleText: { "♪" }, supporterProduct: supporter)

        static let supporter = "fixture.supporter"
    }

    @MainActor
    final class CompanionAppModelTests: XCTestCase {
        private let calendar: Calendar = {
            var calendar = Calendar(identifier: .gregorian)
            calendar.timeZone = TimeZone(identifier: "UTC")!
            return calendar
        }()
        private var clock: MockClock!
        private var idle: MockIdle!
        private var busy: MockBusy!
        private var settings: CompanionSettings!
        private var speaker: FakeSpeaker!
        private var chime: FakeChime!
        private var notifier: FakeNotifier!
        /// The beats waiting between a joke's parts.
        private var beats: [@MainActor @Sendable () -> Void] = []

        private static let catalog = ProductCatalog(unlockables: [FixtureModel.supporter], tips: ["fixture.tip"])

        /// The fixture's single lines: no tunes, no jokes in beats, so every delivery is one spoken line.
        private static let lines = PhraseBank(
            PhraseBank.fixture.phrases.filter {
                if case .line = $0.kind { return true }
                return false
            })

        /// Monday 2026-09-28 at the given UTC time: inside the default active hours.
        private func monday(_ hour: Int, _ minute: Int = 0) -> Date {
            calendar.date(from: DateComponents(year: 2026, month: 9, day: 28, hour: hour, minute: minute))!
        }

        private func makeModel(bank: PhraseBank = lines, configure: (CompanionSettings) -> Void = { _ in }) -> FixtureModel {
            clock = MockClock(monday(10))
            idle = MockIdle()
            busy = MockBusy()
            settings = CompanionSettings(store: InMemorySettingsStore(), defaults: .fixture)
            settings.hasCompletedOnboarding = true
            configure(settings)
            speaker = FakeSpeaker()
            chime = FakeChime()
            notifier = FakeNotifier()
            beats = []
            let signals = SignalHub(idle: idle, busy: busy, audio: nil, screenLock: nil, power: nil, appearance: nil)
            let defaults = UserDefaults(suiteName: "deskling.tests.\(UUID().uuidString)")!
            let purchases = PurchaseStore(catalog: Self.catalog, backend: MockStoreBackend(catalog: Self.catalog), defaults: defaults)
            return FixtureModel(
                settings: settings, bank: bank,
                roles: WindowRoles(home: .home, onboarding: .onboarding, settings: .settings, support: .support),
                configuration: FixtureModel.configuration, clock: clock, idle: idle, busy: busy, calendar: calendar, speaker: speaker,
                chime: chime, notifier: notifier, signals: signals, purchases: purchases,
                voices: VoiceLibrary(folder: nil, language: "en", allowTesting: true), seed: 42,
                schedule: { [unowned self] _, work in beats.append(work) })
        }

        /// Ticks once a minute for `minutes`.
        private func run(_ model: FixtureModel, minutes: Int) {
            for _ in 0..<minutes {
                clock.advance(minutes: 1)
                model.tick()
            }
        }

        private func text(_ id: String) -> String {
            PhraseBank.fixture[id]!.localizedText!
        }

        func testPokeSpeaksWhenVoiceIsOn() {
            let model = makeModel()
            model.sayNow()
            XCTAssertEqual(speaker.spoken.count, 1)
            XCTAssertEqual(model.bubble, speaker.spoken.first)
            XCTAssertEqual(model.said, ["button-press-1"], "the app heard what is about to be said")
            XCTAssertTrue(model.isSpeaking)
            XCTAssertTrue(model.isVoicing)
            XCTAssertEqual(model.mood, .talking)
            speaker.finish()
            XCTAssertFalse(model.isSpeaking)
            XCTAssertFalse(model.isVoicing)
            XCTAssertEqual(model.mood, .awake)
            XCTAssertTrue(chime.played.isEmpty, "the chime channel is off by default")
        }

        func testPokeOnlyShowsTheBubbleWhenEveryChannelIsOff() {
            let model = makeModel { $0.channels = [] }
            model.sayNow()
            XCTAssertNotNil(model.bubble)
            XCTAssertTrue(speaker.spoken.isEmpty)
            XCTAssertTrue(notifier.posted.isEmpty)
        }

        func testBlipPrecedesTheLineWhenTheChimeIsOn() {
            let model = makeModel { $0.channels = [.voice, .chime] }
            model.sayNow()
            XCTAssertEqual(chime.played, ["blip"])
            XCTAssertEqual(speaker.spoken.count, 1)
        }

        func testATickOnACallDeliversNothing() {
            let model = makeModel()
            busy.reasons = [.call]
            run(model, minutes: 75)
            XCTAssertTrue(speaker.spoken.isEmpty)
            XCTAssertNil(model.bubble)
            XCTAssertEqual(model.mood, .hushedForCall)
            XCTAssertEqual(model.statusLine, "Quiet: you're on a call")
        }

        func testAmbientLinesArriveWhenCalm() {
            let model = makeModel()
            run(model, minutes: 75)
            XCTAssertFalse(speaker.spoken.isEmpty)
            XCTAssertTrue(model.statusLine.hasPrefix("Next in about"), model.statusLine)
        }

        func testTurningOnTheNotificationChannelAsksForPermissionOnce() {
            let model = makeModel()
            XCTAssertEqual(notifier.requests, 0)
            model.setChannel(.chime, on: true)
            XCTAssertEqual(notifier.requests, 0)
            model.setChannel(.notification, on: true)
            XCTAssertEqual(notifier.requests, 1)
            XCTAssertTrue(settings.channels.contains(.notification))
            XCTAssertEqual(model.notificationPermission, .authorized)
            run(model, minutes: 75)
            XCTAssertEqual(notifier.requests, 1, "ticks and deliveries never ask again")
            XCTAssertFalse(notifier.posted.isEmpty, "the line went out as a notification")
        }

        func testOnboardingAsksOnlyWhenNotificationsWerePicked() {
            let quiet = makeModel { $0.hasCompletedOnboarding = false }
            quiet.completeOnboarding()
            XCTAssertEqual(notifier.requests, 0)
            XCTAssertTrue(settings.hasCompletedOnboarding)
            let picked = makeModel {
                $0.hasCompletedOnboarding = false
                $0.channels = [.notification]
            }
            picked.completeOnboarding()
            XCTAssertEqual(notifier.requests, 1)
        }

        func testShhForAnHourSetsTheStatusLineAndNoSpeech() {
            let model = makeModel { $0.channels = [.voice, .chime] }
            model.shush(.hour)
            XCTAssertTrue(model.isShushed)
            XCTAssertEqual(model.mood, .shushed)
            XCTAssertEqual(chime.played, ["shh"])
            XCTAssertTrue(model.statusLine.hasPrefix("Shh until"), model.statusLine)
            XCTAssertEqual(settings.shushedUntil, monday(11))
            run(model, minutes: 59)
            XCTAssertTrue(speaker.spoken.isEmpty)
        }

        func testHoldingShushesForAnHourOnlyWhenAwake() {
            let model = makeModel()
            XCTAssertTrue(model.holdToShush())
            XCTAssertTrue(model.isShushed)
            XCTAssertEqual(model.mood, .shushed)
            XCTAssertEqual(settings.shushedUntil, monday(11))
            clock.advance(minutes: 10)
            XCTAssertFalse(model.holdToShush(), "holding while shushed does nothing")
            XCTAssertEqual(settings.shushedUntil, monday(11))

            let indefinite = makeModel()
            indefinite.shush(.untilISay)
            XCTAssertFalse(indefinite.holdToShush(), "holding never shortens a longer Shh")
            XCTAssertEqual(indefinite.statusLine, "Shh until you say")
        }

        func testShhRestOfTodayAndUntilISayHaveTheirOwnStatusLines() {
            let today = makeModel()
            today.shush(.restOfToday)
            XCTAssertEqual(today.statusLine, "Shh for the rest of today")
            let forever = makeModel()
            forever.shush(.untilISay)
            XCTAssertEqual(forever.statusLine, "Shh until you say")
        }

        func testWakeSpeaksTheShushEndLine() {
            let model = makeModel()
            model.shush(.untilISay)
            run(model, minutes: 120)
            XCTAssertTrue(speaker.spoken.isEmpty)
            model.wake()
            XCTAssertFalse(model.isShushed)
            XCTAssertEqual(speaker.spoken, [text("shush-end-1")])
            XCTAssertTrue(model.statusLine.hasPrefix("Next in about"), model.statusLine)
        }

        func testACallStartingMidLineStopsSpeech() {
            let model = makeModel()
            model.sayNow()
            XCTAssertTrue(model.isSpeaking)
            busy.reasons = [.call]
            model.signals.pollBusy()
            XCTAssertEqual(speaker.stops, 1)
            XCTAssertFalse(model.isSpeaking)
            XCTAssertFalse(model.isVoicing)
        }

        func testShhCutsTheCurrentLine() {
            let model = makeModel()
            model.sayNow()
            model.shush(.hour)
            XCTAssertEqual(speaker.stops, 1)
            XCTAssertFalse(model.isSpeaking)
            XCTAssertEqual(model.mood, .shushed)
        }

        func testSayMoreMidLineCutsTheLineAndSaysAnother() {
            let model = makeModel()
            model.sayNow()
            XCTAssertTrue(model.isSpeaking)
            // A notification's Say more, pressed while its line is still heard.
            notifier.onAction?("SAY_MORE")
            XCTAssertEqual(speaker.stops, 1, "the line being said is cut")
            XCTAssertEqual(speaker.spoken.count, 2, "and the press is answered at once, not dropped")
            XCTAssertEqual(model.said.count, 2)
            XCTAssertEqual(model.bubble, speaker.spoken.last)
            XCTAssertTrue(model.isSpeaking)
            speaker.finish()
            XCTAssertFalse(model.isSpeaking)
        }

        func testResetSettingsEndsShhWithoutALine() {
            let model = makeModel { $0.frequency = .chatty }
            model.shush(.untilISay)
            model.resetSettings()
            XCTAssertFalse(model.isShushed)
            XCTAssertEqual(settings.frequency, .rarely)
            XCTAssertTrue(speaker.spoken.isEmpty)
        }

        func testTurningOnAChannelTilePreviewsItOnce() {
            let model = makeModel()
            let hello = PhraseBank.fixture["general-a1"]!
            model.previewChannel(.chime, line: hello)
            XCTAssertEqual(chime.played, ["blip"])
            model.previewChannel(.voice, line: hello)
            XCTAssertEqual(speaker.spoken, [text("general-a1")])
            XCTAssertEqual(speaker.clips, [nil], "no recorded voice: the system voice speaks")
            XCTAssertNil(model.bubblePhraseID, "a preview can't be muted")
            model.previewChannel(.notification, line: hello)
            XCTAssertEqual(chime.played, ["blip"], "a notification has nothing to hear")
            XCTAssertEqual(speaker.spoken, [text("general-a1")])
        }

        func testChannelPreviewsKeepTheirManners() {
            let model = makeModel()
            let hello = PhraseBank.fixture["general-a1"]!
            busy.reasons = [.call]
            model.previewChannel(.chime, line: hello)
            model.previewChannel(.voice, line: hello)
            XCTAssertTrue(chime.played.isEmpty, "never during a call")
            XCTAssertTrue(speaker.spoken.isEmpty, "never during a call")
            busy.reasons = []
            model.previewChannel(.voice, line: hello)
            model.previewChannel(.voice, line: PhraseBank.fixture["general-b1"]!)
            XCTAssertEqual(speaker.spoken, [text("general-a1")], "never over a line already playing")
        }

        func testDontSayThatAgainMutesTheBubbleLineWithUndo() {
            let model = makeModel()
            model.sayNow()
            let id = try! XCTUnwrap(model.bubblePhraseID)
            model.muteBubbleLine()
            XCTAssertEqual(settings.mutedPhraseIDs, [id])
            XCTAssertEqual(model.justMuted, id)
            model.undoMute()
            XCTAssertEqual(settings.mutedPhraseIDs, [])
            XCTAssertNil(model.justMuted)
        }

        func testNotificationActionsRouteBack() {
            let model = makeModel()
            notifier.onAction?("SAY_MORE")
            XCTAssertEqual(speaker.spoken.count, 1)
            notifier.onAction?("SHH_HOUR")
            XCTAssertTrue(model.isShushed)
        }

        func testVoicingIsOffInTheBeatBetweenASetupAndItsPunchline() {
            let model = makeModel(bank: PhraseBank([PhraseBank.fixture["joke-1"]!]))
            var voicing: [Bool] = []
            model.observe {
                _ = model.isVoicing
            } then: {
                voicing.append(model.isVoicing)
            }
            model.sayNow()
            XCTAssertEqual(speaker.spoken, ["Fixture joke, setup"])
            XCTAssertTrue(model.isVoicing)
            speaker.finish()
            XCTAssertTrue(model.isSpeaking, "the beat is part of the joke")
            XCTAssertFalse(model.isVoicing, "the mouth rests in the beat")
            XCTAssertEqual(model.bubble, "Fixture joke, setup")
            beats.removeFirst()()
            XCTAssertEqual(speaker.spoken, ["Fixture joke, setup", "Fixture joke, punchline"])
            XCTAssertTrue(model.isVoicing)
            XCTAssertEqual(model.bubble, "Fixture joke, punchline")
            speaker.finish()
            XCTAssertFalse(model.isSpeaking)
            XCTAssertFalse(model.isVoicing)
        }

        // MARK: Desktop

        func testTheDesktopCharacterIsOffUntilTurnedOn() {
            let model = makeModel()
            model.refreshStatus()
            XCTAssertFalse(model.isOnDesktop)
            model.setShowOnDesktop(true)
            XCTAssertTrue(model.isOnDesktop)
            XCTAssertTrue(settings.showOnDesktop)
            model.setShowOnDesktop(false)
            XCTAssertFalse(model.isOnDesktop)
        }

        func testACallTakesTheCharacterOffTheDesktopUntilTenSecondsAfterIt() {
            let model = makeModel { $0.showOnDesktop = true }
            model.refreshStatus()
            XCTAssertTrue(model.isOnDesktop)
            busy.reasons = [.call]
            model.signals.pollBusy()
            XCTAssertFalse(model.isOnDesktop)
            // Still on the call a minute later: the presence keeps learning the moment from each poll.
            clock.advance(minutes: 1)
            model.signals.pollBusy()
            busy.reasons = []
            clock.advance(minutes: 5.0 / 60)
            model.signals.pollBusy()
            XCTAssertFalse(model.isOnDesktop)
            clock.advance(minutes: 5.0 / 60)
            model.refreshStatus()
            XCTAssertTrue(model.isOnDesktop)
        }

        func testADeliveredLineFillsTheDesktopBubble() {
            let model = makeModel {
                $0.showOnDesktop = true
                $0.channels = []
            }
            model.sayNow()
            XCTAssertTrue(model.isOnDesktop)
            XCTAssertNotNil(model.desktopBubble)
            XCTAssertEqual(model.desktopBubble, model.bubble)
            XCTAssertNotNil(model.desktopCountdown, "nothing spoken: the bubble runs down at once")
            model.dismissDesktopBubble()
            XCTAssertNil(model.desktopBubble)
            XCTAssertNotNil(model.bubble, "the panel keeps its line")
        }

        func testACallClearsTheDesktopBubble() {
            let model = makeModel {
                $0.showOnDesktop = true
                $0.channels = []
            }
            model.sayNow()
            busy.reasons = [.call]
            model.refreshStatus()
            XCTAssertNil(model.desktopBubble)
        }

        func testHideForAnHourComesBackAnHourLater() {
            let model = makeModel { $0.showOnDesktop = true }
            model.hideDesktop(.hour)
            XCTAssertFalse(model.isOnDesktop)
            XCTAssertTrue(settings.showOnDesktop)
            XCTAssertEqual(model.desktopHiddenUntil, monday(11))
            XCTAssertTrue(model.desktopBackLine?.hasPrefix("Back at") ?? false)
            clock.advance(minutes: 59)
            model.refreshStatus()
            XCTAssertFalse(model.isOnDesktop)
            clock.advance(minutes: 1)
            model.refreshStatus()
            XCTAssertTrue(model.isOnDesktop)
            XCTAssertNil(model.desktopHiddenUntil)
            XCTAssertNil(model.desktopBackLine)
        }

        func testShowNowEndsAHideForTheRestOfToday() {
            let model = makeModel { $0.showOnDesktop = true }
            model.hideDesktop(.restOfToday)
            XCTAssertFalse(model.isOnDesktop)
            XCTAssertEqual(model.desktopBackLine, "Back tomorrow")
            model.showDesktopNow()
            XCTAssertTrue(model.isOnDesktop)
            XCTAssertNil(model.desktopBackLine)
        }

        func testHideUntilShownTurnsTheSettingOff() {
            let model = makeModel { $0.showOnDesktop = true }
            model.hideDesktop(.untilShown)
            XCTAssertFalse(settings.showOnDesktop)
            XCTAssertFalse(model.isOnDesktop)
            XCTAssertNil(model.desktopBackLine)
            model.showDesktopNow()
            XCTAssertTrue(settings.showOnDesktop)
            XCTAssertTrue(model.isOnDesktop)
        }

        func testAPinchStepsTheSizeAndStopsAtTheEnds() {
            let model = makeModel()
            model.stepDesktopSize(1)
            XCTAssertEqual(settings.desktopSize, .large)
            model.stepDesktopSize(1)
            XCTAssertEqual(settings.desktopSize, .large)
            model.stepDesktopSize(-1)
            model.stepDesktopSize(-1)
            model.stepDesktopSize(-1)
            XCTAssertEqual(settings.desktopSize, .small)
        }

        func testResetPutsTheCharacterBackInTheCorner() {
            let model = makeModel { $0.showOnDesktop = true }
            model.moveDesktop(to: DesktopPosition(x: 300, y: 400))
            model.hideDesktop(.hour)
            model.resetSettings()
            XCTAssertNil(settings.desktopPosition)
            XCTAssertFalse(settings.showOnDesktop)
            XCTAssertFalse(model.isOnDesktop)
            XCTAssertNil(model.desktopHiddenUntil)
        }

        // MARK: Menus

        func testTheMenusFollowTheState() {
            let model = makeModel()
            let titles = { (menu: NSMenu) in menu.items.map(\.title) }
            XCTAssertEqual(
                titles(model.menuBarMenu()), ["Say something", "Shh", "", "On the desktop", "", "Settings…", "Support…", "", "Quit"])
            XCTAssertEqual(
                titles(model.desktopMenu()), ["Say something", "Shh", "", "Size", "Hide", "", "Settings…", "Support…", "", "Quit"])
            model.shush(.hour)
            XCTAssertEqual(titles(model.menuBarMenu())[1], "Wake up")
            let desktop = model.menuBarMenu().items[3].submenu!
            XCTAssertEqual(titles(desktop), ["Show", "Hide for an hour", "Hide for the rest of today", "", "small", "medium", "large"])
            XCTAssertEqual(desktop.items[0].state, .off)
            XCTAssertFalse(desktop.items[1].isEnabled, "nothing to hide while it's off")
            XCTAssertEqual(desktop.items[5].state, .on, "medium is the default size")
        }
    }
#endif
