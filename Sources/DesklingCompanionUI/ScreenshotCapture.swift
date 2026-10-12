#if os(macOS)
    import AppKit
    import DesklingCore
    import DesklingShell
    import DesklingStore
    import SwiftUI

    /// Renders an app's windows and views to PNGs in-process (no Screen Recording permission), for documentation and
    /// store screenshots: the generic half of a screenshot mode. The app's run script sets `DESKLING_SCREENSHOT_MODE` (a
    /// comma-separated list of shot name prefixes, or `all`) and optionally `DESKLING_SCREENSHOT_DIR` (else a folder in
    /// the temporary directory, which a sandboxed app's script copies out of its container: `finish()` prints its path
    /// last). The app builds its model over a throwaway defaults suite, a fixed clock and the silent fakes below, so the
    /// user's settings, speakers and the App Store are never touched, then shoots what it wants. Needs the main actor.
    ///
    /// Every shot is also checked: a window that couldn't be drawn, a PNG that wasn't written, or SwiftUI content taller
    /// than its window (`LayoutCheck`) is a problem. `finish()` writes them to `problems.txt` in the output folder, which
    /// `tools/screenshots.sh` prints and fails on; `report(_:)` adds the app's own.
    @MainActor
    public enum ScreenshotCapture {
        public static let modeVariable = "DESKLING_SCREENSHOT_MODE"
        public static let directoryVariable = "DESKLING_SCREENSHOT_DIR"

        public static var isEnabled: Bool { ProcessInfo.processInfo.environment[modeVariable] != nil }

        /// The shot name prefixes asked for; empty means all.
        public static var wanted: [String] {
            (ProcessInfo.processInfo.environment[modeVariable] ?? "")
                .split(separator: ",").map { $0.trimmingCharacters(in: .whitespaces) }.filter { !$0.isEmpty && $0 != "all" }
        }

        /// Whether `shot` was asked for (by a prefix of its name).
        public static func wants(_ shot: String) -> Bool {
            let wanted = wanted
            return wanted.isEmpty || wanted.contains { shot.hasPrefix($0) }
        }

        public static var outputDirectory: URL {
            if let dir = ProcessInfo.processInfo.environment[directoryVariable], !dir.isEmpty {
                return URL(fileURLWithPath: dir, isDirectory: true)
            }
            return FileManager.default.temporaryDirectory.appendingPathComponent("screenshots", isDirectory: true)
        }

        /// The file `finish()` writes the run's problems to, next to the shots; absent when there were none.
        public static let problemsFile = "problems.txt"

        /// What the run found wrong so far, one line each, starting with the shot's file name.
        public private(set) static var problems: [String] = []

        /// Records a problem (once), for example a window the app expected to open and didn't, and prints it to
        /// standard error as it happens.
        public static func report(_ problem: String) {
            guard !problems.contains(problem) else { return }
            problems.append(problem)
            FileHandle.standardError.write(Data("\(problem)\n".utf8))
        }

        /// Empties and creates `outputDirectory`.
        public static func prepareOutputDirectory() {
            try? FileManager.default.removeItem(at: outputDirectory)
            try? FileManager.default.createDirectory(at: outputDirectory, withIntermediateDirectories: true)
        }

        /// A `UserDefaults` suite of its own, so nothing a shot changes reaches the user's real defaults.
        public static func throwawayDefaults(prefix: String) -> UserDefaults {
            UserDefaults(suiteName: "\(prefix).screenshots.\(UUID().uuidString)")!
        }

        /// The next moment at `hour`:`minute` local time on `weekday` (1 = Sunday … 7 = Saturday), such as a Tuesday at
        /// 10:30, inside the default active hours.
        public static func sampleNow(hour: Int = 10, minute: Int = 30, weekday: Int = 3, calendar: Calendar = .current) -> Date {
            calendar.nextDate(
                after: Date(), matching: DateComponents(hour: hour, minute: minute, weekday: weekday), matchingPolicy: .nextTime)
                ?? Date()
        }

        /// Writes the problems found (`problemsFile`), prints the output folder (the run script reads the last line) and
        /// quits.
        public static func finish() {
            if !problems.isEmpty {
                try? problems.joined(separator: "\n").appending("\n").write(
                    to: outputDirectory.appendingPathComponent(problemsFile), atomically: true, encoding: .utf8)
            }
            print(outputDirectory.path)
            NSApp.terminate(nil)
        }

        // MARK: Capture

        /// Draws the whole window (title bar included) once it is key, so it looks as it does in use, then checks that
        /// its content fits (`LayoutCheck`).
        public static func capture(_ window: NSWindow, to url: URL) async {
            let shot = url.lastPathComponent
            await makeKey(window, for: shot)
            guard let view = window.contentView?.superview ?? window.contentView,
                let rep = view.bitmapImageRepForCachingDisplay(in: view.bounds)
            else { return report("\(shot): the window couldn't be drawn") }
            window.makeFirstResponder(nil)  // no focus ring in the shot
            view.displayIfNeeded()
            view.cacheDisplay(in: view.bounds, to: rep)
            write(rep, to: url)
            if let overflow = LayoutCheck.overflow(in: window) { report("\(shot): \(overflow)") }
        }

        /// Saves a drawn shot, reporting it when that fails.
        private static func write(_ rep: NSBitmapImageRep, to url: URL) {
            do {
                guard let png = rep.representation(using: .png, properties: [:]) else { throw CocoaError(.fileWriteUnknown) }
                try png.write(to: url)
            } catch {
                report("\(url.lastPathComponent): not written (\(error.localizedDescription))")
            }
        }

        /// `view` in a titled window built the way `WindowManager` builds one, for states a real window can't be told
        /// to open in (a later onboarding page).
        public static func captureWindow(_ view: some View, size: NSSize, background: NSColor, to url: URL) async {
            let window = NSWindow(
                contentRect: NSRect(origin: .zero, size: size),
                styleMask: [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView], backing: .buffered,
                defer: false)
            window.titleVisibility = .hidden
            window.titlebarAppearsTransparent = true
            window.backgroundColor = background
            window.isReleasedWhenClosed = false  // ARC owns it; AppKit's extra release on close crashes
            window.contentViewController = NSHostingController(rootView: view)
            window.setContentSize(size)
            window.center()
            await capture(window, to: url)
            window.close()
        }

        /// `view` on its own (a panel, a sheet of poses) at its fitting size.
        public static func captureFitting(_ view: some View, to url: URL) async {
            let host = NSHostingView(rootView: view)
            host.frame.size = host.fittingSize
            let window = NSWindow(contentRect: host.frame, styleMask: [.borderless], backing: .buffered, defer: false)
            window.isReleasedWhenClosed = false  // ARC owns it; AppKit's extra release on close crashes
            window.contentView = host
            try? await Task.sleep(for: .seconds(0.5))
            if let rep = host.bitmapImageRepForCachingDisplay(in: host.bounds) {
                host.cacheDisplay(in: host.bounds, to: rep)
                write(rep, to: url)
            } else {
                report("\(url.lastPathComponent): the view couldn't be drawn")
            }
            window.close()
        }

        /// Shots show a window key, with colored traffic lights. macOS 14+ may turn `activate()` down while another app
        /// is in front, so this asks again until the window is key, and stops the run rather than save an inactive shot.
        public static func makeKey(_ window: NSWindow, for shot: String) async {
            var tries = 0
            while !(NSApp.isActive && window.isKeyWindow) {
                if tries == 30 {
                    FileHandle.standardError.write(
                        Data("Waiting for \(shot) to come forward. Leave the Mac alone, or click the window.\n".utf8))
                }
                if tries == 300 {
                    FileHandle.standardError.write(
                        Data("\(shot): the window never became key (another app kept the focus), so the run stopped.\n".utf8))
                    exit(1)
                }
                NSApp.activate(ignoringOtherApps: true)
                window.makeKeyAndOrderFront(nil)
                try? await Task.sleep(for: .milliseconds(100))
                tries += 1
                if NSApp.isActive && window.isKeyWindow { try? await Task.sleep(for: .seconds(0.5)) }
            }
        }
    }

    /// A clock stopped at one moment.
    public struct FixedClock: Clock, Sendable {
        public let now: Date

        public init(now: Date) {
            self.now = now
        }
    }

    /// Never idle: the user is always there.
    public struct ConstantIdle: IdleTimeProvider, Sendable {
        public init() {}

        public func secondsSinceLastInput() -> TimeInterval { 0 }
    }

    /// Speaks nothing and finishes at once, so the bubble shows the line with the character awake.
    @MainActor
    public final class SilentSpeaker: Speaking {
        public var isSpeaking = false
        public var onFinished: (() -> Void)?

        public init() {}

        public func speak(_ text: String, clip: URL?, voiceIdentifier: String?, rate: Double, volume: Double) {
            onFinished?()
        }

        public func stop() {}
    }

    /// Plays nothing and completes at once.
    @MainActor
    public final class SilentChime: ChimePlaying {
        public init() {}

        public func play(_ sound: String, volume: Double, completion: (() -> Void)?) {
            completion?()
        }

        public func stop() {}
    }

    /// Fixed prices (such as the app's `Products.storekit`), without asking the App Store. Purchases are cancelled;
    /// `owned` is what the user is entitled to, set by the shot.
    @MainActor
    public final class FixedPriceBackend: StoreBackend {
        public let prices: [String: String]
        public var owned: Set<String> = []

        /// `prices` maps product ids to display prices ("1,99 €").
        public init(prices: [String: String]) {
            self.prices = prices
        }

        public func loadProducts(_ ids: [String]) async throws -> [StoreProduct] {
            ids.compactMap { id in prices[id].map { StoreProduct(id: id, displayPrice: $0) } }
        }

        public func entitledProductIDs() async -> Set<String> { owned }

        public func purchase(_ id: String) async throws -> PurchaseOutcome { .cancelled }

        public func restore() async throws {}

        public func observeTransactions(_ onUpdate: @escaping @MainActor (TransactionUpdate) -> Void) {}
    }
#endif
