#if os(macOS)
    import SwiftUI

    /// Where content sits in every window, so titles, text and buttons line up from one window to the next.
    public enum WindowInset {
        /// The side margin of every window's content and its action bar.
        public static let side = Space.region
        /// From the bottom of the title bar to the top of the window title.
        public static let top = Space.section
        /// Under the last thing in a window.
        public static let bottom = Space.region
        /// The panel hangs under the menu bar, so it is tighter.
        public static let panel = Space.group
    }

    extension View {
        /// The shared window frame: 32 pt at the sides, 24 pt under the title bar, `bottom` under the content.
        public func windowPadding(bottom: CGFloat = WindowInset.bottom) -> some View {
            padding(.horizontal, WindowInset.side)
                .padding(.top, WindowInset.top)
                .padding(.bottom, bottom)
        }
    }
#endif
