#if os(macOS)
    import AppKit
    import DesklingCompanion

    /// Draws a character's menu bar glyph: its glyph frame for a mood (two while talking), a line drawing in a `glyphSize`
    /// square scaled to `size`, made a template image so macOS tints it. Cached per skin, mood, frame and size (there are
    /// only a handful). Needs the main actor.
    @MainActor
    public enum GlyphRenderer {
        private static var cache: [String: NSImage] = [:]

        /// `frame` wraps around the mood's frames; `accessibilityDescription` is the app's, already localized.
        public static func image(
            character: RiggedCharacter, mood: Mood, frame: Int = 0, size: CGFloat = 18, accessibilityDescription: String
        ) -> NSImage {
            let count = max(character.glyphFrameCount(mood: mood), 1)
            let frame = ((frame % count) + count) % count
            let key = "\(character.skin.rawValue)-\(mood.rawValue)-\(frame)-\(size)-\(accessibilityDescription)"
            if let cached = cache[key] { return cached }
            let shapes = character.glyph(mood: mood, frame: frame)
            let scale = size / character.glyphSize
            let image = NSImage(size: NSSize(width: size, height: size), flipped: true) { _ in
                NSColor.black.setStroke()
                NSColor.black.setFill()
                let transform = NSAffineTransform()
                transform.scale(by: scale)
                for shape in shapes {
                    let path = bezierPath(for: shape)
                    path.transform(using: transform as AffineTransform)
                    path.windingRule = shape.evenOdd ? .evenOdd : .nonZero
                    if shape.fill != nil { path.fill() }
                    if shape.stroke != nil, shape.strokeWidth > 0 {
                        path.lineWidth = shape.strokeWidth * scale
                        path.stroke()
                    }
                }
                return true
            }
            image.isTemplate = true
            image.accessibilityDescription = accessibilityDescription
            cache[key] = image
            return image
        }

        /// A path as an AppKit path, with round caps and joins so the tiny strokes stay soft.
        static func bezierPath(for shape: PathShape) -> NSBezierPath {
            let path = NSBezierPath()
            path.lineCapStyle = .round
            path.lineJoinStyle = .round
            func p(_ point: DesklingCompanion.Point) -> NSPoint { NSPoint(x: point.x, y: point.y) }
            for command in shape.path {
                switch command {
                case .move(let point): path.move(to: p(point))
                case .line(let point): path.line(to: p(point))
                case .cubic(let c1, let c2, let point): path.curve(to: p(point), controlPoint1: p(c1), controlPoint2: p(c2))
                case .close: path.close()
                }
            }
            return path
        }
    }
#endif
