import Foundation

/// Where a character is at one moment: plain numbers and part ids, so a pose can be stored, compared, replayed by
/// the Windows port and blended (the 0.6 s spring when the character wakes from Shh).
public struct Pose: Equatable, Sendable {
    public var mood: Mood
    /// Degrees, clockwise, around the head's pivot: droop plus sway.
    public var headAngle: Double
    public var sink: Double
    public var pulse: Double
    public var eyes: String
    public var mouth: String
    /// Parts that ride on the head under the eyes and mouth, such as cheeks.
    public var headExtras: [String]
    /// Parts drawn last and never moved, such as the sleeping "zz".
    public var overlays: [String]
    public var blinking: Bool

    public init(
        mood: Mood, headAngle: Double, sink: Double, pulse: Double, eyes: String, mouth: String, headExtras: [String],
        overlays: [String], blinking: Bool
    ) {
        self.mood = mood
        self.headAngle = headAngle
        self.sink = sink
        self.pulse = pulse
        self.eyes = eyes
        self.mouth = mouth
        self.headExtras = headExtras
        self.overlays = overlays
        self.blinking = blinking
    }

    /// Numbers interpolate; parts and flags switch to `target`'s at the halfway point.
    public func blended(to target: Pose, progress: Double) -> Pose {
        let t = min(max(progress, 0), 1)
        if t == 1 { return target }  // exactly, not 14 + (target − 14) × 1
        var pose = t < 0.5 ? self : target
        pose.headAngle = headAngle + (target.headAngle - headAngle) * t
        pose.sink = sink + (target.sink - sink) * t
        pose.pulse = pulse + (target.pulse - pulse) * t
        return pose
    }
}
