namespace Deskling.Companion.Rules;

/// <summary>
/// How the character delivers a line: any combination of speaking it, playing a chime and showing a notification. Empty
/// (<see cref="Quiet"/>) means "quietly": the line appears only in the panel. The names are stored in settings and
/// appear in the vectors; never rename one. An option set like Swift's, so it compares by value.
/// </summary>
public readonly record struct Channels(int RawValue)
{
    public static readonly Channels Quiet = new(0);
    public static readonly Channels Voice = new(1 << 0);
    public static readonly Channels Chime = new(1 << 1);
    public static readonly Channels Notification = new(1 << 2);
    public static readonly Channels All = new(Voice.RawValue | Chime.RawValue | Notification.RawValue);

    /// <summary>The stable names, in a fixed order, for storage and vectors.</summary>
    private static readonly (Channels Channel, string Name)[] Named =
    [
        (Voice, "voice"),
        (Chime, "chime"),
        (Notification, "notification"),
    ];

    public bool IsEmpty => RawValue == 0;

    /// <summary>The names of the channels in this set, in the fixed order.</summary>
    public IReadOnlyList<string> Names
    {
        get
        {
            // A loop, not a lambda: a lambda inside a struct can't read this.RawValue.
            var names = new List<string>();
            foreach (var (channel, name) in Named)
            {
                if (Contains(channel))
                    names.Add(name);
            }
            return names;
        }
    }

    public bool Contains(Channels channel) => (RawValue & channel.RawValue) == channel.RawValue;

    public static Channels operator |(Channels left, Channels right) => new(left.RawValue | right.RawValue);

    /// <summary>Unknown names are ignored, so a newer setting reads on an older build.</summary>
    public static Channels FromNames(IEnumerable<string> names)
    {
        var wanted = new HashSet<string>(names, StringComparer.Ordinal);
        var set = Quiet;
        foreach (var (channel, name) in Named)
        {
            if (wanted.Contains(name))
                set |= channel;
        }
        return set;
    }
}
