#if os(macOS)
    import AudioToolbox
    import CoreAudio
    import Foundation

    /// Where the default output device sends sound.
    public enum OutputRouteKind: Equatable, Sendable {
        case builtInSpeakers
        /// The built-in jack with headphones plugged in.
        case headphones
        case bluetooth
        case usb
        /// HDMI, DisplayPort, AirPlay, aggregate and virtual devices, or no output device at all.
        case other
    }

    /// Reports the state of the default audio output. Implementations must never play or capture sound.
    public protocol AudioOutputStateProvider {
        /// Muted, or `nil` when there is no output device or it doesn't say.
        func isOutputMuted() -> Bool?
        func outputRoute() -> OutputRouteKind
    }

    /// The real default output device, through CoreAudio's property API. Reads device properties only: no
    /// audio unit is opened, nothing plays, and no permission is involved (output needs none).
    public final class SystemAudioOutput: AudioOutputStateProvider {
        public init() {}

        /// The device's mute switch; when it has none (AirPlay, some USB devices), the virtual main volume
        /// at zero counts as muted.
        public func isOutputMuted() -> Bool? {
            guard let device = Self.defaultOutputDevice() else { return nil }
            var mute = AudioObjectPropertyAddress(
                mSelector: kAudioDevicePropertyMute,
                mScope: kAudioObjectPropertyScopeOutput,
                mElement: kAudioObjectPropertyElementMain)
            if AudioObjectHasProperty(device, &mute) {
                let value: UInt32? = Self.property(device, kAudioDevicePropertyMute, scope: kAudioObjectPropertyScopeOutput)
                if let value { return value != 0 }
            }
            var volume = AudioObjectPropertyAddress(
                mSelector: kAudioHardwareServiceDeviceProperty_VirtualMainVolume,
                mScope: kAudioObjectPropertyScopeOutput,
                mElement: kAudioObjectPropertyElementMain)
            var level: Float32 = 0
            var size = UInt32(MemoryLayout<Float32>.size)
            guard AudioObjectGetPropertyData(device, &volume, 0, nil, &size, &level) == noErr else { return nil }
            return level == 0
        }

        public func outputRoute() -> OutputRouteKind {
            guard let device = Self.defaultOutputDevice(),
                let transport: UInt32 = Self.property(device, kAudioDevicePropertyTransportType, scope: kAudioObjectPropertyScopeGlobal)
            else { return .other }
            switch transport {
            case kAudioDeviceTransportTypeBuiltIn:
                let source: UInt32? = Self.property(device, kAudioDevicePropertyDataSource, scope: kAudioObjectPropertyScopeOutput)
                return source == Self.headphonesDataSource ? .headphones : .builtInSpeakers
            case kAudioDeviceTransportTypeBluetooth, kAudioDeviceTransportTypeBluetoothLE:
                return .bluetooth
            case kAudioDeviceTransportTypeUSB:
                return .usb
            default:
                return .other
            }
        }

        /// `'hdpn'`: the built-in device's data source while headphones are plugged in.
        private static let headphonesDataSource: UInt32 = 0x6864_706E

        private static func defaultOutputDevice() -> AudioObjectID? {
            let device: AudioObjectID? = property(
                AudioObjectID(kAudioObjectSystemObject), kAudioHardwarePropertyDefaultOutputDevice,
                scope: kAudioObjectPropertyScopeGlobal)
            guard let device, device != kAudioObjectUnknown else { return nil }
            return device
        }

        private static func property<T: FixedWidthInteger>(
            _ object: AudioObjectID,
            _ selector: AudioObjectPropertySelector,
            scope: AudioObjectPropertyScope
        ) -> T? {
            var address = AudioObjectPropertyAddress(
                mSelector: selector,
                mScope: scope,
                mElement: kAudioObjectPropertyElementMain)
            var value: T = 0
            var size = UInt32(MemoryLayout<T>.size)
            guard AudioObjectGetPropertyData(object, &address, 0, nil, &size, &value) == noErr else { return nil }
            return value
        }
    }
#endif
