import Foundation

/// A JSON tree a generator returns when an app adds its own fields (`SettingsVector.base(defaults:)`): edit it, then
/// write it with `ConformanceSupport.encode`. Integers and doubles stay apart, so `30` and `0.5` come out as written.
public enum JSONValue: Equatable, Sendable, Codable {
    case string(String)
    case int(Int)
    case double(Double)
    case bool(Bool)
    case null
    case array([JSONValue])
    case object([String: JSONValue])

    public init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        if container.decodeNil() {
            self = .null
        } else if let value = try? container.decode(Bool.self) {
            self = .bool(value)
        } else if let value = try? container.decode(Int.self) {
            self = .int(value)
        } else if let value = try? container.decode(Double.self) {
            self = .double(value)
        } else if let value = try? container.decode(String.self) {
            self = .string(value)
        } else if let value = try? container.decode([JSONValue].self) {
            self = .array(value)
        } else {
            self = .object(try container.decode([String: JSONValue].self))
        }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.singleValueContainer()
        switch self {
        case .string(let value): try container.encode(value)
        case .int(let value): try container.encode(value)
        case .double(let value): try container.encode(value)
        case .bool(let value): try container.encode(value)
        case .null: try container.encodeNil()
        case .array(let value): try container.encode(value)
        case .object(let value): try container.encode(value)
        }
    }

    /// `value` (any `Encodable`) as a tree.
    public static func from<T: Encodable>(_ value: T) throws -> JSONValue {
        try JSONDecoder().decode(JSONValue.self, from: JSONEncoder().encode(value))
    }

    /// A strings array.
    public static func strings(_ values: [String]) -> JSONValue {
        .array(values.map(JSONValue.string))
    }

    /// The member `key` of an object; setting it on a non-object does nothing.
    public subscript(key: String) -> JSONValue? {
        get {
            guard case .object(let members) = self else { return nil }
            return members[key]
        }
        set {
            guard case .object(var members) = self else { return }
            members[key] = newValue
            self = .object(members)
        }
    }

    /// An array with `value` inserted after the first element equal to `after` (at the end when there is none).
    public func inserting(_ value: JSONValue, after: JSONValue) -> JSONValue {
        guard case .array(var items) = self else { return self }
        items.insert(value, at: items.firstIndex(of: after).map { $0 + 1 } ?? items.endIndex)
        return .array(items)
    }
}
