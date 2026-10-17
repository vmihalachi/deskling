#if os(macOS)
    import Foundation
    import LachingCompanion
    import LachingCore
    import LachingSystem

    /// Builds the companion's `Snapshot` from Laching's permission-free system signals, and raises the events the shell
    /// notices on its own (battery, charger, appearance). While a line is spoken or the character stands on the desktop it
    /// polls the busy state every second, so speech stops the instant a call starts and the character steps off the
    /// desktop right away. Reads device state only; never prompts. Needs the main actor.
    @MainActor
    public final class SignalHub {
        /// The battery level a "battery low" comment fires below, as it drops past it on battery.
        public let lowBatteryPercent: Int

        /// Whether a line is being spoken right now, beats included (the app's `SpeechSequencer.isActive`).
        public var isSpeaking: () -> Bool = { false }
        /// The last known notification permission, refreshed by the app.
        public var notificationsAuthorized = false
        /// Something happened that the character may comment on.
        public var onEvent: ((Trigger) -> Void)?
        /// A call started while a line was being spoken.
        public var onCallStarted: (() -> Void)?
        /// Every poll of the busy watch: the busy reasons, and whether they changed since the last poll (a call or full
        /// screen began or ended).
        public var onBusyPoll: ((Set<BusyReason>, Bool) -> Void)?

        private let idle: IdleTimeProvider
        private let busy: BusyStateProvider
        private let audio: AudioOutputStateProvider?
        private let screenLock: ScreenLockWatcher?
        private let power: SystemPowerSource?
        private let appearance: AppearanceWatcher?
        private var lastPower: PowerState?
        private var busyWatch: Timer?
        private var watchingSpeech = false
        private var watchingDesktop = false
        private var lastBusy: Set<BusyReason> = []

        public init(
            idle: IdleTimeProvider, busy: BusyStateProvider, audio: AudioOutputStateProvider?, screenLock: ScreenLockWatcher?,
            power: SystemPowerSource?, appearance: AppearanceWatcher?, lowBatteryPercent: Int = 20
        ) {
            self.lowBatteryPercent = lowBatteryPercent
            self.idle = idle
            self.busy = busy
            self.audio = audio
            self.screenLock = screenLock
            self.power = power
            self.appearance = appearance
            lastPower = power?.currentPowerState()
            power?.onChange = { [weak self] in
                MainActor.assumeIsolated { self?.powerChanged() }
            }
            appearance?.onChange = { [weak self] next in
                MainActor.assumeIsolated { self?.onEvent?(next == .dark ? .wentDark : .wentLight) }
            }
        }

        public func snapshot() -> Snapshot {
            Snapshot(
                busy: busy.currentBusyReasons(), idleSeconds: idle.secondsSinceLastInput(),
                isScreenLocked: screenLock?.isLocked ?? false, isOutputMuted: audio?.isOutputMuted(), isSpeaking: isSpeaking(),
                notificationsAuthorized: notificationsAuthorized)
        }

        // MARK: Busy watch

        public func speechStarted() {
            watchingSpeech = true
            updateBusyWatch()
        }

        public func speechEnded() {
            watchingSpeech = false
            updateBusyWatch()
        }

        /// The character stands on the desktop (or waits to come back after a call): watch for calls and full screen.
        public func watchForDesktop(_ on: Bool) {
            guard on != watchingDesktop else { return }
            watchingDesktop = on
            updateBusyWatch()
        }

        public var isWatchingBusy: Bool { busyWatch != nil }

        private func updateBusyWatch() {
            if watchingSpeech || watchingDesktop {
                guard busyWatch == nil else { return }
                lastBusy = []
                let timer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { [weak self] _ in
                    MainActor.assumeIsolated { self?.pollBusy() }
                }
                timer.tolerance = 0.25
                busyWatch = timer
            } else {
                busyWatch?.invalidate()
                busyWatch = nil
            }
        }

        /// One poll of the 1 s watch: a call that just started cuts the line, and the busy reasons are reported.
        public func pollBusy() {
            let reasons = busy.currentBusyReasons()
            if watchingSpeech, reasons.contains(.call) {
                speechEnded()
                onCallStarted?()
            }
            let changed = reasons != lastBusy
            lastBusy = reasons
            onBusyPoll?(reasons, changed)
        }

        // MARK: Power

        private func powerChanged() {
            guard let state = power?.currentPowerState() else { return }
            let previous = lastPower
            lastPower = state
            guard let previous else { return }
            if state.isOnBattery != previous.isOnBattery {
                onEvent?(state.isOnBattery ? .unplugged : .charging)
            }
            if state.isOnBattery, let percent = state.percent, let before = previous.percent,
                before >= lowBatteryPercent, percent < lowBatteryPercent
            {
                onEvent?(.batteryLow)
            }
        }
    }
#endif
