using System.Text.Json.Nodes;
using Laching.Companion.Cadence;
using Laching.Companion.Coordination;
using Laching.Companion.Desktop;
using Laching.Companion.Phrases;
using Laching.Companion.Rules;
using Laching.Companion.Settings;
using Laching.Core.Settings;

namespace Laching.Testing.Conformance;

/// <summary>
/// Replays a settings.json (Swift's <c>SettingsVector.base</c>) against a fresh <see cref="CompanionSettings"/> over
/// <c>defaults</c>, the way first launch reads it: every generic first-launch value, the generic storage keys (each
/// listed under its own name, in <c>keyOrder</c> order, with <c>resettable</c> minus onboarding), the frequency, name
/// and desktop size tables and the package's constants. The app's own keys, values and lists (its <c>skins</c>, a voice
/// choice) are left to the app's tests.
/// </summary>
public static class SettingsReplay
{
    /// <summary>The problems found, empty when the file agrees with the code.</summary>
    public static IReadOnlyList<string> Verify(Stream json, string file, CompanionDefaults defaults)
    {
        var committed = JsonNode.Parse(json)!.AsObject();
        var current = Base(defaults);
        var problems = new List<string>();
        var committedDefaults = committed["defaults"]?.AsObject();
        foreach (var (key, value) in current["defaults"]!.AsObject())
        {
            if (committedDefaults is null || !committedDefaults.ContainsKey(key) || !VectorSupport.Same(value, committedDefaults[key]))
                problems.Add($"{file}: defaults.{key} differs from the code");
        }
        var keys = committed["keys"]?.AsObject();
        foreach (var key in SettingsKeys.All)
        {
            if (keys?[key]?.GetValue<string>() != key)
                problems.Add($"{file}: keys.{key} is missing or renamed");
        }
        var generic = new HashSet<string>(SettingsKeys.All, StringComparer.Ordinal);
        foreach (var list in new[] { "keyOrder", "resettable" })
        {
            var names = committed[list]?.AsArray().Select(n => n!.GetValue<string>()).Where(generic.Contains).ToArray() ?? [];
            var expected = current[list]!.AsArray().Select(n => n!.GetValue<string>()).ToArray();
            if (!names.SequenceEqual(expected))
                problems.Add($"{file}: the generic keys of {list} differ");
        }
        foreach (var (key, value) in current)
        {
            if (key is "defaults" or "keys" or "keyOrder" or "resettable")
                continue;
            if (!VectorSupport.Same(value, committed[key]))
                problems.Add($"{file}: {key} differs from the code");
        }
        return problems;
    }

    /// <summary>The generic tree Swift's <c>SettingsVector.base(defaults:)</c> writes, without the app's additions.</summary>
    public static JsonObject Base(CompanionDefaults defaults)
    {
        var s = new CompanionSettings(new InMemorySettingsStore(), defaults);
        var values = new JsonObject
        {
            ["channels"] = VectorSupport.Array(s.Channels.Names),
            ["frequency"] = s.Frequency.RawValue(),
            ["activeStartMinute"] = s.ActiveStartMinute,
            ["activeEndMinute"] = s.ActiveEndMinute,
            ["activeWeekdays"] = VectorSupport.Array(s.ActiveWeekdays.Order()),
            ["quietHours"] = VectorSupport.Array(s.QuietHours.Order()),
            ["hushInFullScreen"] = s.HushInFullScreen,
            ["greetOnReturn"] = s.GreetOnReturn,
            ["commentOnPower"] = s.CommentOnPower,
            ["commentOnAppearance"] = s.CommentOnAppearance,
            ["voiceIdentifier"] = s.VoiceIdentifier,
            ["speechRate"] = s.SpeechRate,
            ["speechVolume"] = s.SpeechVolume,
            ["phraseFlavors"] = VectorSupport.Array(s.PhraseFlavors.Select(f => f.RawValue).Order(StringComparer.Ordinal)),
            ["mutedPhraseIDs"] = VectorSupport.Array(s.MutedPhraseIDs.Order(StringComparer.Ordinal)),
            ["appearance"] = s.Appearance.RawValue(),
            ["skin"] = s.Skin.RawValue,
            ["shushedUntil"] = s.ShushedUntil is { } until ? ConformanceSupport.Iso(until, TimeZoneInfo.Utc) : null,
            ["showOnDesktop"] = s.ShowOnDesktop,
            ["desktopSize"] = s.DesktopSize.RawValue(),
            ["desktopPosition"] = s.DesktopPosition is { } spot ? VectorSupport.Array([spot.X, spot.Y]) : null,
            ["hasCompletedOnboarding"] = s.HasCompletedOnboarding,
        };
        var frequencies = new JsonObject();
        foreach (var f in Frequencies.All)
            frequencies[f.RawValue()] = VectorSupport.Array([(int)f.IntervalSeconds(), (int)f.JitterSeconds()]);
        var keys = new JsonObject();
        foreach (var key in SettingsKeys.All)
            keys[key] = key;
        var sizes = new JsonObject();
        foreach (var size in DesktopSizes.All)
            sizes[size.RawValue()] = (int)size.Height();
        return new JsonObject
        {
            ["defaults"] = values,
            ["keys"] = keys,
            ["keyOrder"] = VectorSupport.Array(SettingsKeys.All),
            ["resettable"] = VectorSupport.Array(SettingsKeys.Resettable),
            ["frequencies"] = frequencies,
            ["channels"] = VectorSupport.Array(Channels.All.Names),
            ["appearances"] = VectorSupport.Array(Appearances.All.Select(a => a.RawValue())),
            ["shushes"] = VectorSupport.Array(Shushes.All.Select(x => x.RawValue())),
            ["desktopSizes"] = sizes,
            ["idleThresholdSeconds"] = (int)ChatterCadence.IdleThresholdSeconds,
            ["busyGraceSeconds"] = (int)ChatterCadence.BusyGraceSeconds,
            ["welcomeBackCooldownSeconds"] = (int)CompanionCoordinator.WelcomeBackCooldownSeconds,
            ["recentCapacity"] = CompanionCoordinator.RecentCapacity,
            ["recentWindow"] = PhrasePicker.RecentWindow,
            ["partBeatSeconds"] = PhraseTiming.PartBeatSeconds,
            ["reappearDelaySeconds"] = (int)DesktopPresence.ReappearDelaySeconds,
            ["hourHideSeconds"] = (int)DesktopPresence.HourHideSeconds,
        };
    }
}
