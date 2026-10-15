using System.Text.Json;
using System.Text.Json.Nodes;
using Deskling.Companion.Rules;
using Deskling.Core.Scheduling;

namespace Deskling.Testing.Tests.Conformance;

/// <summary>The companion replays' shared conventions: busy names, the snapshot object, optional values, arrays, JSON equality.</summary>
public sealed class VectorSupportTests
{
    [Fact]
    public void BusyNamesRoundTrip()
    {
        foreach (var reason in Enum.GetValues<BusyReason>())
            Assert.Equal(reason, VectorSupport.ParseBusy(VectorSupport.BusyName(reason)));
        Assert.Equal("fullScreen", VectorSupport.BusyName(BusyReason.FullScreen));
        Assert.Null(VectorSupport.ParseBusy("meeting"));
    }

    [Fact]
    public void ReadsASnapshotDroppingUnknownBusyNames()
    {
        using var doc = JsonDocument.Parse(
            """{"busy": ["call", "meeting"], "idleSeconds": 12, "isScreenLocked": true, "isSpeaking": false, "notificationsAuthorized": true}""");
        var snapshot = VectorSupport.ReadSnapshot(doc.RootElement);
        var expected = new Snapshot
        {
            Busy = new HashSet<BusyReason> { BusyReason.Call },
            IdleSeconds = 12,
            IsScreenLocked = true,
            NotificationsAuthorized = true,
        };
        Assert.Equal(expected, snapshot);
        Assert.Null(snapshot.IsOutputMuted);
    }

    [Fact]
    public void AMutedValueIsReadWhenPresent()
    {
        using var doc = JsonDocument.Parse(
            """{"busy": [], "idleSeconds": 0, "isScreenLocked": false, "isOutputMuted": true, "isSpeaking": false, "notificationsAuthorized": false}""");
        Assert.True(VectorSupport.ReadSnapshot(doc.RootElement).IsOutputMuted);
    }

    [Fact]
    public void ANullOrAbsentValueIsNoValue()
    {
        using var doc = JsonDocument.Parse("""{"a": null, "b": "x"}""");
        Assert.False(VectorSupport.Has(doc.RootElement, "a", out _));
        Assert.False(VectorSupport.Has(doc.RootElement, "missing", out _));
        Assert.True(VectorSupport.Has(doc.RootElement, "b", out var b));
        Assert.Equal("x", b.GetString());
        Assert.Null(VectorSupport.OptionalString(doc.RootElement, "a"));
        Assert.Null(VectorSupport.OptionalString(doc.RootElement, "missing"));
        Assert.Equal("x", VectorSupport.OptionalString(doc.RootElement, "b"));
    }

    [Fact]
    public void ArraysReadAndWrite()
    {
        using var doc = JsonDocument.Parse("""{"s": ["a", "b"], "i": [1, 2]}""");
        Assert.Equal(["a", "b"], VectorSupport.Strings(doc.RootElement.GetProperty("s")));
        Assert.Equal([1, 2], VectorSupport.Ints(doc.RootElement.GetProperty("i")));
        Assert.Equal("""["a","b"]""", VectorSupport.Array(["a", "b"]).ToJsonString());
        Assert.Equal("[1,2]", VectorSupport.Array([1, 2]).ToJsonString());
    }

    [Fact]
    public void SameComparesNumbersByValueAndNullToNull()
    {
        Assert.True(VectorSupport.Same(JsonNode.Parse("""{"a": 1, "b": [true]}"""), JsonNode.Parse("""{"a": 1.0, "b": [true]}""")));
        Assert.False(VectorSupport.Same(JsonNode.Parse("""{"a": 1}"""), JsonNode.Parse("""{"a": 2}""")));
        Assert.False(VectorSupport.Same(JsonNode.Parse("""{"a": 1}"""), JsonNode.Parse("""{"a": 1, "b": 2}""")));
        Assert.True(VectorSupport.Same(null, JsonNode.Parse("null")));
    }
}
