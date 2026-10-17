using Laching.Companion.Cadence;
using Laching.Companion.Phrases;

namespace Laching.Companion.Rules;

/// <summary>Why the character is about to say something. The raw values (<see cref="Triggers.RawValue"/>) are in the vectors.</summary>
public enum Trigger
{
    /// <summary>The chatter interval came up.</summary>
    Due,

    /// <summary>The user pressed Say something.</summary>
    ButtonPress,

    /// <summary>A line held during a call is released (one held for full screen comes back as <see cref="Due"/>).</summary>
    AfterCall,
    WelcomeBack,
    ShushEnd,
    BatteryLow,
    Charging,
    Unplugged,
    WentDark,
    WentLight,
}

/// <summary>The trigger tables and raw values (Swift's <c>Trigger.tag</c>, <c>rawValue</c>).</summary>
public static class Triggers
{
    /// <summary>Every trigger, in declaration order.</summary>
    public static IReadOnlyList<Trigger> All { get; } = Enum.GetValues<Trigger>();

    /// <summary>The phrase tag the picker should prefer, or null for ambient chatter.</summary>
    public static Tag? PhraseTag(this Trigger trigger) =>
        trigger switch
        {
            Trigger.Due => null,
            Trigger.ButtonPress => Tag.ButtonPress,
            Trigger.AfterCall => Tag.AfterCall,
            Trigger.WelcomeBack => Tag.WelcomeBack,
            Trigger.ShushEnd => Tag.ShushEnd,
            Trigger.BatteryLow => Tag.BatteryLow,
            Trigger.Charging => Tag.Charging,
            Trigger.Unplugged => Tag.Unplugged,
            Trigger.WentDark => Tag.WentDark,
            Trigger.WentLight => Tag.WentLight,
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "unknown trigger"),
        };

    public static string RawValue(this Trigger trigger) =>
        trigger switch
        {
            Trigger.Due => "due",
            Trigger.ButtonPress => "buttonPress",
            Trigger.AfterCall => "afterCall",
            Trigger.WelcomeBack => "welcomeBack",
            Trigger.ShushEnd => "shushEnd",
            Trigger.BatteryLow => "batteryLow",
            Trigger.Charging => "charging",
            Trigger.Unplugged => "unplugged",
            Trigger.WentDark => "wentDark",
            Trigger.WentLight => "wentLight",
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "unknown trigger"),
        };

    /// <summary>The inverse of <see cref="RawValue"/>: null for an unknown name.</summary>
    public static Trigger? Parse(string? rawValue) => RawValues.Parse(All, RawValue, rawValue);
}

/// <summary>How a line is delivered. The panel's speech bubble always shows it.</summary>
public sealed record ChannelPlan(bool Speak, bool Chime, bool Notify)
{
    public static ChannelPlan BubbleOnly { get; } = new(false, false, false);
}

/// <summary>Why a line was not delivered.</summary>
public enum SkipReason
{
    /// <summary>A call or a full-screen app.</summary>
    Busy,
    Away,
    Locked,

    /// <summary>A line is being spoken; two never overlap (a button press is delivered, and the shell cuts the line).</summary>
    Speaking,

    /// <summary>The user turned this kind of comment off.</summary>
    Disabled,
}

/// <summary>The raw values of <see cref="SkipReason"/> (Swift's <c>rawValue</c>).</summary>
public static class SkipReasons
{
    public static string RawValue(this SkipReason reason) =>
        reason switch
        {
            SkipReason.Busy => "busy",
            SkipReason.Away => "away",
            SkipReason.Locked => "locked",
            SkipReason.Speaking => "speaking",
            SkipReason.Disabled => "disabled",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "unknown skip reason"),
        };
}

/// <summary>What the rules decided: skip for a reason, or deliver with a plan.</summary>
public abstract record Outcome
{
    private Outcome()
    {
    }

    public sealed record Skip(SkipReason Reason) : Outcome;

    public sealed record Deliver(ChannelPlan Plan) : Outcome;
}

/// <summary>The settings the rules consult.</summary>
public sealed record Manners(Channels Channels, bool GreetOnReturn = true, bool CommentOnPower = true, bool CommentOnAppearance = true);

/// <summary>
/// Decides whether a line may be delivered right now and how. Pure; <c>conformance/companion/quiet</c> pins it.
/// <para>
/// In order: a comment the user turned off is skipped; nothing starts while a line is being spoken, except a button
/// press: the user asking now (Say something, a click on the character, a notification's Say more, which comes while its
/// line is still heard) is delivered, and the shell cuts the line being said first, as Shh does, so two still never
/// overlap and nothing waits in a queue; nothing is said to a locked screen; nothing is said to an empty room (idle
/// past the threshold) except the welcome back itself; a call or full-screen app holds everything except a button
/// press, which still shows in the panel but neither speaks nor chimes. Then the channels: voice and chime only when the
/// output isn't muted; a notification when that channel is on, or as the fallback when voice or chime would have played
/// but the output is muted and notifications are allowed. A button press never notifies.
/// </para>
/// </summary>
public static class QuietRules
{
    public const double AwayThresholdSeconds = ChatterCadence.IdleThresholdSeconds;

    public static Outcome Decide(Trigger trigger, Snapshot snapshot, Manners manners)
    {
        switch (trigger)
        {
            case Trigger.WelcomeBack when !manners.GreetOnReturn:
                return new Outcome.Skip(SkipReason.Disabled);
            case Trigger.BatteryLow:
            case Trigger.Charging:
            case Trigger.Unplugged:
                if (!manners.CommentOnPower)
                    return new Outcome.Skip(SkipReason.Disabled);
                break;
            case Trigger.WentDark:
            case Trigger.WentLight:
                if (!manners.CommentOnAppearance)
                    return new Outcome.Skip(SkipReason.Disabled);
                break;
        }
        var isButton = trigger == Trigger.ButtonPress;
        if (snapshot.IsSpeaking && !isButton)
            return new Outcome.Skip(SkipReason.Speaking);
        if (snapshot.IsScreenLocked)
            return new Outcome.Skip(SkipReason.Locked);
        if (snapshot.IdleSeconds >= AwayThresholdSeconds && trigger != Trigger.WelcomeBack && !isButton)
            return new Outcome.Skip(SkipReason.Away);
        var busy = snapshot.Busy.Count > 0;
        if (busy && !isButton)
            return new Outcome.Skip(SkipReason.Busy);

        var muted = snapshot.IsOutputMuted == true;
        var audible = !muted && !busy;
        var speak = manners.Channels.Contains(Channels.Voice) && audible;
        var chime = manners.Channels.Contains(Channels.Chime) && audible;
        var wantedSound = manners.Channels.Contains(Channels.Voice) || manners.Channels.Contains(Channels.Chime);
        var fallback = muted && wantedSound && snapshot.NotificationsAuthorized;
        var notify = !isButton && (manners.Channels.Contains(Channels.Notification) || fallback);
        return new Outcome.Deliver(new ChannelPlan(speak, chime, notify));
    }
}
