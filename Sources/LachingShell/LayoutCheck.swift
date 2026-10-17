#if os(macOS)
    import AppKit
    import SwiftUI

    /// Finds SwiftUI content that is taller than the window it's in. A window that doesn't scroll squeezes such
    /// content: the padding goes first, then buttons and text are cut off at the bottom, often only in the longer
    /// languages. Measures the content's smallest comfortable height at the window's width (text wrapped, spacers at
    /// their minimum, every view at its ideal height), the way a screenshot mode can check each window it renders
    /// instead of someone spotting it in a shot. Windows with a scroll view are skipped: their content is meant to be
    /// taller. Text cut off inside its own box isn't measured (SwiftUI doesn't expose its text layout). Draws nothing
    /// on screen and needs the main actor.
    @MainActor
    public enum LayoutCheck {
        /// The overflow of `window`'s SwiftUI content (an `NSHostingController` or `NSHostingView`) in the room below
        /// its title bar, or nil when it fits, scrolls or isn't SwiftUI.
        public static func overflow(in window: NSWindow) -> Overflow? {
            let host = window.contentViewController?.view ?? window.contentView
            guard let host, let root = (window.contentViewController as? HostedRoot ?? host as? HostedRoot)?.hostedRoot,
                !scrolls(host)
            else { return nil }
            return overflow(of: root, in: window.contentLayoutRect.size)
        }

        /// The overflow of `content` in a window whose room below the title bar is `size`, or nil when it fits.
        /// For a view that isn't in a window yet (a later onboarding page); it can't tell whether the view scrolls.
        public static func overflow(of content: some View, in size: CGSize) -> Overflow? {
            let needed = neededHeight(of: content, width: size.width)
            return needed > size.height + slack ? Overflow(needed: needed, available: size.height) : nil
        }

        /// The height `content` needs at `width`: its ideal height, with text wrapped to the width.
        public static func neededHeight(of content: some View, width: CGFloat) -> CGFloat {
            NSHostingView(rootView: content.fixedSize(horizontal: false, vertical: true).frame(width: width)).fittingSize.height
        }

        /// The room a titled window built the way `WindowManager` builds one gives its content below the title bar,
        /// for a window of content size `size`.
        public static func contentRoom(forWindowOfSize size: CGSize) -> CGSize {
            let window = NSWindow(
                contentRect: NSRect(origin: .zero, size: size),
                styleMask: [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView], backing: .buffered,
                defer: true)
            window.isReleasedWhenClosed = false
            return window.contentLayoutRect.size
        }

        /// Whether `view` or one of its subviews scrolls (SwiftUI's `ScrollView`, `List` and `TextEditor` are
        /// `NSScrollView`s underneath).
        public static func scrolls(_ view: NSView) -> Bool {
            view is NSScrollView || view.subviews.contains(where: scrolls)
        }
    }

    /// SwiftUI hosting whose root view can be measured on its own, whatever its type.
    @MainActor
    private protocol HostedRoot {
        var hostedRoot: AnyView { get }
    }

    extension NSHostingController: HostedRoot {
        fileprivate var hostedRoot: AnyView { AnyView(rootView) }
    }

    extension NSHostingView: HostedRoot {
        fileprivate var hostedRoot: AnyView { AnyView(rootView) }
    }
#endif
