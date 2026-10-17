#if os(macOS)
    import AppKit
    import LachingCompanion
    import SwiftUI

    /// A character drawn from its compiled art on a `Canvas`: the pose (sway, blink, the talking mouth and the pulse)
    /// comes from the mood and the seconds since the view appeared, and everything holds the still pose when `animated`
    /// is false, Reduce Motion is on or a `ScreenshotCapture` run is going (so shots are the same every run). Waking from Shh springs from the drooped pose back over 0.6 s. Vector parts are
    /// filled and stroked with the `companionPalette`'s paints; image parts are drawn from `artBundle`'s assets
    /// (`<skin>.<part>`) under the rig's transform. While talking, `mouthLevel` (read every frame; the app model's
    /// `mouthLevel`) moves the mouth with a recorded clip's loudness; nil keeps the mood's timer. VoiceOver reads
    /// `line`, else `accessibilityLabel` (the app's words).
    public struct CharacterView: View {
        public let character: RiggedCharacter
        public var mood: Mood
        public var talking: Bool
        /// The latest line, read by VoiceOver.
        public var line: String?
        public var animated: Bool
        public var artBundle: Bundle
        /// What VoiceOver says when there's no line, such as "Sunny, asleep".
        public var accessibilityLabel: String?
        /// The voice's mouth level (0–3) right now, or nil for the mood's timer.
        public var mouthLevel: (@MainActor () -> Int?)?

        @Environment(\.accessibilityReduceMotion) private var reduceMotion
        @Environment(\.companionPalette) private var palette
        @State private var start = Date()
        /// When the character last woke from Shh, for the spring back up.
        @State private var wokeAt: Date?

        /// How long the spring back from Shh takes.
        public nonisolated static let wakeDuration = 0.6

        public init(
            character: RiggedCharacter, mood: Mood, talking: Bool, line: String?, animated: Bool = true, artBundle: Bundle = .main,
            accessibilityLabel: String? = nil, mouthLevel: (@MainActor () -> Int?)? = nil
        ) {
            self.character = character
            self.mood = mood
            self.talking = talking
            self.line = line
            self.animated = animated
            self.artBundle = artBundle
            self.accessibilityLabel = accessibilityLabel
            self.mouthLevel = mouthLevel
        }

        private var paused: Bool { reduceMotion || !animated || ScreenshotCapture.isEnabled }

        public var body: some View {
            TimelineView(.animation(minimumInterval: 1.0 / 30.0, paused: paused)) { timeline in
                let level = talking && !paused ? mouthLevel?() : nil
                Canvas { context, size in
                    var pose =
                        paused
                        ? character.stillPose(mood: shownMood)
                        : character.pose(mood: shownMood, time: timeline.date.timeIntervalSince(start), mouthLevel: level)
                    if let wokeAt, !paused, timeline.date.timeIntervalSince(wokeAt) < Self.wakeDuration {
                        let drooped = character.stillPose(mood: .shushed)
                        pose = Self.pose(waking: drooped, to: pose, elapsed: timeline.date.timeIntervalSince(wokeAt))
                    }
                    Self.draw(
                        character.layers(for: pose), viewBox: character.viewBox, in: &context, size: size, palette: palette,
                        artBundle: artBundle)
                }
            }
            .onChange(of: shownMood) { old, new in
                if old == .shushed && new != .shushed && !paused { wokeAt = Date() }
            }
            .accessibilityElement(children: .ignore)
            .accessibilityLabel(Text(verbatim: line ?? accessibilityLabel ?? ""))
        }

        private var shownMood: Mood { talking ? .talking : mood }

        /// The spring back from Shh: critically damped (no overshoot), 0 at the start, settled at `wakeDuration`.
        public nonisolated static func wakeProgress(elapsed: Double) -> Double {
            guard elapsed > 0 else { return 0 }
            guard elapsed < wakeDuration else { return 1 }
            let omega = 10 / wakeDuration
            return 1 - (1 + omega * elapsed) * exp(-omega * elapsed)
        }

        /// The pose `elapsed` seconds into waking: the drooped pose blending into the live one.
        public nonisolated static func pose(waking drooped: Pose, to target: Pose, elapsed: Double) -> Pose {
            drooped.blended(to: target, progress: wakeProgress(elapsed: elapsed))
        }

        /// Draws placed parts centered in `size`, the view box scaled to fit: vector parts through their transform,
        /// image parts as the asset in their frame under it.
        public static func draw(
            _ layers: [PlacedPart], viewBox: LachingCompanion.Size, in context: inout GraphicsContext, size: CGSize,
            palette: CompanionPalette, artBundle: Bundle
        ) {
            let scale = min(size.width / viewBox.width, size.height / viewBox.height)
            let dx = (size.width - viewBox.width * scale) / 2
            let dy = (size.height - viewBox.height * scale) / 2
            for layer in layers {
                switch layer.content {
                case .shapes(let shapes):
                    for shape in shapes {
                        draw(shape.transformed(by: layer.transform), scale: scale, dx: dx, dy: dy, in: &context, palette: palette)
                    }
                case .image(let asset, let frame):
                    let t = layer.transform.affine
                    context.drawLayer { sub in
                        sub.translateBy(x: dx, y: dy)
                        sub.scaleBy(x: scale, y: scale)
                        sub.concatenate(CGAffineTransform(a: t.a, b: t.b, c: t.c, d: t.d, tx: t.tx, ty: t.ty))
                        let rect = CGRect(x: frame.origin.x, y: frame.origin.y, width: frame.size.width, height: frame.size.height)
                        if let image = image(asset, in: artBundle) { sub.draw(Image(nsImage: image).resizable(), in: rect) }
                    }
                }
            }
        }

        /// Image parts by bundle and name: `Bundle.image(forResource:)` finds both asset catalog images and loose files
        /// (SwiftUI's `Image(_:bundle:)` draws nothing in a `Canvas` for a name with a dot). Thread-safe.
        private nonisolated(unsafe) static let images = NSCache<NSString, NSImage>()

        private static func image(_ asset: String, in bundle: Bundle) -> NSImage? {
            let key = "\(bundle.bundlePath)/\(asset)" as NSString
            if let cached = images.object(forKey: key) { return cached }
            guard let image = bundle.image(forResource: asset) else { return nil }
            images.setObject(image, forKey: key)
            return image
        }

        private static func draw(
            _ shape: PathShape, scale: CGFloat, dx: CGFloat, dy: CGFloat, in context: inout GraphicsContext,
            palette: CompanionPalette
        ) {
            let path = Path { path in
                func p(_ point: LachingCompanion.Point) -> CGPoint { CGPoint(x: dx + point.x * scale, y: dy + point.y * scale) }
                for command in shape.path {
                    switch command {
                    case .move(let point): path.move(to: p(point))
                    case .line(let point): path.addLine(to: p(point))
                    case .cubic(let c1, let c2, let point): path.addCurve(to: p(point), control1: p(c1), control2: p(c2))
                    case .close: path.closeSubpath()
                    }
                }
            }
            var layer = context
            layer.opacity = shape.opacity
            if let fill = shape.fill {
                layer.fill(path, with: .color(palette.paints(fill)), style: FillStyle(eoFill: shape.evenOdd))
            }
            if let stroke = shape.stroke, shape.strokeWidth > 0 {
                let style = StrokeStyle(lineWidth: shape.strokeWidth * scale, lineCap: .round, lineJoin: .round)
                layer.stroke(path, with: .color(palette.paints(stroke)), style: style)
            }
        }
    }
#endif
