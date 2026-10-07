import Foundation

/// A global keyboard shortcut: a virtual key code plus Carbon modifier flags. Apps persist it (Codable) and
/// hand it to `DesklingShell.GlobalHotKey`; the Windows port maps the same fields onto `RegisterHotKey`.
public struct KeyShortcut: Codable, Equatable, Sendable {
    public var keyCode: UInt32
    public var modifiers: UInt32
    /// What the key looked like when it was recorded, e.g. "B" or "F5".
    public var key: String

    public init(keyCode: UInt32, modifiers: UInt32, key: String) {
        self.keyCode = keyCode
        self.modifiers = modifiers
        self.key = key
    }

    // Carbon modifier masks (cmdKey, shiftKey, optionKey, controlKey).
    public static let command: UInt32 = 0x0100
    public static let shift: UInt32 = 0x0200
    public static let option: UInt32 = 0x0800
    public static let control: UInt32 = 0x1000

    public var displayString: String {
        var s = ""
        if modifiers & Self.control != 0 { s += "⌃" }
        if modifiers & Self.option != 0 { s += "⌥" }
        if modifiers & Self.shift != 0 { s += "⇧" }
        if modifiers & Self.command != 0 { s += "⌘" }
        return s + key
    }

    /// Needs ⌘, ⌃ or ⌥ so it can't swallow ordinary typing.
    public var isValid: Bool { modifiers & (Self.command | Self.control | Self.option) != 0 }

    /// Labels for keys whose characters don't print well.
    public static func label(forKeyCode code: UInt16) -> String? {
        let special: [UInt16: String] = [
            49: "␣", 36: "↩", 48: "⇥", 51: "⌫", 117: "⌦", 53: "⎋",
            123: "←", 124: "→", 125: "↓", 126: "↑", 115: "↖", 119: "↘", 116: "⇞", 121: "⇟",
            122: "F1", 120: "F2", 99: "F3", 118: "F4", 96: "F5", 97: "F6",
            98: "F7", 100: "F8", 101: "F9", 109: "F10", 103: "F11", 111: "F12",
        ]
        return special[code]
    }
}
