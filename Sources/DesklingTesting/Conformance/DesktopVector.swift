import DesklingCompanion
import DesklingCore
import Foundation

/// `desktop.json`: `DesktopPresence` over short timelines (the setting, busy moments, hide for a while, show now),
/// the desktop bubble's lingering time for sample lines and, when asked, `PhraseTiming.partDelay` for the parts of a
/// sequence. The Windows port must agree step for step.
public enum DesktopVector {
    public static let file = "desktop.json"
    public static let start = Date(timeIntervalSince1970: 1_800_000_000)

    public struct Step: Codable, Equatable, Sendable {
        /// Seconds since the timeline's start.
        public var t: Double
        public var enabled: Bool
        public var busy: [String]
        /// Hide for this many seconds from `t`, before the update.
        public var hideForSeconds: Int?
        /// End a hide for a while, before the update.
        public var showNow: Bool?
        public var shown: Bool
    }

    public struct Timeline: Codable, Equatable, Sendable {
        public var name: String
        public var steps: [Step]
    }

    public struct Bubble: Codable, Equatable, Sendable {
        public var text: String
        public var seconds: Double
    }

    /// The wait before the next part of a sequence: the beat after a spoken part, else the bubble time of the text.
    public struct PartDelay: Codable, Equatable, Sendable {
        public var spoken: Bool
        public var previousPart: String
        public var seconds: Double
    }

    public struct Vector: Codable, Equatable, Sendable {
        public var reappearDelaySeconds: Int
        public var hourHideSeconds: Int
        public var timelines: [Timeline]
        public var bubbles: [Bubble]
        public var partDelays: [PartDelay]?
    }

    /// The inputs of one step.
    public struct Input: Sendable {
        public var t: Double
        public var enabled: Bool
        public var busy: [BusyReason]
        public var hide: Int?
        public var showNow: Bool

        public init(_ t: Double, _ busy: [BusyReason] = [], enabled: Bool = true, hide: Int? = nil, showNow: Bool = false) {
            self.t = t
            self.enabled = enabled
            self.busy = busy
            self.hide = hide
            self.showNow = showNow
        }
    }

    public struct Case: Sendable {
        public var name: String
        public var steps: [Input]

        public init(_ name: String, _ steps: [Input]) {
            self.name = name
            self.steps = steps
        }
    }

    public static func timeline(_ c: Case) -> Timeline {
        var presence = DesktopPresence()
        let steps = c.steps.map { input -> Step in
            let now = start.addingTimeInterval(input.t)
            if let hide = input.hide { presence.hide(until: now.addingTimeInterval(TimeInterval(hide))) }
            if input.showNow { presence.showNow() }
            let shown = presence.update(enabled: input.enabled, snapshot: Snapshot(busy: Set(input.busy)), now: now)
            return Step(
                t: input.t, enabled: input.enabled, busy: input.busy.map(SnapshotData.name).sorted(), hideForSeconds: input.hide,
                showNow: input.showNow ? true : nil, shown: shown)
        }
        return Timeline(name: c.name, steps: steps)
    }

    /// `partDelays` lists (spoken, previous part) pairs; none leaves the field out.
    public static func vector(timelines: [Case], bubbleTexts: [String], partDelays: [(spoken: Bool, previousPart: String)] = []) -> Vector {
        Vector(
            reappearDelaySeconds: Int(DesktopPresence.reappearDelay), hourHideSeconds: Int(DesktopPresence.hourHide),
            timelines: timelines.map(timeline),
            bubbles: bubbleTexts.map { Bubble(text: $0, seconds: DesktopPresence.bubbleSeconds(for: $0)) },
            partDelays: partDelays.isEmpty
                ? nil
                : partDelays.map {
                    PartDelay(
                        spoken: $0.spoken, previousPart: $0.previousPart,
                        seconds: PhraseTiming.partDelay(spoken: $0.spoken, previousPart: $0.previousPart))
                })
    }

    public static func files(
        timelines: [Case], bubbleTexts: [String], partDelays: [(spoken: Bool, previousPart: String)] = []
    ) throws -> [String: Data] {
        [file: try ConformanceSupport.encode(vector(timelines: timelines, bubbleTexts: bubbleTexts, partDelays: partDelays))]
    }

    public static func verify(_ data: Data, file: String) throws -> [String] {
        let committed = try ConformanceSupport.decode(Vector.self, from: data)
        var problems: [String] = []
        if committed.reappearDelaySeconds != Int(DesktopPresence.reappearDelay) {
            problems.append("\(file): reappearDelaySeconds differs")
        }
        if committed.hourHideSeconds != Int(DesktopPresence.hourHide) {
            problems.append("\(file): hourHideSeconds differs")
        }
        for timeline in committed.timelines {
            let inputs = timeline.steps.map { step in
                Input(
                    step.t, step.busy.compactMap(SnapshotData.busyReason), enabled: step.enabled, hide: step.hideForSeconds,
                    showNow: step.showNow ?? false)
            }
            if self.timeline(Case(timeline.name, inputs)) != timeline { problems.append("\(file): timeline \(timeline.name) differs") }
        }
        for bubble in committed.bubbles where abs(DesktopPresence.bubbleSeconds(for: bubble.text) - bubble.seconds) > 1e-9 {
            problems.append("\(file): bubble seconds for \"\(bubble.text)\" differ")
        }
        for delay in committed.partDelays ?? []
        where abs(PhraseTiming.partDelay(spoken: delay.spoken, previousPart: delay.previousPart) - delay.seconds) > 1e-9 {
            problems.append("\(file): the part delay after \"\(delay.previousPart)\" (spoken: \(delay.spoken)) differs")
        }
        return problems
    }
}
