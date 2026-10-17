using System.Text.Json;
using System.Text.Json.Nodes;
using Laching.Companion.Rules;
using Laching.Core.Scheduling;

namespace Laching.Testing.Conformance;

/// <summary>
/// The conventions of conformance/README.md the replays share (the scheduler's and the companion's): busy reasons by
/// name, the snapshot object (Swift's <c>SnapshotData</c>), optional values (null or left out both mean "no value") and
/// JSON comparison.
/// </summary>
public static class VectorSupport
{
    /// <summary><see cref="BusyReason"/> as the vectors spell it.</summary>
    public static string BusyName(BusyReason reason) =>
        reason switch
        {
            BusyReason.Call => "call",
            BusyReason.FullScreen => "fullScreen",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "unknown busy reason"),
        };

    /// <summary>The inverse of <see cref="BusyName"/>: null for an unknown name.</summary>
    public static BusyReason? ParseBusy(string name) =>
        name switch
        {
            "call" => BusyReason.Call,
            "fullScreen" => BusyReason.FullScreen,
            _ => null,
        };

    /// <summary>
    /// A step's <c>snapshot</c> object: <c>busy</c> names, whole <c>idleSeconds</c>, <c>isScreenLocked</c>,
    /// <c>isOutputMuted</c> (null when unknown), <c>isSpeaking</c>, <c>notificationsAuthorized</c>. Unknown busy names
    /// are dropped, as in Swift.
    /// </summary>
    public static Snapshot ReadSnapshot(JsonElement e) =>
        new()
        {
            Busy = Strings(e.GetProperty("busy")).Select(ParseBusy).OfType<BusyReason>().ToHashSet(),
            IdleSeconds = e.GetProperty("idleSeconds").GetInt32(),
            IsScreenLocked = e.GetProperty("isScreenLocked").GetBoolean(),
            IsOutputMuted = Has(e, "isOutputMuted", out var muted) ? muted.GetBoolean() : null,
            IsSpeaking = e.GetProperty("isSpeaking").GetBoolean(),
            NotificationsAuthorized = e.GetProperty("notificationsAuthorized").GetBoolean(),
        };

    public static bool Has(JsonElement e, string name, out JsonElement value) =>
        e.TryGetProperty(name, out value) && value.ValueKind != JsonValueKind.Null;

    public static string? OptionalString(JsonElement e, string name) => Has(e, name, out var v) ? v.GetString() : null;

    public static double OptionalDouble(JsonElement e, string name, double fallback) => Has(e, name, out var v) ? v.GetDouble() : fallback;

    public static int OptionalInt(JsonElement e, string name, int fallback) => Has(e, name, out var v) ? v.GetInt32() : fallback;

    public static ulong OptionalUInt64(JsonElement e, string name, ulong fallback) => Has(e, name, out var v) ? v.GetUInt64() : fallback;

    /// <summary>The strings array <paramref name="name"/>, or none when it is null or left out.</summary>
    public static string[] OptionalStrings(JsonElement e, string name) => Has(e, name, out var v) ? Strings(v) : [];

    public static string[] Strings(JsonElement array) => array.EnumerateArray().Select(x => x.GetString()!).ToArray();

    public static int[] Ints(JsonElement array) => array.EnumerateArray().Select(x => x.GetInt32()).ToArray();

    /// <summary>A strings array node.</summary>
    public static JsonArray Array(IEnumerable<string> values) => new(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());

    /// <summary>An integers array node.</summary>
    public static JsonArray Array(IEnumerable<int> values) => new(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());

    /// <summary>Equal JSON, numbers compared by value (so <c>1</c> and <c>1.0</c> agree); a null node equals JSON null.</summary>
    public static bool Same(JsonNode? expected, JsonNode? actual) => JsonNode.DeepEquals(expected, actual);
}
