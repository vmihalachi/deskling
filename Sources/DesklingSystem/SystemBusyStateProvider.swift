#if os(macOS)
    import AVFoundation
    import AppKit
    import CoreAudio
    import CoreGraphics
    import CoreMediaIO
    import DesklingCore
    import Foundation

    /// Detects calls (camera or microphone in use by another app) and full-screen apps.
    ///
    /// Only reads device and window state: it never opens a device, so it can't trigger a camera or
    /// microphone permission prompt, and window bounds and owners need no Screen Recording permission.
    ///
    /// `cameraCheck` picks how the camera is inspected. `.avFoundation` asks `AVCaptureDevice` discovery,
    /// which lists the cameras an app may capture from: a sandboxed app without the camera entitlement
    /// (and `NSCameraUsageDescription`) may see no devices at all there, so its calls would go unnoticed.
    /// `.coreMediaIO` reads every CoreMediaIO device's "running somewhere" flag instead, which needs no
    /// entitlement. `.off` skips the camera and relies on the microphone, which every call uses anyway.
    public struct SystemBusyStateProvider: BusyStateProvider, Sendable {
        /// How to find out whether another app is using the camera.
        public enum CameraCheck: Equatable, Sendable {
            /// `AVCaptureDevice.DiscoverySession` and `isInUseByAnotherApplication`. Needs the camera entitlement in
            /// a sandboxed app, or discovery may come back empty.
            case avFoundation
            /// `kCMIODevicePropertyDeviceIsRunningSomewhere` on each CoreMediaIO device. No entitlement needed.
            case coreMediaIO
            /// Don't look at the camera; the microphone check still runs.
            case off
        }

        public var cameraCheck: CameraCheck

        public init(cameraCheck: CameraCheck = .avFoundation) {
            self.cameraCheck = cameraCheck
        }

        public func currentBusyReasons() -> Set<BusyReason> {
            var reasons: Set<BusyReason> = []
            if Self.isCameraInUse(using: cameraCheck) || Self.isMicrophoneInUse() { reasons.insert(.call) }
            if Self.isFrontmostAppFullScreen() { reasons.insert(.fullScreen) }
            return reasons
        }

        // MARK: Camera

        public static func isCameraInUse(using check: CameraCheck) -> Bool {
            switch check {
            case .avFoundation: return isCameraInUseByAVFoundation()
            case .coreMediaIO: return isCameraInUseByCoreMediaIO()
            case .off: return false
            }
        }

        static func isCameraInUseByAVFoundation() -> Bool {
            let discovery = AVCaptureDevice.DiscoverySession(
                deviceTypes: [.builtInWideAngleCamera, .external, .continuityCamera],
                mediaType: .video, position: .unspecified)
            return discovery.devices.contains { $0.isInUseByAnotherApplication }
        }

        /// Any CoreMediaIO device with IO running in some process. Includes this one, like
        /// `isInUseByAnotherApplication` would not; apps that capture themselves should use `.off` meanwhile.
        static func isCameraInUseByCoreMediaIO() -> Bool {
            var address = CMIOObjectPropertyAddress(
                mSelector: CMIOObjectPropertySelector(kCMIOHardwarePropertyDevices),
                mScope: CMIOObjectPropertyScope(kCMIOObjectPropertyScopeGlobal),
                mElement: CMIOObjectPropertyElement(kCMIOObjectPropertyElementMain))
            let system = CMIOObjectID(kCMIOObjectSystemObject)
            var size: UInt32 = 0
            guard CMIOObjectGetPropertyDataSize(system, &address, 0, nil, &size) == noErr, size > 0 else { return false }
            var devices = [CMIOObjectID](repeating: 0, count: Int(size) / MemoryLayout<CMIOObjectID>.size)
            var used: UInt32 = 0
            guard CMIOObjectGetPropertyData(system, &address, 0, nil, size, &used, &devices) == noErr else { return false }
            var running = CMIOObjectPropertyAddress(
                mSelector: CMIOObjectPropertySelector(kCMIODevicePropertyDeviceIsRunningSomewhere),
                mScope: CMIOObjectPropertyScope(kCMIOObjectPropertyScopeGlobal),
                mElement: CMIOObjectPropertyElement(kCMIOObjectPropertyElementMain))
            return devices.prefix(Int(used) / MemoryLayout<CMIOObjectID>.size).contains { device in
                var value: UInt32 = 0
                var valueUsed: UInt32 = 0
                let status = CMIOObjectGetPropertyData(
                    device, &running, 0, nil, UInt32(MemoryLayout<UInt32>.size), &valueUsed, &value)
                return status == noErr && value != 0
            }
        }

        // MARK: Microphone

        public static func isMicrophoneInUse() -> Bool {
            processIsRunningInput() ?? inputDeviceIsRunning()
        }

        /// macOS 14+: asks each audio client whether it's recording, so music
        /// playing through a headset doesn't count as a call. `nil` if unsupported.
        private static func processIsRunningInput() -> Bool? {
            guard let processes = objectList(kAudioHardwarePropertyProcessObjectList) else { return nil }
            let me = getpid()
            var answered = false
            for process in processes {
                guard let running: UInt32 = property(process, kAudioProcessPropertyIsRunningInput) else { continue }
                answered = true
                let pid: pid_t? = property(process, kAudioProcessPropertyPID)
                if running != 0, pid != me { return true }
            }
            return (answered || processes.isEmpty) ? false : nil
        }

        /// Fallback: any input-capable device with IO running in some process.
        private static func inputDeviceIsRunning() -> Bool {
            guard let devices = objectList(kAudioHardwarePropertyDevices) else { return false }
            return devices.contains { device in
                hasInputStreams(device)
                    && (property(device, kAudioDevicePropertyDeviceIsRunningSomewhere) as UInt32? ?? 0) != 0
            }
        }

        private static func hasInputStreams(_ device: AudioObjectID) -> Bool {
            var address = AudioObjectPropertyAddress(
                mSelector: kAudioDevicePropertyStreams,
                mScope: kAudioObjectPropertyScopeInput,
                mElement: kAudioObjectPropertyElementMain)
            var size: UInt32 = 0
            return AudioObjectGetPropertyDataSize(device, &address, 0, nil, &size) == noErr && size > 0
        }

        private static func objectList(_ selector: AudioObjectPropertySelector) -> [AudioObjectID]? {
            var address = AudioObjectPropertyAddress(
                mSelector: selector,
                mScope: kAudioObjectPropertyScopeGlobal,
                mElement: kAudioObjectPropertyElementMain)
            let system = AudioObjectID(kAudioObjectSystemObject)
            var size: UInt32 = 0
            guard AudioObjectGetPropertyDataSize(system, &address, 0, nil, &size) == noErr else { return nil }
            var ids = [AudioObjectID](repeating: 0, count: Int(size) / MemoryLayout<AudioObjectID>.size)
            guard AudioObjectGetPropertyData(system, &address, 0, nil, &size, &ids) == noErr else { return nil }
            return Array(ids.prefix(Int(size) / MemoryLayout<AudioObjectID>.size))
        }

        private static func property<T: FixedWidthInteger>(
            _ object: AudioObjectID,
            _ selector: AudioObjectPropertySelector
        ) -> T? {
            var address = AudioObjectPropertyAddress(
                mSelector: selector,
                mScope: kAudioObjectPropertyScopeGlobal,
                mElement: kAudioObjectPropertyElementMain)
            var value: T = 0
            var size = UInt32(MemoryLayout<T>.size)
            guard AudioObjectGetPropertyData(object, &address, 0, nil, &size, &value) == noErr else { return nil }
            return value
        }

        // MARK: Full screen

        /// True when a normal-level window of the frontmost app covers a whole
        /// display, menu bar included. Zoomed windows leave the menu bar visible, so
        /// they don't count. Window bounds and owners need no Screen Recording permission.
        public static func isFrontmostAppFullScreen() -> Bool {
            guard let app = NSWorkspace.shared.frontmostApplication,
                app.processIdentifier != getpid(),
                let windows = CGWindowListCopyWindowInfo(
                    [.optionOnScreenOnly, .excludeDesktopElements],
                    kCGNullWindowID) as? [[String: Any]]
            else { return false }
            let screens = NSScreen.screens.map(\.frame.size)
            return windows.contains { info in
                guard (info[kCGWindowOwnerPID as String] as? pid_t) == app.processIdentifier,
                    (info[kCGWindowLayer as String] as? Int) == 0,
                    let boundsDict = info[kCGWindowBounds as String] as? NSDictionary,
                    let bounds = CGRect(dictionaryRepresentation: boundsDict)
                else { return false }
                return screens.contains { $0 == bounds.size }
            }
        }
    }
#endif
