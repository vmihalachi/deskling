#if os(macOS) || os(iOS)
    import CoreText
    import Foundation
    #if os(macOS)
        import AppKit
    #elseif os(iOS)
        import UIKit
    #endif

    /// Registers the fonts an app bundles, for the process only (no install, no prompt). macOS usually loads them
    /// already through `ATSApplicationFontsPath`; registering again covers a missing folder reference, and an "already
    /// registered" error is ignored. Thread-safe; each bundle and folder is registered once.
    public enum FontRegistration {
        private static let lock = NSLock()
        nonisolated(unsafe) private static var registered: Set<String> = []

        /// Every `.ttf` in `bundle`'s `subdirectory` (`Fonts` by default) and at its resources' top level.
        public static func register(bundle: Bundle = .main, subdirectory: String? = "Fonts") {
            let key = "\(bundle.bundlePath)|\(subdirectory ?? "")"
            lock.lock()
            defer { lock.unlock() }
            guard registered.insert(key).inserted else { return }
            var urls = subdirectory.flatMap { bundle.urls(forResourcesWithExtension: "ttf", subdirectory: $0) } ?? []
            urls += bundle.urls(forResourcesWithExtension: "ttf", subdirectory: nil) ?? []
            for url in urls {
                CTFontManagerRegisterFontsForURL(url as CFURL, .process, nil)
            }
        }

        /// Whether every font in `names` (PostScript names) can be made at `size`.
        public static func isAvailable(_ names: [String], size: CGFloat = 12) -> Bool {
            #if os(macOS)
                names.allSatisfy { NSFont(name: $0, size: size) != nil }
            #elseif os(iOS)
                names.allSatisfy { UIFont(name: $0, size: size) != nil }
            #endif
        }
    }
#endif
