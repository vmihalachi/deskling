#if os(macOS)
    import AppKit
    import DesklingCompanion
    import DesklingShell

    extension StatusItemController {
        /// Shows `character`'s glyph for `mood`, stepping the talking frames while `talking` (or while a wave runs).
        public func show(glyph character: RiggedCharacter, mood: Mood, talking: Bool, accessibilityDescription: String) {
            setImage { animating, frame in
                GlyphRenderer.image(
                    character: character, mood: animating ? .talking : mood, frame: frame,
                    accessibilityDescription: accessibilityDescription)
            }
            setAnimating(talking)
        }
    }
#endif
