#if os(macOS)
    import DesklingCompanion

    /// A 100 × 100 raster character for the render tests: a red body under a blue head that droops 90° around the neck
    /// (50, 50) when shushed; eyes and mouth are a clear image. The assets are `render-test.<part>` in the test bundle.
    enum FixtureArtForUI {
        static let square: CharacterArt = {
            func image(_ part: String, _ frame: Rect) -> PartContent { .image(asset: "render-test.\(part)", frame: frame) }
            var moods: [Mood: MoodRig] = [:]
            var glyph: [Mood: [String]] = [:]
            for mood in Mood.allCases {
                moods[mood] = MoodRig(
                    eyes: "none", mouth: ["none"], mouthRate: 0, headExtras: [], overlays: [], blink: false,
                    droop: mood == .shushed ? 90 : 0, sink: 0, sway: 0, swayPeriod: 4, pulse: 0)
                glyph[mood] = ["g"]
            }
            return CharacterArt(
                skin: "render-test", viewBox: Size(100, 100),
                pivots: ["base": Point(50, 90), "neck": Point(50, 50), "face": Point(50, 30)],
                parts: [
                    "body": image("body", Rect(40, 50, 20, 40)), "head": image("head", Rect(30, 10, 40, 40)),
                    "none": image("none", Rect(45, 25, 10, 10)),
                ],
                rig: Rig(
                    period: 4, stillTime: 0, always: ["body", "head"], head: RigGroup(pivot: "neck", parts: ["head"], follow: 1),
                    stem: RigGroup(pivot: "base", parts: ["body"], follow: 0), pulse: RigGroup(pivot: "face", parts: [], follow: 1),
                    blink: Blink(duration: 0.1, intervals: [4], closedEyes: "none"), moods: moods, glyph: glyph,
                    bubble: BubbleAnchor(pivot: "face", tuck: 0.1)),
                glyphSize: 18,
                glyphFrames: ["g": [PathShape([.move(Point(2, 2)), .line(Point(16, 16))], stroke: .ink, strokeWidth: 1.5)]])
        }()
    }
#endif
