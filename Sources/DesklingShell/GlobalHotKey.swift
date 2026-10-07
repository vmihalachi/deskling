#if os(macOS)
    import Carbon.HIToolbox
    import DesklingCore
    import Foundation

    /// System-wide hotkeys through Carbon's `RegisterEventHotKey`, which works in the sandbox and needs no
    /// Accessibility permission. Several combos can be registered at once, each under an app-chosen `id`;
    /// `onPress` gets that id, on the main actor. Needs a four-character `signature` unique to the app
    /// (for example `OSType(0x4D42_4B48)`, "MBKH"). Never swallows ordinary typing: a combo without ⌘, ⌃ or ⌥
    /// is refused.
    @MainActor
    public final class GlobalHotKey {
        /// A registered hotkey was pressed; the argument is the `id` it was registered under.
        public var onPress: ((UInt32) -> Void)?
        public let signature: OSType
        private var hotKeys: [UInt32: EventHotKeyRef] = [:]
        private var handler: EventHandlerRef?

        public init(signature: OSType) {
            self.signature = signature
        }

        /// Replaces whatever `id` was registered to. Returns false when the combo is invalid
        /// (`KeyShortcut.isValid`) or another app already owns it; nothing is registered then.
        @discardableResult
        public func register(_ shortcut: KeyShortcut, id: UInt32) -> Bool {
            unregister(id: id)
            guard shortcut.isValid else { return false }
            installHandlerIfNeeded()
            let hotKeyID = EventHotKeyID(signature: signature, id: id)
            var ref: EventHotKeyRef?
            let status = RegisterEventHotKey(
                shortcut.keyCode, shortcut.modifiers, hotKeyID,
                GetApplicationEventTarget(), 0, &ref)
            guard status == noErr, let ref else { return false }
            hotKeys[id] = ref
            return true
        }

        public func unregister(id: UInt32) {
            if let ref = hotKeys.removeValue(forKey: id) { UnregisterEventHotKey(ref) }
        }

        public func unregisterAll() {
            for ref in hotKeys.values { UnregisterEventHotKey(ref) }
            hotKeys = [:]
        }

        public func isRegistered(id: UInt32) -> Bool { hotKeys[id] != nil }

        private func installHandlerIfNeeded() {
            guard handler == nil else { return }
            var spec = EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed))
            let me = Unmanaged.passUnretained(self).toOpaque()
            InstallEventHandler(
                GetApplicationEventTarget(),
                { _, event, userData in
                    guard let userData, let event else { return noErr }
                    var hotKeyID = EventHotKeyID()
                    let status = GetEventParameter(
                        event, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID), nil,
                        MemoryLayout<EventHotKeyID>.size, nil, &hotKeyID)
                    guard status == noErr else { return noErr }
                    let hotKey = Unmanaged<GlobalHotKey>.fromOpaque(userData).takeUnretainedValue()
                    // Carbon delivers application events on the main thread.
                    MainActor.assumeIsolated {
                        guard hotKeyID.signature == hotKey.signature else { return }
                        hotKey.onPress?(hotKeyID.id)
                    }
                    return noErr
                }, 1, &spec, me, &handler)
        }
    }
#endif
