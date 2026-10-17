#if os(macOS)
    import CoreGraphics

    /// The spacing scale: one 4 pt grid where each step has a job, the same numbers as the Windows apps. Pick the step
    /// by the relationship, not by how it looks in one place.
    public enum Space {
        /// 4: a number and its unit, a glyph and its word, a title and the note under it.
        public static let tight: CGFloat = 4
        /// 8: a heading and its text, a label and its control, the lines of one list.
        public static let snug: CGFloat = 8
        /// 12: siblings in a group: rows, chips, buttons in a row.
        public static let item: CGFloat = 12
        /// 16: a card's inner margin, and between the groups inside one section; the panel's margin.
        public static let group: CGFloat = 16
        /// 24: between a window's sections.
        public static let section: CGFloat = 24
        /// 32: the window margin.
        public static let region: CGFloat = 32
    }
#endif
