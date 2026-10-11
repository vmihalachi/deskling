import DesklingCompanion
import DesklingCore
import DesklingTesting
import Foundation

/// The cases behind `conformance/companion/`, over the fixtures. Cadence, desktop and the quiet timelines carry no app
/// data, so their lists are sunnysays' own; the picker cases are sunnysays' re-expressed over fixture ids.
enum CompanionCases {
    static let bank = PhraseBank.fixture
    static let defaults = CompanionDefaults.fixture
    /// The app's flavors in display order.
    static let flavors = [FixtureIDs.flavorA, FixtureIDs.flavorB, FixtureIDs.flavorC]
    static let characters = [RiggedCharacter(art: FixtureArt.vector), RiggedCharacter(art: FixtureArt.raster)]

    /// `settings.json`: the package tree plus the app's skins.
    static var settings: JSONValue {
        var tree = SettingsVector.base(defaults: defaults)
        tree["skins"] = .strings(FixtureIDs.skins.map(\.rawValue))
        return tree
    }

    // MARK: Picker

    static func context(
        _ hour: Int, _ weekday: Int, event: Tag? = nil, chime: Bool, recent: [String] = [], flavors: Set<FlavorID> = [],
        excluded: Set<String> = [], skin: SkinID = FixtureIDs.vectorSkin, language: String = "en"
    ) -> PhraseContext {
        PhraseContext(
            hour: hour, weekday: weekday, event: event, chimeEnabled: chime, recentIDs: recent, flavors: flavors,
            excludedIDs: excluded, skin: skin, language: language)
    }

    static let picker: [PickerVector.Case] = {
        let morning = PhrasePicker.candidates(in: bank, context: context(10, 2, chime: true))
        let (a, b, c) = (FixtureIDs.flavorA, FixtureIDs.flavorB, FixtureIDs.flavorC)
        return [
            .init(name: "monday-morning-chime", context: context(10, 2, chime: true)),
            .init(name: "monday-morning-no-chime", context: context(10, 2, chime: false)),
            .init(name: "wednesday-afternoon", context: context(14, 4, chime: false)),
            .init(name: "friday-evening", context: context(19, 6, chime: true)),
            .init(name: "saturday-night", context: context(23, 7, chime: false)),
            .init(name: "sunday-dawn", context: context(4, 1, chime: false)),
            .init(name: "recent-skipped", context: context(10, 2, chime: true, recent: morning.prefix(4).map(\.id))),
            .init(
                name: "recent-older-than-window-count-again",
                context: context(
                    10, 2, chime: true,
                    recent: morning.prefix(2).map(\.id) + Array(repeating: "general-c1", count: PhrasePicker.recentWindow))),
            .init(name: "event-welcome-back", context: context(10, 2, event: .welcomeBack, chime: true)),
            .init(name: "event-after-call", context: context(15, 3, event: .afterCall, chime: false)),
            .init(name: "event-button-press", context: context(23, 7, event: .buttonPress, chime: true)),
            .init(name: "event-battery-low", context: context(12, 5, event: .batteryLow, chime: false)),
            .init(name: "event-went-dark", context: context(18, 2, event: .wentDark, chime: false)),
            .init(name: "flavors-calm-only", context: context(14, 4, chime: true, flavors: [c])),
            .init(name: "flavors-playful-and-wonder", context: context(20, 6, chime: false, flavors: [a, b])),
            .init(
                name: "event-flavor-left-empty-falls-back",
                context: context(12, 5, event: .batteryLow, chime: false, flavors: [a])),
            .init(name: "excluded-ids", context: context(10, 2, chime: true, excluded: Set(morning.prefix(4).map(\.id)))),
            .init(
                name: "event-all-excluded-falls-back",
                context: context(23, 7, event: .unplugged, chime: false, excluded: Set(bank.phrases(tagged: .unplugged).map(\.id)))),
            .init(
                name: "event-recent-exhausted",
                context: context(10, 2, event: .shushEnd, chime: false, recent: bank.phrases(tagged: .shushEnd).map(\.id))),
            // New with the companion layer.
            .init(
                name: "sequence-picked",
                context: context(16, 3, event: .buttonPress, chime: false, excluded: ["button-press-1"])),
            .init(name: "skin-restricted-dropped", context: context(14, 4, chime: false, flavors: [b])),
            .init(
                name: "skin-restricted-kept", context: context(14, 4, chime: false, flavors: [b], skin: FixtureIDs.rasterSkin)),
            .init(name: "language-scoped-dropped", context: context(14, 4, chime: false, flavors: [c])),
            .init(name: "language-scoped-kept", context: context(14, 4, chime: false, flavors: [c], language: "it-IT")),
        ]
    }()

    // MARK: Cadence (sunnysays' cases, byte for byte)

    static let weekdays = [2, 3, 4, 5, 6]

    static let cadence: [CadenceVector.Case] = [
        .init(
            name: "sometimes-default",
            settings: .init(
                frequency: "sometimes", activeStartMinute: 540, activeEndMinute: 1080, activeWeekdays: weekdays,
                quietHours: [], hushInFullScreen: true),
            start: (2026, 9, 28, 10, 0)),
        .init(
            name: "rarely",
            settings: .init(
                frequency: "rarely", activeStartMinute: 540, activeEndMinute: 1080, activeWeekdays: weekdays,
                quietHours: [], hushInFullScreen: true),
            start: (2026, 9, 28, 10, 0), seed: 7, minutes: 480),
        .init(
            name: "chatty",
            settings: .init(
                frequency: "chatty", activeStartMinute: 540, activeEndMinute: 1080, activeWeekdays: weekdays,
                quietHours: [], hushInFullScreen: false),
            start: (2026, 9, 28, 10, 0), seed: 1),
        .init(
            name: "quiet-hour-and-end-of-day",
            settings: .init(
                frequency: "chatty", activeStartMinute: 540, activeEndMinute: 1080, activeWeekdays: weekdays,
                quietHours: [13], hushInFullScreen: true),
            start: (2026, 9, 28, 12, 0), seed: 3, minutes: 420),
        .init(
            name: "weekend-off-bucharest",
            settings: .init(
                frequency: "sometimes", activeStartMinute: 600, activeEndMinute: 1320, activeWeekdays: [1, 7],
                quietHours: [], hushInFullScreen: true),
            timeZone: "Europe/Bucharest", start: (2026, 10, 2, 21, 0), seed: 9, minutes: 900),
    ]

    // MARK: Desktop (sunnysays' timelines and bubbles, plus the part delays)

    static let desktopTimelines: [DesktopVector.Case] = {
        typealias I = DesktopVector.Input
        return [
            .init("calm", [I(0), I(20, enabled: false), I(40)]),
            .init("call", [I(0), I(20, [.call]), I(25, [.call]), I(26), I(34.9), I(35)]),
            .init("fullScreen", [I(0, [.fullScreen]), I(9.5), I(10)]),
            .init("callAndFullScreen", [I(0, [.call, .fullScreen]), I(5, [.fullScreen]), I(14), I(15)]),
            .init("busyWhileDisabled", [I(0, [.call], enabled: false), I(5), I(10)]),
            .init("hideForAnHour", [I(0, hide: Int(DesktopPresence.hourHide)), I(1800), I(3599), I(3600)]),
            .init("hideThenShowNow", [I(0, hide: 3600), I(60, showNow: true)]),
            .init("callAtTheEndOfAHide", [I(0, hide: 60), I(55, [.call]), I(60), I(65)]),
            .init("hideWhileDisabled", [I(0, enabled: false, hide: 60), I(30), I(60)]),
        ]
    }()

    static let bubbleTexts = [
        "", "Hi!", "♪ hm hm hmm ♪", "Ça va, ça va ? Encore un café ?",
        "Some days the light comes in sideways and everything looks like a painting.",
        String(repeating: "A long line that keeps going. ", count: 8),
    ]

    static let partDelays: [(spoken: Bool, previousPart: String)] = [
        (true, "Fixture joke, setup"), (false, "Fixture joke, setup"), (false, ""),
        (false, String(repeating: "A long setup that keeps going. ", count: 8)),
    ]

    // MARK: Quiet timelines (sunnysays' 17 over the given defaults, plus the fixture's sequence)

    static func quiet(defaults: CompanionDefaults) -> [QuietTimeline.Case] {
        typealias Input = QuietTimeline.Input
        typealias Settings = QuietTimeline.Settings
        let minute = QuietTimeline.minute
        let base = Settings(defaults)
        let call = Snapshot(busy: [.call])
        let fullScreen = Snapshot(busy: [.fullScreen])
        let away = Snapshot(idleSeconds: 600)
        let locked = Snapshot(isScreenLocked: true)
        let muted = Snapshot(isOutputMuted: true, notificationsAuthorized: true)
        let mutedNoBanner = Snapshot(isOutputMuted: true, notificationsAuthorized: false)
        let speaking = Snapshot(isSpeaking: true)
        func with(_ change: (inout Settings) -> Void) -> Settings {
            var s = base
            change(&s)
            return s
        }
        let voiceChime = with { $0.channels = ["voice", "chime"] }
        return [
            .init(
                name: "ambient-monday-morning", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 120),
                    Input(advance: 0, action: "poke"),
                    Input(advance: minute, action: "tick", repeat: 25),
                ]),
            .init(
                name: "chatty-with-chime", start: (2026, 9, 29, 9, 0), seed: 7, settings: voiceChime,
                steps: [Input(advance: minute, action: "tick", repeat: 90)]),
            .init(
                name: "shush-hour-then-wake-line", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: 0, action: "shush", argument: "hour"),
                    Input(advance: minute, action: "tick", repeat: 59),
                    Input(advance: minute, action: "tick", repeat: 2),
                    Input(advance: 0, action: "shush", argument: "untilISay"),
                    Input(advance: minute, action: "tick", repeat: 30),
                    Input(advance: 0, action: "wake"),
                    Input(advance: 0, action: "wake"),
                    Input(advance: 0, action: "shush", argument: "restOfToday"),
                    Input(advance: minute, action: "tick", repeat: 5),
                ]),
            .init(
                name: "shush-restored-from-settings", start: (2026, 9, 28, 10, 0),
                settings: with { $0.shushedUntil = "2026-09-28T10:30:00Z" },
                steps: [
                    Input(advance: minute, action: "tick", repeat: 29),
                    Input(advance: minute, action: "tick", repeat: 2),
                ]),
            .init(
                name: "shush-expired-in-settings", start: (2026, 9, 28, 10, 0),
                settings: with { $0.shushedUntil = "2026-09-28T09:00:00Z" },
                steps: [Input(advance: minute, action: "tick", repeat: 3)]),
            .init(
                name: "call-holds-then-after-call", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 15),
                    Input(advance: minute, action: "tick", repeat: 40, snapshot: call),
                    Input(advance: 0, action: "poke", snapshot: call),
                    Input(advance: minute, action: "tick", repeat: 3),
                ]),
            .init(
                name: "full-screen-hushed", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 60, snapshot: fullScreen),
                    Input(advance: minute, action: "tick", repeat: 2),
                ]),
            .init(
                name: "full-screen-ignored-by-setting", start: (2026, 9, 28, 10, 0), settings: with { $0.hushInFullScreen = false },
                steps: [Input(advance: minute, action: "tick", repeat: 60, snapshot: fullScreen)]),
            .init(
                name: "away-welcome-back-cooldown", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick"),
                    Input(advance: minute, action: "tick", repeat: 10, snapshot: away),
                    Input(advance: minute, action: "tick"),
                    Input(advance: minute, action: "tick", repeat: 6, snapshot: away),
                    Input(advance: minute, action: "tick"),
                    Input(advance: minute, action: "tick", repeat: 10, snapshot: locked),
                    Input(advance: 31 * minute, action: "tick", snapshot: locked),
                    Input(advance: minute, action: "tick"),
                ]),
            .init(
                name: "muted-falls-back-to-notification", start: (2026, 9, 28, 10, 0), settings: voiceChime,
                steps: [
                    Input(advance: 0, action: "poke", snapshot: muted),
                    Input(advance: 0, action: "event", argument: "wentDark", snapshot: muted),
                    Input(advance: 0, action: "event", argument: "wentLight", snapshot: mutedNoBanner),
                    Input(advance: 0, action: "event", argument: "batteryLow", snapshot: speaking),
                    Input(advance: 0, action: "event", argument: "charging"),
                ]),
            .init(
                name: "notification-channel", start: (2026, 9, 28, 10, 0), settings: with { $0.channels = ["notification"] },
                steps: [
                    Input(advance: 0, action: "poke"),
                    Input(advance: 0, action: "event", argument: "unplugged"),
                    Input(advance: minute, action: "tick", repeat: 45),
                ]),
            .init(
                name: "quietly", start: (2026, 9, 28, 10, 0), settings: with { $0.channels = [] },
                steps: [
                    Input(advance: 0, action: "poke"),
                    Input(advance: 0, action: "event", argument: "wentDark"),
                ]),
            .init(
                name: "comments-off", start: (2026, 9, 28, 10, 0),
                settings: with {
                    $0.greetOnReturn = false
                    $0.commentOnPower = false
                    $0.commentOnAppearance = false
                },
                steps: [
                    Input(advance: 0, action: "event", argument: "batteryLow"),
                    Input(advance: 0, action: "event", argument: "wentDark"),
                    Input(advance: minute, action: "tick"),
                    Input(advance: minute, action: "tick", repeat: 10, snapshot: away),
                    Input(advance: minute, action: "tick"),
                    Input(advance: 0, action: "poke"),
                ]),
            .init(
                name: "off-hours", start: (2026, 9, 28, 20, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 30),
                    Input(advance: 0, action: "event", argument: "batteryLow"),
                    Input(advance: 0, action: "poke"),
                ]),
            .init(
                name: "weekend-off", start: (2026, 10, 3, 11, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 30),
                    Input(advance: 0, action: "event", argument: "wentDark"),
                ]),
            .init(
                name: "frequency-change-reschedules", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 5),
                    Input(advance: 0, action: "setFrequency", argument: "rarely"),
                    Input(advance: minute, action: "tick", repeat: 44),
                    Input(advance: minute, action: "tick", repeat: 40),
                    Input(advance: 0, action: "setChannels", argument: "voice,chime,notification"),
                    Input(advance: 0, action: "poke"),
                ]),
            .init(
                name: "evening-bucharest-dst-end", timeZone: "Europe/Bucharest", start: (2026, 10, 24, 17, 30), seed: 5,
                settings: with {
                    $0.activeStartMinute = 8 * 60
                    $0.activeEndMinute = 22 * 60
                    $0.activeWeekdays = [1, 7]
                },
                steps: [
                    Input(advance: minute, action: "tick", repeat: 90),
                    Input(advance: 0, action: "shush", argument: "restOfToday"),
                    Input(advance: minute, action: "tick", repeat: 3),
                    Input(advance: 0, action: "wake"),
                ]),
        ]
    }

    /// The fixture's own timelines: a two-part joke delivered by the button, keyed on its id like any line, and the
    /// Italian lines in an Italian afternoon.
    static let quietExtras: [QuietTimeline.Case] = [
        .init(
            name: "sequence-delivered", start: (2026, 9, 30, 16, 0), seed: 3, settings: QuietTimeline.Settings(defaults),
            steps: [
                QuietTimeline.Input(advance: 0, action: "poke", repeat: 3),
                QuietTimeline.Input(advance: QuietTimeline.minute, action: "tick", repeat: 30),
            ]),
        .init(
            name: "italian-afternoon", start: (2026, 9, 30, 14, 0), seed: 3, language: "it",
            settings: {
                var s = QuietTimeline.Settings(defaults)
                s.frequency = "chatty"
                return s
            }(),
            steps: [QuietTimeline.Input(advance: QuietTimeline.minute, action: "tick", repeat: 180)]),
    ]
}
