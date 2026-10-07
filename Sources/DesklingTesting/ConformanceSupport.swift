import Foundation

/// Shared helpers for conformance vectors: JSON in the one format every generator uses, and the date,
/// time zone and calendar conventions (`conformance/README.md`). Apps' test targets use these too.
public enum ConformanceSupport {
    public struct VectorError: Error, CustomStringConvertible {
        public let description: String

        public init(_ description: String) {
            self.description = description
        }
    }

    // MARK: JSON

    /// Pretty-printed, sorted keys, slashes unescaped, one trailing newline: the committed byte format.
    public static func encode<T: Encodable>(_ value: T) throws -> Data {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys, .withoutEscapingSlashes]
        var data = try encoder.encode(value)
        data.append(0x0A)
        return data
    }

    public static func decode<T: Decodable>(_ type: T.Type, from data: Data) throws -> T {
        try JSONDecoder().decode(type, from: data)
    }

    // MARK: Time

    /// An IANA time zone such as "UTC" or "Europe/Bucharest".
    public static func timeZone(_ id: String) throws -> TimeZone {
        guard let tz = TimeZone(identifier: id) else { throw VectorError("unknown time zone \(id)") }
        return tz
    }

    /// The calendar every vector uses: Gregorian in the vector's time zone.
    public static func calendar(_ timeZoneID: String) throws -> Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = try timeZone(timeZoneID)
        return calendar
    }

    /// Parses ISO 8601 with an offset, with or without fractional seconds.
    public static func date(_ string: String) throws -> Date {
        for options: ISO8601DateFormatter.Options in [
            [.withInternetDateTime, .withFractionalSeconds],
            [.withInternetDateTime],
        ] {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = options
            if let date = formatter.date(from: string) { return date }
        }
        throw VectorError("not an ISO 8601 date: \(string)")
    }

    /// ISO 8601 in `timeZone` ("2026-09-28T10:00:00+03:00", "Z" for UTC), with milliseconds only
    /// when the date has a fraction of a second.
    public static func string(_ date: Date, in timeZone: TimeZone) -> String {
        let formatter = ISO8601DateFormatter()
        formatter.timeZone = timeZone
        let hasFraction = date.timeIntervalSince1970.rounded(.down) != date.timeIntervalSince1970
        formatter.formatOptions = hasFraction ? [.withInternetDateTime, .withFractionalSeconds] : [.withInternetDateTime]
        return formatter.string(from: date)
    }

    /// Builds a local date in `calendar`'s time zone, for writing readable vector inputs.
    public static func local(
        _ calendar: Calendar, _ year: Int, _ month: Int, _ day: Int,
        _ hour: Int = 0, _ minute: Int = 0, _ second: Int = 0
    ) -> Date {
        calendar.date(
            from: DateComponents(
                year: year, month: month, day: day,
                hour: hour, minute: minute, second: second))!
    }
}
