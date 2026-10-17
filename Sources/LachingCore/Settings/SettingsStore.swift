import Foundation

/// Where an app's settings are kept: `UserDefaults` on the Mac (`UserDefaultsSettingsStore`), the app's local
/// settings on Windows (`LocalSettingsStore` in `Laching.Windows`), memory in tests (`InMemorySettingsStore`).
/// Values are typed; a getter returns nil when the key is missing or holds another type. A store never touches
/// the network.
public protocol SettingsStore: AnyObject {
    func bool(forKey key: String) -> Bool?
    func integer(forKey key: String) -> Int?
    func double(forKey key: String) -> Double?
    func string(forKey key: String) -> String?
    func strings(forKey key: String) -> [String]?
    func integers(forKey key: String) -> [Int]?
    func set(bool value: Bool, forKey key: String)
    func set(integer value: Int, forKey key: String)
    func set(double value: Double, forKey key: String)
    func set(string value: String, forKey key: String)
    func set(strings value: [String], forKey key: String)
    func set(integers value: [Int], forKey key: String)
    func remove(forKey key: String)
}

/// A settings store that forgets everything when it goes away: tests and previews.
public final class InMemorySettingsStore: SettingsStore {
    public private(set) var values: [String: Any] = [:]

    public init() {}

    public var keys: Set<String> { Set(values.keys) }

    public func bool(forKey key: String) -> Bool? { values[key] as? Bool }
    public func integer(forKey key: String) -> Int? { values[key] as? Int }
    public func double(forKey key: String) -> Double? { values[key] as? Double }
    public func string(forKey key: String) -> String? { values[key] as? String }
    public func strings(forKey key: String) -> [String]? { values[key] as? [String] }
    public func integers(forKey key: String) -> [Int]? { values[key] as? [Int] }
    public func set(bool value: Bool, forKey key: String) { values[key] = value }
    public func set(integer value: Int, forKey key: String) { values[key] = value }
    public func set(double value: Double, forKey key: String) { values[key] = value }
    public func set(string value: String, forKey key: String) { values[key] = value }
    public func set(strings value: [String], forKey key: String) { values[key] = value }
    public func set(integers value: [Int], forKey key: String) { values[key] = value }
    public func remove(forKey key: String) { values[key] = nil }
}
