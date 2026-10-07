#if os(macOS)
    import AppKit
    import Foundation

    /// How the app was started. Reads the launch Apple Event, which is only there before launching
    /// finishes: call `launchedAsLoginItem()` from `applicationWillFinishLaunching` and keep the answer.
    /// Reads only; never registers or changes anything.
    public enum LaunchContext {
        /// Whether macOS started the app as a login item (`kAEOpenApplication` with
        /// `keyAELaunchedAsLogInItem`), as opposed to the user opening it.
        public static func launchedAsLoginItem() -> Bool {
            let event = NSAppleEventManager.shared().currentAppleEvent
            return event?.eventID == kAEOpenApplication
                && event?.paramDescriptor(forKeyword: keyAEPropData)?.enumCodeValue == keyAELaunchedAsLogInItem
        }
    }
#endif
