import Foundation

/// System, light or dark. Raw values are stored; never rename one.
public enum AppearanceSetting: String, CaseIterable, Codable, Sendable {
    case system
    case light
    case dark
}

/// How big the character stands on the desktop. Raw values are stored; never rename one.
public enum DesktopSize: String, CaseIterable, Codable, Sendable {
    case small
    case medium
    case large

    /// The character's height on the desktop, in points (device-independent pixels on Windows); the width follows
    /// the art.
    public var height: Double {
        switch self {
        case .small: 64
        case .medium: 96
        case .large: 140
        }
    }

    /// `step` sizes bigger (or smaller, when negative), stopping at Small and Large: a trackpad pinch on the character.
    public func stepped(by step: Int) -> DesktopSize {
        let all = Self.allCases
        let index = (all.firstIndex(of: self) ?? 0) + step
        return all[min(max(index, 0), all.count - 1)]
    }
}

/// Where the character stands on the desktop: the middle of the bottom edge of its desktop window (the contact
/// shadow under the character), in points from the primary screen's top left with y down, so the same value means
/// the same spot on both platforms.
public struct DesktopPosition: Equatable, Codable, Sendable {
    public var x: Int
    public var y: Int

    public init(x: Int, y: Int) {
        self.x = x
        self.y = y
    }
}
