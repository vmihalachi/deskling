import Foundation

/// `SettingsStore` over `UserDefaults`, `.standard` unless given a suite. A missing key reads as nil, never as
/// `false` or 0, so "never set" falls back to the app's default; so does a key holding another type.
/// `UserDefaults` keeps every number as an `NSNumber` and its own getters coerce between them (`integer(forKey:)`
/// reads `true` as 1), so these go by the number's stored type instead, the way `InMemorySettingsStore` goes by the
/// Swift type: a boolean reads only as a `Bool`, an integer only as an `Int` and a floating-point number only as a
/// `Double`, so `true` under an integer key, `1` under a boolean key and `3.0` under an integer key all read as nil,
/// in this process or after a relaunch (the property list keeps the three apart). A list reads only when every
/// element has the list's type.
public final class UserDefaultsSettingsStore: SettingsStore {
    private let defaults: UserDefaults

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    public func bool(forKey key: String) -> Bool? { Self.number(defaults.object(forKey: key), .bool)?.boolValue }

    public func integer(forKey key: String) -> Int? { Self.number(defaults.object(forKey: key), .integer)?.intValue }

    public func double(forKey key: String) -> Double? { Self.number(defaults.object(forKey: key), .double)?.doubleValue }

    public func string(forKey key: String) -> String? { defaults.object(forKey: key) as? String }

    public func strings(forKey key: String) -> [String]? { defaults.object(forKey: key) as? [String] }

    public func integers(forKey key: String) -> [Int]? {
        guard let values = defaults.object(forKey: key) as? [Any] else { return nil }
        let integers = values.compactMap { Self.number($0, .integer)?.intValue }
        return integers.count == values.count ? integers : nil
    }

    public func set(bool value: Bool, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(integer value: Int, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(double value: Double, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(string value: String, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(strings value: [String], forKey key: String) { defaults.set(value, forKey: key) }

    public func set(integers value: [Int], forKey key: String) { defaults.set(value, forKey: key) }

    public func remove(forKey key: String) { defaults.removeObject(forKey: key) }

    // MARK: Numbers by their stored type

    /// The kinds `UserDefaults` keeps apart, read off a number's `objCType`: a boolean (`kCFBooleanTrue` is a
    /// char-typed `NSNumber`, in memory and back from the property list), a floating-point number, or an integer of
    /// any width. A cast would bridge `true` to 1 and `3.0` to 3; the kind doesn't.
    private enum NumberKind: Equatable {
        case bool, integer, double

        init(_ number: NSNumber) {
            switch String(cString: number.objCType) {
            case "c", "B": self = .bool
            case "d", "f": self = .double
            default: self = .integer
            }
        }
    }

    /// `value` as a number, when it is one of `kind`.
    private static func number(_ value: Any?, _ kind: NumberKind) -> NSNumber? {
        guard let number = value as? NSNumber, NumberKind(number) == kind else { return nil }
        return number
    }
}
