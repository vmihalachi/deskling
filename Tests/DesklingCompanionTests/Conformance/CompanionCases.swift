import DesklingCompanion
import DesklingCore
import DesklingTesting
import Foundation

/// The cases behind `conformance/companion/`, over the fixtures. Cadence, desktop and the quiet timelines carry no app
/// data, so their lists are sunnysays' own; the picker and quiet cases are the standard ones (`PickerVector.standardCases`,
/// `QuietTimeline.standardCases`) plus the fixture's own.
enum CompanionCases: CompanionVectors {
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

    static let pickerCases: [PickerVector.Case] = {
        let morning = PhrasePicker.candidates(in: bank, context: context(10, 2, chime: true))
        let (a, b, c) = (FixtureIDs.flavorA, FixtureIDs.flavorB, FixtureIDs.flavorC)
        let standard = PickerVector.standardCases(
            bank: bank, skin: FixtureIDs.vectorSkin, single: ("flavors-calm-only", [c]), pair: ("flavors-playful-and-wonder", [a, b]),
            leftEmpty: a, excluded: 4, recent: 4)
        return [
            .init(name: "monday-morning-chime", context: context(10, 2, chime: true)),
            .init(name: "monday-morning-no-chime", context: context(10, 2, chime: false)),
            .init(
                name: "recent-older-than-window-count-again",
                context: context(
                    10, 2, chime: true,
                    recent: morning.prefix(2).map(\.id) + Array(repeating: "general-c1", count: PhrasePicker.recentWindow))),
            .init(name: "event-button-press", context: context(23, 7, event: .buttonPress, chime: true)),
        ] + standard + [
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
            name: "sometimes",
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
            name: "every-2-hours",
            settings: .init(
                frequency: "every2Hours", activeStartMinute: 540, activeEndMinute: 1080, activeWeekdays: weekdays,
                quietHours: [], hushInFullScreen: true),
            start: (2026, 9, 28, 10, 0), seed: 5, minutes: 480),
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

    // MARK: Quiet timelines (the standard 17 over the fixture's defaults, plus the fixture's own)

    static var quietCases: [QuietTimeline.Case] {
        // Every 30 minutes, the default these timelines were written for, so each still hears what it tests.
        var base = QuietTimeline.Settings(defaults)
        base.frequency = "sometimes"
        return QuietTimeline.standardCases(base: base, dst: ("evening-bucharest-dst-end", "Europe/Bucharest")) + quietExtras
    }

    /// The fixture's own timelines: a two-part joke delivered by the button, keyed on its id like any line, and the
    /// Italian lines in an Italian afternoon.
    static let quietExtras: [QuietTimeline.Case] = [
        .init(
            name: "sequence-delivered", start: (2026, 9, 30, 16, 0), seed: 3,
            settings: {
                var s = QuietTimeline.Settings(defaults)
                s.frequency = "sometimes"
                return s
            }(),
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
