import DesklingCompanion
import DesklingCore
import Foundation

extension QuietTimeline {
    /// The timelines every companion app pins (and `conformance/companion` over the fixtures): 17 days of ticks,
    /// pokes, Shh, calls, full screen, being away, muted output, the notification channel, comments, off hours,
    /// weekends, a frequency change and a DST change, over `base` (the app's defaults with the frequency the timelines
    /// were written for: every 30 minutes). `dst` names the last one and sets its time zone, the app's own city
    /// ("evening-bucharest-dst-end", "Europe/Bucharest"). Names, starts, seeds and steps are fixed: an app's vectors
    /// stay byte-identical as long as its bank and defaults do.
    public static func standardCases(base: Settings, dst: (name: String, timeZone: String)) -> [Case] {
        let minute = Self.minute
        let call = Snapshot(busy: [.call])
        let fullScreen = Snapshot(busy: [.fullScreen])
        let away = Snapshot(idleSeconds: 600)
        let locked = Snapshot(isScreenLocked: true)
        let muted = Snapshot(isOutputMuted: true, notificationsAuthorized: true)
        let mutedNoBanner = Snapshot(isOutputMuted: true, notificationsAuthorized: false)
        let speaking = Snapshot(isSpeaking: true)
        func with(_ change: (inout Settings) -> Void) -> Settings {
            var settings = base
            change(&settings)
            return settings
        }
        let voiceChime = with { $0.channels = ["voice", "chime"] }
        return [
            Case(
                name: "ambient-monday-morning", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 120),
                    Input(advance: 0, action: "poke"),
                    Input(advance: minute, action: "tick", repeat: 25),
                ]),
            Case(
                name: "chatty-with-chime", start: (2026, 9, 29, 9, 0), seed: 7, settings: voiceChime,
                steps: [Input(advance: minute, action: "tick", repeat: 90)]),
            Case(
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
            Case(
                name: "shush-restored-from-settings", start: (2026, 9, 28, 10, 0),
                settings: with { $0.shushedUntil = "2026-09-28T10:30:00Z" },
                steps: [
                    Input(advance: minute, action: "tick", repeat: 29),
                    Input(advance: minute, action: "tick", repeat: 2),
                ]),
            Case(
                name: "shush-expired-in-settings", start: (2026, 9, 28, 10, 0),
                settings: with { $0.shushedUntil = "2026-09-28T09:00:00Z" },
                steps: [Input(advance: minute, action: "tick", repeat: 3)]),
            Case(
                name: "call-holds-then-after-call", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 15),
                    Input(advance: minute, action: "tick", repeat: 40, snapshot: call),
                    Input(advance: 0, action: "poke", snapshot: call),
                    Input(advance: minute, action: "tick", repeat: 3),
                ]),
            Case(
                name: "full-screen-hushed", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 60, snapshot: fullScreen),
                    Input(advance: minute, action: "tick", repeat: 2),
                ]),
            Case(
                name: "full-screen-ignored-by-setting", start: (2026, 9, 28, 10, 0), settings: with { $0.hushInFullScreen = false },
                steps: [Input(advance: minute, action: "tick", repeat: 60, snapshot: fullScreen)]),
            Case(
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
            Case(
                name: "muted-falls-back-to-notification", start: (2026, 9, 28, 10, 0), settings: voiceChime,
                steps: [
                    Input(advance: 0, action: "poke", snapshot: muted),
                    Input(advance: 0, action: "event", argument: "wentDark", snapshot: muted),
                    Input(advance: 0, action: "event", argument: "wentLight", snapshot: mutedNoBanner),
                    Input(advance: 0, action: "event", argument: "batteryLow", snapshot: speaking),
                    Input(advance: 0, action: "event", argument: "charging"),
                ]),
            Case(
                name: "notification-channel", start: (2026, 9, 28, 10, 0), settings: with { $0.channels = ["notification"] },
                steps: [
                    Input(advance: 0, action: "poke"),
                    Input(advance: 0, action: "event", argument: "unplugged"),
                    Input(advance: minute, action: "tick", repeat: 45),
                ]),
            Case(
                name: "quietly", start: (2026, 9, 28, 10, 0), settings: with { $0.channels = [] },
                steps: [
                    Input(advance: 0, action: "poke"),
                    Input(advance: 0, action: "event", argument: "wentDark"),
                ]),
            Case(
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
            Case(
                name: "off-hours", start: (2026, 9, 28, 20, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 30),
                    Input(advance: 0, action: "event", argument: "batteryLow"),
                    Input(advance: 0, action: "poke"),
                ]),
            Case(
                name: "weekend-off", start: (2026, 10, 3, 11, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 30),
                    Input(advance: 0, action: "event", argument: "wentDark"),
                ]),
            Case(
                name: "frequency-change-reschedules", start: (2026, 9, 28, 10, 0), settings: base,
                steps: [
                    Input(advance: minute, action: "tick", repeat: 5),
                    Input(advance: 0, action: "setFrequency", argument: "rarely"),
                    Input(advance: minute, action: "tick", repeat: 44),
                    Input(advance: minute, action: "tick", repeat: 40),
                    Input(advance: 0, action: "setChannels", argument: "voice,chime,notification"),
                    Input(advance: 0, action: "poke"),
                ]),
            Case(
                name: dst.name, timeZone: dst.timeZone, start: (2026, 10, 24, 17, 30), seed: 5,
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
}

extension PickerVector {
    /// The picker cases every companion app pins: the dayparts, the events, the recent lines skipped, the flavors
    /// (`single`: one flavor, `pair`: two, by the app's own case names; `leftEmpty`: a flavor with no line for a
    /// battery comment, so the pick falls back), the excluded lines and the fallbacks when an event has nothing left.
    /// `excluded` and `recent` are how many of Monday morning's candidates the excluded and recent cases take. The app
    /// adds its own cases (its button, its languages, its skins) around them.
    public static func standardCases(
        bank: PhraseBank, skin: SkinID, language: String = "en", single: (name: String, flavors: Set<FlavorID>),
        pair: (name: String, flavors: Set<FlavorID>), leftEmpty: FlavorID, excluded: Int, recent: Int = PhrasePicker.recentWindow
    ) -> [Case] {
        func context(
            _ hour: Int, _ weekday: Int, event: Tag? = nil, chime: Bool, recent: [String] = [], flavors: Set<FlavorID> = [],
            excluded: Set<String> = []
        ) -> PhraseContext {
            PhraseContext(
                hour: hour, weekday: weekday, event: event, chimeEnabled: chime, recentIDs: recent, flavors: flavors,
                excludedIDs: excluded, skin: skin, language: language)
        }
        let morning = PhrasePicker.candidates(in: bank, context: context(10, 2, chime: true))
        return [
            Case(name: "wednesday-afternoon", context: context(14, 4, chime: false)),
            Case(name: "friday-evening", context: context(19, 6, chime: true)),
            Case(name: "saturday-night", context: context(23, 7, chime: false)),
            Case(name: "sunday-dawn", context: context(4, 1, chime: false)),
            Case(name: "recent-skipped", context: context(10, 2, chime: true, recent: morning.prefix(recent).map(\.id))),
            Case(name: "event-welcome-back", context: context(10, 2, event: .welcomeBack, chime: true)),
            Case(name: "event-after-call", context: context(15, 3, event: .afterCall, chime: false)),
            Case(name: "event-battery-low", context: context(12, 5, event: .batteryLow, chime: false)),
            Case(name: "event-went-dark", context: context(18, 2, event: .wentDark, chime: false)),
            Case(name: single.name, context: context(14, 4, chime: true, flavors: single.flavors)),
            Case(name: pair.name, context: context(20, 6, chime: false, flavors: pair.flavors)),
            Case(
                name: "event-flavor-left-empty-falls-back",
                context: context(12, 5, event: .batteryLow, chime: false, flavors: [leftEmpty])),
            Case(name: "excluded-ids", context: context(10, 2, chime: true, excluded: Set(morning.prefix(excluded).map(\.id)))),
            Case(
                name: "event-all-excluded-falls-back",
                context: context(23, 7, event: .unplugged, chime: false, excluded: Set(bank.phrases(tagged: .unplugged).map(\.id)))),
            Case(
                name: "event-recent-exhausted",
                context: context(10, 2, event: .shushEnd, chime: false, recent: bank.phrases(tagged: .shushEnd).map(\.id))),
        ]
    }
}
