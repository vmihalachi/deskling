#if os(macOS)
    import Foundation
    import ServiceManagement

    /// The app's own "open at login" registration through `SMAppService.mainApp` (macOS 13+). Registering
    /// shows nothing in the sandbox, but macOS may ask the user to allow it in System Settings → Login
    /// Items; `status` says when that happened (`.requiresApproval`). Never touches other apps' items.
    public enum LoginItem {
        public static var isEnabled: Bool { SMAppService.mainApp.status == .enabled }

        public static var status: SMAppService.Status { SMAppService.mainApp.status }

        public static func set(_ enabled: Bool) throws {
            if enabled { try SMAppService.mainApp.register() } else { try SMAppService.mainApp.unregister() }
        }
    }
#endif
