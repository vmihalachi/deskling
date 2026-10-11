import Foundation

/// Any character drawn from compiled art: the rig picks parts per mood and moves the head. A new mascot or skin is a
/// new art folder run through `tools/character.py`, not new code. Pure; part of `conformance/companion/characters`.
/// Never reads assets: image parts are placed, and the app draws them.
public struct RiggedCharacter: DrawableCharacter {
    public let art: CharacterArt

    public init(art: CharacterArt) {
        self.art = art
    }

    public var skin: SkinID { art.skin }
    public var viewBox: Size { art.viewBox }
    public var glyphSize: Double { art.glyphSize }

    public func pose(mood: Mood, time: Double) -> Pose {
        let rig = art.rig
        let m = moodRig(mood)
        let swayPhase = Self.wrapped(time, m.swayPeriod) / m.swayPeriod
        let angle = m.droop + m.sway * sin(2 * .pi * swayPhase)
        let mouthIndex = m.mouth.count > 1 ? Int(floor(Self.wrapped(time, Double(m.mouth.count) / m.mouthRate) * m.mouthRate)) : 0
        let pulse = m.pulse == 0 ? 0 : m.pulse * abs(sin(.pi * Self.wrapped(time, 2 / m.mouthRate) * m.mouthRate))
        let blinking = m.blink && isBlinking(at: time)
        return Pose(
            mood: mood, headAngle: angle, sink: m.sink, pulse: pulse, eyes: blinking ? rig.blink.closedEyes : m.eyes,
            mouth: m.mouth[mouthIndex % m.mouth.count], headExtras: m.headExtras, overlays: m.overlays, blinking: blinking)
    }

    public func stillPose(mood: Mood) -> Pose {
        let m = moodRig(mood)
        var pose = pose(mood: mood, time: art.rig.stillTime)
        pose.blinking = false
        pose.eyes = m.eyes
        pose.mouth = m.mouth[0]
        return pose
    }

    public func layers(for pose: Pose) -> [PlacedPart] {
        let rig = art.rig
        let head = Transform.rotation(degrees: pose.headAngle, around: pivot(rig.head.pivot))
            .concatenating(.translation(0, pose.sink))
        let stem = Transform.rotation(degrees: pose.headAngle * rig.stem.follow, around: pivot(rig.stem.pivot))
        let pulse = Transform.scale(1 + pose.pulse, around: pivot(rig.pulse.pivot)).concatenating(head)
        var layers: [PlacedPart] = []
        for name in rig.always {
            let transform =
                rig.pulse.parts.contains(name)
                ? pulse : rig.head.parts.contains(name) ? head : rig.stem.parts.contains(name) ? stem : .identity
            layers.append(PlacedPart(name: name, transform: transform, content: part(name)))
        }
        for name in pose.headExtras + [pose.eyes, pose.mouth] {
            layers.append(PlacedPart(name: name, transform: head, content: part(name)))
        }
        for name in pose.overlays {
            layers.append(PlacedPart(name: name, transform: .identity, content: part(name)))
        }
        return layers
    }

    public func shapes(for pose: Pose) -> [PathShape] {
        layers(for: pose).flatMap { layer in
            (layer.content.shapes ?? []).map { $0.transformed(by: layer.transform) }
        }
    }

    public func glyph(mood: Mood, frame: Int) -> [PathShape] {
        let frames = art.rig.glyph[mood] ?? []
        guard !frames.isEmpty else { return [] }
        let index = ((frame % frames.count) + frames.count) % frames.count
        return art.glyphFrames[frames[index]] ?? []
    }

    public func glyphFrameCount(mood: Mood) -> Int {
        art.rig.glyph[mood]?.count ?? 0
    }

    // MARK: Helpers

    private func moodRig(_ mood: Mood) -> MoodRig {
        guard let m = art.rig.moods[mood] else { preconditionFailure("\(art.skin) has no rig for \(mood)") }
        return m
    }

    private func part(_ name: String) -> PartContent {
        guard let content = art.parts[name] else { preconditionFailure("\(art.skin) has no part \(name)") }
        return content
    }

    private func pivot(_ name: String) -> Point {
        guard let point = art.pivots[name] else { preconditionFailure("\(art.skin) has no pivot \(name)") }
        return point
    }

    /// Whether `time` falls in a blink: the last `duration` seconds of each interval, cycling.
    private func isBlinking(at time: Double) -> Bool {
        let blink = art.rig.blink
        let cycle = blink.intervals.reduce(0, +)
        let t = Self.wrapped(time, cycle)
        var end = 0.0
        for interval in blink.intervals {
            end += interval
            if t >= end - blink.duration && t < end { return true }
        }
        return false
    }

    /// `value` modulo `period`, in 0..<period, so very long running times stay exact.
    static func wrapped(_ value: Double, _ period: Double) -> Double {
        guard period > 0, period.isFinite else { return value }
        let r = value.truncatingRemainder(dividingBy: period)
        return r < 0 ? r + period : r
    }
}
