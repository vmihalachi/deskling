#if os(macOS)
    import Foundation
    import IOKit.ps

    /// The machine's power situation at one moment.
    public struct PowerState: Equatable, Sendable {
        /// Running on the battery (unplugged).
        public var isOnBattery: Bool
        /// Plugged in and the battery is charging (false when full or on battery).
        public var isCharging: Bool
        /// Battery level 0...100, when the power source reports one.
        public var percent: Int?

        public init(isOnBattery: Bool, isCharging: Bool, percent: Int?) {
            self.isOnBattery = isOnBattery
            self.isCharging = isCharging
            self.percent = percent
        }
    }

    /// Reports the power source. `nil` means there is none to report, as on a desktop without a battery.
    public protocol PowerStateProvider {
        func currentPowerState() -> PowerState?
    }

    /// The real power source, from IOKit's power-source list. Reads public keys only: no entitlement, no
    /// prompt. Returns `nil` on a Mac with no battery (and no UPS). Set `onChange` to hear about changes;
    /// the callback runs on the main run loop.
    public final class SystemPowerSource: PowerStateProvider {
        /// Called on the main run loop whenever a power source changes (plugged in, level, charging).
        public var onChange: (() -> Void)? {
            didSet { if onChange != nil { installNotificationIfNeeded() } }
        }
        private var source: CFRunLoopSource?

        public init() {}

        deinit {
            if let source {
                CFRunLoopRemoveSource(CFRunLoopGetMain(), source, .defaultMode)
                CFRunLoopSourceInvalidate(source)
            }
        }

        public func currentPowerState() -> PowerState? {
            guard let info = IOPSCopyPowerSourcesInfo()?.takeRetainedValue(),
                let list = IOPSCopyPowerSourcesList(info)?.takeRetainedValue()
            else { return nil }
            let descriptions = (list as NSArray).compactMap { source -> NSDictionary? in
                guard let description = IOPSGetPowerSourceDescription(info, source as CFTypeRef) else { return nil }
                return description.takeUnretainedValue() as NSDictionary
            }
            // The internal battery first; a UPS only when there is no battery.
            let battery = descriptions.first { $0[kIOPSTypeKey] as? String == kIOPSInternalBatteryType }
            guard let description = battery ?? descriptions.first else { return nil }
            let state = description[kIOPSPowerSourceStateKey] as? String
            let currentCapacity = description[kIOPSCurrentCapacityKey] as? Int
            let maxCapacity = description[kIOPSMaxCapacityKey] as? Int
            var percent: Int?
            if let currentCapacity, let maxCapacity, maxCapacity > 0 {
                percent = min(100, max(0, currentCapacity * 100 / maxCapacity))
            }
            return PowerState(
                isOnBattery: state == kIOPSBatteryPowerValue,
                isCharging: description[kIOPSIsChargingKey] as? Bool ?? false,
                percent: percent)
        }

        private func installNotificationIfNeeded() {
            guard source == nil else { return }
            let callback: IOPowerSourceCallbackType = { context in
                guard let context else { return }
                Unmanaged<SystemPowerSource>.fromOpaque(context).takeUnretainedValue().onChange?()
            }
            let context = Unmanaged.passUnretained(self).toOpaque()
            guard let created = IOPSNotificationCreateRunLoopSource(callback, context)?.takeRetainedValue() else { return }
            CFRunLoopAddSource(CFRunLoopGetMain(), created, .defaultMode)
            source = created
        }
    }
#endif
