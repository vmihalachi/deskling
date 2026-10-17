namespace Laching.Companion.Phrases;

/// <summary>
/// When a phrase may play. Event tags describe something that just happened and win over ambient chatter; daypart and
/// weekday tags narrow ambient chatter to the moment; <see cref="General"/> fits any moment. A closed set: these are
/// the moments the system signals can detect, the same for every companion. The raw values
/// (<see cref="Tags.RawValue"/>) are public: they appear in phrases.json and the vectors. Never rename one.
/// </summary>
public enum Tag
{
    General,
    Morning,
    Afternoon,
    Evening,
    Night,
    Monday,
    Friday,
    Weekend,
    WelcomeBack,
    AfterCall,
    ShushEnd,
    ButtonPress,
    BatteryLow,
    Charging,
    Unplugged,
    WentDark,
    WentLight,
}

/// <summary>The tag tables and raw values (Swift's <c>Tag.allCases</c>, <c>events</c>, <c>daypart</c>, <c>rawValue</c>).</summary>
public static class Tags
{
    /// <summary>Every tag, in declaration order (the order of <c>tags</c> in phrases.json).</summary>
    public static IReadOnlyList<Tag> All { get; } = Enum.GetValues<Tag>();

    /// <summary>Tags that describe something that just happened, as opposed to the time of day.</summary>
    public static IReadOnlySet<Tag> Events { get; } =
        new HashSet<Tag>
        {
            Tag.WelcomeBack,
            Tag.AfterCall,
            Tag.ShushEnd,
            Tag.ButtonPress,
            Tag.BatteryLow,
            Tag.Charging,
            Tag.Unplugged,
            Tag.WentDark,
            Tag.WentLight,
        };

    public static IReadOnlySet<Tag> Dayparts { get; } = new HashSet<Tag> { Tag.Morning, Tag.Afternoon, Tag.Evening, Tag.Night };

    public static IReadOnlySet<Tag> Weekdays { get; } = new HashSet<Tag> { Tag.Monday, Tag.Friday, Tag.Weekend };

    public static bool IsEvent(this Tag tag) => Events.Contains(tag);

    /// <summary>The daypart a local hour (0–23) falls in: morning 05–11, afternoon 12–16, evening 17–21, night 22–04.</summary>
    public static Tag Daypart(int hour) =>
        hour switch
        {
            >= 5 and <= 11 => Tag.Morning,
            >= 12 and <= 16 => Tag.Afternoon,
            >= 17 and <= 21 => Tag.Evening,
            _ => Tag.Night,
        };

    /// <summary>The weekday tag for a calendar weekday (1 = Sunday … 7 = Saturday), if the day has one.</summary>
    public static Tag? WeekdayTag(int weekday) =>
        weekday switch
        {
            2 => Tag.Monday,
            6 => Tag.Friday,
            1 or 7 => Tag.Weekend,
            _ => null,
        };

    /// <summary>The Swift raw value: the name in phrases.json and the vectors.</summary>
    public static string RawValue(this Tag tag) =>
        tag switch
        {
            Tag.General => "general",
            Tag.Morning => "morning",
            Tag.Afternoon => "afternoon",
            Tag.Evening => "evening",
            Tag.Night => "night",
            Tag.Monday => "monday",
            Tag.Friday => "friday",
            Tag.Weekend => "weekend",
            Tag.WelcomeBack => "welcomeBack",
            Tag.AfterCall => "afterCall",
            Tag.ShushEnd => "shushEnd",
            Tag.ButtonPress => "buttonPress",
            Tag.BatteryLow => "batteryLow",
            Tag.Charging => "charging",
            Tag.Unplugged => "unplugged",
            Tag.WentDark => "wentDark",
            Tag.WentLight => "wentLight",
            _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, "unknown tag"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static Tag? Parse(string? rawValue) =>
        rawValue switch
        {
            "general" => Tag.General,
            "morning" => Tag.Morning,
            "afternoon" => Tag.Afternoon,
            "evening" => Tag.Evening,
            "night" => Tag.Night,
            "monday" => Tag.Monday,
            "friday" => Tag.Friday,
            "weekend" => Tag.Weekend,
            "welcomeBack" => Tag.WelcomeBack,
            "afterCall" => Tag.AfterCall,
            "shushEnd" => Tag.ShushEnd,
            "buttonPress" => Tag.ButtonPress,
            "batteryLow" => Tag.BatteryLow,
            "charging" => Tag.Charging,
            "unplugged" => Tag.Unplugged,
            "wentDark" => Tag.WentDark,
            "wentLight" => Tag.WentLight,
            _ => null,
        };
}
