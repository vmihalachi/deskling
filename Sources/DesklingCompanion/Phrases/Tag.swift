import Foundation

/// When a phrase may play. Event tags describe something that just happened and win over ambient chatter;
/// daypart and weekday tags narrow ambient chatter to the moment; `general` fits any moment. A closed set: these
/// are the moments the system signals can detect, the same for every companion.
/// Raw values are public: they appear in `phrases.json` and the vectors. Never rename one.
public enum Tag: String, CaseIterable, Codable, Sendable {
    case general
    case morning, afternoon, evening, night
    case monday, friday, weekend
    case welcomeBack, afterCall, shushEnd, buttonPress
    case batteryLow, charging, unplugged
    case wentDark, wentLight

    /// Tags that describe something that just happened, as opposed to the time of day.
    public static let events: Set<Tag> = [
        .welcomeBack, .afterCall, .shushEnd, .buttonPress, .batteryLow, .charging, .unplugged, .wentDark, .wentLight,
    ]
    public static let dayparts: Set<Tag> = [.morning, .afternoon, .evening, .night]
    public static let weekdays: Set<Tag> = [.monday, .friday, .weekend]

    public var isEvent: Bool { Tag.events.contains(self) }

    /// The daypart a local hour (0–23) falls in: morning 05–11, afternoon 12–16, evening 17–21, night 22–04.
    public static func daypart(hour: Int) -> Tag {
        switch hour {
        case 5...11: .morning
        case 12...16: .afternoon
        case 17...21: .evening
        default: .night
        }
    }

    /// The weekday tag for a calendar weekday (1 = Sunday … 7 = Saturday), if the day has one.
    public static func weekdayTag(weekday: Int) -> Tag? {
        switch weekday {
        case 2: .monday
        case 6: .friday
        case 1, 7: .weekend
        default: nil
        }
    }
}
