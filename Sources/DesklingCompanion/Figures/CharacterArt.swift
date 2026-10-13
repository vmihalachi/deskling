import Foundation

/// A character's compiled art and rig: what `tools/character.py` makes of a skin folder. The app's Swift value is
/// generated (`--swift-out`); the C# core loads the same data from `characters/<skin>.json` (`CharacterArtJSON`).
/// Vector parts are shapes; raster parts are images from the app's assets.
public struct CharacterArt: Equatable, Sendable {
    public let skin: SkinID
    public let viewBox: Size
    public let pivots: [String: Point]
    public let parts: [String: PartContent]
    public let rig: Rig
    public let glyphSize: Double
    /// The menu bar frames: a vector line drawing in `ink` for every skin.
    public let glyphFrames: [String: [PathShape]]

    public init(
        skin: SkinID, viewBox: Size, pivots: [String: Point], parts: [String: PartContent], rig: Rig, glyphSize: Double,
        glyphFrames: [String: [PathShape]]
    ) {
        self.skin = skin
        self.viewBox = viewBox
        self.pivots = pivots
        self.parts = parts
        self.rig = rig
        self.glyphSize = glyphSize
        self.glyphFrames = glyphFrames
    }

    /// Where a speech bubble's tail aims, in view-box units: the rig's bubble pivot (the origin if the art lacks it,
    /// which `CharacterArtJSON` and the compiler both refuse).
    public var bubblePoint: Point {
        pivots[rig.bubble.pivot] ?? Point(0, 0)
    }
}

/// How a character moves: which parts each mood shows and how its head droops, sways, pulses and blinks.
public struct Rig: Equatable, Sendable {
    public let period: Double
    public let stillTime: Double
    /// Parts every mood draws, in drawing order.
    public let always: [String]
    /// Droops and sways around its pivot; the mood's head extras, eyes and mouth ride on it.
    public let head: RigGroup
    /// Follows the head by `follow` of its angle, around its own pivot.
    public let stem: RigGroup
    /// Pulses (scales) around its pivot while talking, then moves with the head.
    public let pulse: RigGroup
    public let blink: Blink
    public let moods: [Mood: MoodRig]
    /// Glyph frame ids per mood.
    public let glyph: [Mood: [String]]
    public let bubble: BubbleAnchor

    public init(
        period: Double, stillTime: Double, always: [String], head: RigGroup, stem: RigGroup, pulse: RigGroup, blink: Blink,
        moods: [Mood: MoodRig], glyph: [Mood: [String]], bubble: BubbleAnchor
    ) {
        self.period = period
        self.stillTime = stillTime
        self.always = always
        self.head = head
        self.stem = stem
        self.pulse = pulse
        self.blink = blink
        self.moods = moods
        self.glyph = glyph
        self.bubble = bubble
    }
}

public struct RigGroup: Equatable, Sendable {
    public let pivot: String
    public let parts: [String]
    public let follow: Double

    public init(pivot: String, parts: [String], follow: Double) {
        self.pivot = pivot
        self.parts = parts
        self.follow = follow
    }
}

/// The eyes close for `duration` seconds at the end of each interval, cycling through `intervals`.
public struct Blink: Equatable, Sendable {
    public let duration: Double
    public let intervals: [Double]
    public let closedEyes: String

    public init(duration: Double, intervals: [Double], closedEyes: String) {
        self.duration = duration
        self.intervals = intervals
        self.closedEyes = closedEyes
    }
}

/// Where a speech bubble aims (a pivot) and how far it tucks in past the character's empty sides, as a fraction of
/// the character's width.
public struct BubbleAnchor: Equatable, Sendable {
    public let pivot: String
    public let tuck: Double

    public init(pivot: String, tuck: Double) {
        self.pivot = pivot
        self.tuck = tuck
    }
}

public struct MoodRig: Equatable, Sendable {
    public let eyes: String
    /// One mouth, or several that alternate `mouthRate` times a second.
    public let mouth: [String]
    public let mouthRate: Double
    /// Parts that ride on the head under the eyes and mouth, such as cheeks.
    public let headExtras: [String]
    /// Parts drawn last and never moved, such as the sleeping "zz".
    public let overlays: [String]
    public let blink: Bool
    /// Degrees, clockwise, around the head's pivot.
    public let droop: Double
    public let sink: Double
    /// Sway amplitude in degrees over `swayPeriod` seconds.
    public let sway: Double
    public let swayPeriod: Double
    /// Pulse amplitude (0.03 = 3 %).
    public let pulse: Double
    /// The closed mouth between words when a recorded clip's envelope drives the mouth (level 0); nil uses the first
    /// of `mouth`. The talking mouths are listed from the least open to the most.
    public let restMouth: String?

    public init(
        eyes: String, mouth: [String], mouthRate: Double, headExtras: [String], overlays: [String], blink: Bool,
        droop: Double, sink: Double, sway: Double, swayPeriod: Double, pulse: Double, restMouth: String? = nil
    ) {
        self.eyes = eyes
        self.mouth = mouth
        self.mouthRate = mouthRate
        self.headExtras = headExtras
        self.overlays = overlays
        self.blink = blink
        self.droop = droop
        self.sink = sink
        self.sway = sway
        self.swayPeriod = swayPeriod
        self.pulse = pulse
        self.restMouth = restMouth
    }
}
