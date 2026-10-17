using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Laching.Core.Settings;
using Windows.Storage;

namespace Laching.Windows.Shell;

/// <summary>
/// Settings storage in the package's <c>ApplicationData.LocalSettings</c> (needs package identity). Booleans, integers,
/// doubles and strings are stored natively; <c>int</c> and <c>string</c> lists go in a composite value as JSON, because
/// settings values can't be arrays. A getter returns null when the key is missing or holds another type.
/// </summary>
public sealed class LocalSettingsStore : ISettingsStore
{
    private const string TypeField = "type";
    private const string JsonField = "json";

    /// <summary><see cref="ForApp"/>'s file stores, one per file: each holds the whole file and writes all of it.</summary>
    private static readonly ConcurrentDictionary<string, FileSettingsStore> Files = new(StringComparer.OrdinalIgnoreCase);

    private readonly IDictionary<string, object> values;

    /// <summary>The app's local settings container (throws without package identity).</summary>
    public LocalSettingsStore()
        : this(ApplicationData.Current.LocalSettings) { }

    /// <summary>
    /// The app's settings wherever it runs: the package's local settings when it has package identity, else a
    /// <see cref="FileSettingsStore"/> at <c>%LOCALAPPDATA%\<paramref name="appFolder"/>\settings.json</c> (an
    /// unpackaged Debug build, which has no package data). Name the folder after the build ("My App Dev") so a Debug
    /// run never reads a Release install's settings. The same folder gives the same file store for the whole process,
    /// so two parts of the app that ask for it (the language <c>CompanionApp.Start</c> reads, the settings the app makes)
    /// never write over each other's changes.
    /// </summary>
    public static ISettingsStore ForApp(string appFolder) =>
        PackageIdentity.IsPackaged
            ? new LocalSettingsStore()
            : Files.GetOrAdd(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolder, "settings.json"),
                path => new FileSettingsStore(path)
            );

    /// <summary>A container of your choice, such as a sub-container for one feature.</summary>
    public LocalSettingsStore(ApplicationDataContainer container)
        : this(container.Values) { }

    /// <summary>Any value set, for tests that run without package identity.</summary>
    internal LocalSettingsStore(IDictionary<string, object> values)
    {
        this.values = values;
    }

    public bool? GetBool(string key) => values.TryGetValue(key, out var value) && value is bool typed ? typed : null;

    public int? GetInteger(string key) => values.TryGetValue(key, out var value) && value is int typed ? typed : null;

    public double? GetDouble(string key) => values.TryGetValue(key, out var value) && value is double typed ? typed : null;

    public string? GetString(string key) => values.TryGetValue(key, out var value) && value is string typed ? typed : null;

    public IReadOnlyList<string>? GetStrings(string key) =>
        StoredJson(key, "string[]") is { } json ? JsonSerializer.Deserialize(json, SettingsJson.Default.StringArray) : null;

    public IReadOnlyList<int>? GetIntegers(string key) =>
        StoredJson(key, "int[]") is { } json ? JsonSerializer.Deserialize(json, SettingsJson.Default.Int32Array) : null;

    public void SetBool(string key, bool value) => values[key] = value;

    public void SetInteger(string key, int value) => values[key] = value;

    public void SetDouble(string key, double value) => values[key] = value;

    public void SetString(string key, string value) => values[key] = value;

    public void SetStrings(string key, IReadOnlyList<string> value) =>
        values[key] = Composite("string[]", JsonSerializer.Serialize(value.ToArray(), SettingsJson.Default.StringArray));

    public void SetIntegers(string key, IReadOnlyList<int> value) =>
        values[key] = Composite("int[]", JsonSerializer.Serialize(value.ToArray(), SettingsJson.Default.Int32Array));

    public void Remove(string key) => values.Remove(key);

    /// <summary>The JSON of the composite under <paramref name="key"/> when it holds <paramref name="type"/>.</summary>
    private string? StoredJson(string key, string type) =>
        values.TryGetValue(key, out var value)
        && value is ApplicationDataCompositeValue composite
        && composite.TryGetValue(TypeField, out var stored)
        && type.Equals(stored)
        && composite.TryGetValue(JsonField, out var json)
            ? json as string
            : null;

    private static ApplicationDataCompositeValue Composite(string type, string json) =>
        new() { [TypeField] = type, [JsonField] = json };
}

/// <summary>Source-generated JSON for the list settings, so it works under Native AOT.</summary>
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class SettingsJson : JsonSerializerContext;
