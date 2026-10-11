import Foundation

/// `SettingsStore` over `UserDefaults`, `.standard` unless given a suite. A missing key reads as nil, never as
/// `false` or 0, so "never set" falls back to the app's default.
public final class UserDefaultsSettingsStore: SettingsStore {
    private let defaults: UserDefaults

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    public func bool(forKey key: String) -> Bool? {
        defaults.object(forKey: key) == nil ? nil : defaults.bool(forKey: key)
    }

    public func integer(forKey key: String) -> Int? {
        defaults.object(forKey: key) == nil ? nil : defaults.integer(forKey: key)
    }

    public func double(forKey key: String) -> Double? {
        defaults.object(forKey: key) == nil ? nil : defaults.double(forKey: key)
    }

    public func string(forKey key: String) -> String? { defaults.string(forKey: key) }

    public func strings(forKey key: String) -> [String]? { defaults.stringArray(forKey: key) }

    public func integers(forKey key: String) -> [Int]? { defaults.array(forKey: key) as? [Int] }

    public func set(bool value: Bool, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(integer value: Int, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(double value: Double, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(string value: String, forKey key: String) { defaults.set(value, forKey: key) }

    public func set(strings value: [String], forKey key: String) { defaults.set(value, forKey: key) }

    public func set(integers value: [Int], forKey key: String) { defaults.set(value, forKey: key) }

    public func remove(forKey key: String) { defaults.removeObject(forKey: key) }
}
