import Foundation

/// A character the app can draw, whichever skin. Geometry only: the app fills and strokes shapes with its palette
/// and draws image parts from its assets. A pose comes from the mood and the time in seconds (or the still pose under
/// Reduce Motion); the layers come from the pose. The menu bar / tray glyph is a vector template in a `glyphSize`
/// square for every skin.
public protocol DrawableCharacter: Sendable {
    var skin: SkinID { get }
    var viewBox: Size { get }
    var glyphSize: Double { get }
    /// The pose at `time`; with a `mouthLevel` (0–3, a recorded clip's envelope right now) the mouth and the pulse
    /// follow the voice's loudness instead of the mood's timer.
    func pose(mood: Mood, time: Double, mouthLevel: Int?) -> Pose
    /// The pose shown when nothing moves: no blink, the first mouth.
    func stillPose(mood: Mood) -> Pose
    /// The parts to draw, in order: `always` parts under the pulse, head or stem transform (or none), then the head
    /// extras, eyes and mouth under the head, then the overlays untransformed.
    func layers(for pose: Pose) -> [PlacedPart]
    /// The vector convenience: every shape part flattened through its transform; image parts are skipped.
    func shapes(for pose: Pose) -> [PathShape]
    /// Glyph frames: the mood's frames cycle (two while talking).
    func glyph(mood: Mood, frame: Int) -> [PathShape]
    func glyphFrameCount(mood: Mood) -> Int
}

extension DrawableCharacter {
    /// The pose at `time`, the mouth on the mood's timer.
    public func pose(mood: Mood, time: Double) -> Pose {
        pose(mood: mood, time: time, mouthLevel: nil)
    }
}

/// One part placed for drawing: its content in view-box units and the transform the rig gives it.
public struct PlacedPart: Equatable, Sendable {
    public let name: String
    public let transform: Transform
    public let content: PartContent

    public init(name: String, transform: Transform, content: PartContent) {
        self.name = name
        self.transform = transform
        self.content = content
    }
}
