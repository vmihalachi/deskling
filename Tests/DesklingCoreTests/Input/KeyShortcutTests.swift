import DesklingCore
import XCTest

final class KeyShortcutTests: XCTestCase {
    func testDisplayStringOrdersModifiersLikeTheMenuBar() {
        let shortcut = KeyShortcut(keyCode: 11, modifiers: KeyShortcut.control | KeyShortcut.option | KeyShortcut.command, key: "B")
        XCTAssertEqual(shortcut.displayString, "⌃⌥⌘B")
        XCTAssertEqual(KeyShortcut(keyCode: 1, modifiers: KeyShortcut.shift | KeyShortcut.command, key: "S").displayString, "⇧⌘S")
    }

    func testValidityNeedsCommandControlOrOption() {
        XCTAssertTrue(KeyShortcut(keyCode: 11, modifiers: KeyShortcut.command, key: "B").isValid)
        XCTAssertFalse(KeyShortcut(keyCode: 11, modifiers: KeyShortcut.shift, key: "B").isValid)
        XCTAssertFalse(KeyShortcut(keyCode: 11, modifiers: 0, key: "B").isValid)
    }

    func testSpecialKeyLabels() {
        XCTAssertEqual(KeyShortcut.label(forKeyCode: 49), "␣")
        XCTAssertEqual(KeyShortcut.label(forKeyCode: 122), "F1")
        XCTAssertNil(KeyShortcut.label(forKeyCode: 11))
    }

    func testRoundTripsThroughJSON() throws {
        let shortcut = KeyShortcut(keyCode: 96, modifiers: KeyShortcut.option, key: "F5")
        let data = try JSONEncoder().encode(shortcut)
        XCTAssertEqual(try JSONDecoder().decode(KeyShortcut.self, from: data), shortcut)
    }
}
