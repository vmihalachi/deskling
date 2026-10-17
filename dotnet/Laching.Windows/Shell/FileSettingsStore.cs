using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Laching.Core.Settings;

namespace Laching.Windows.Shell;

/// <summary>
/// Settings in one JSON file, for an app that runs without package identity (an unpackaged Debug build), where
/// <see cref="LocalSettingsStore"/>'s <c>ApplicationData</c> doesn't exist. Every value keeps its type, as in the
/// packaged store, so a getter returns null when the key is missing or holds another type, also when the file was edited
/// by hand and a value doesn't match the type stored with it. Each change writes the
/// file at once (settings change a few times a day); a file that can't be read starts empty rather than failing the
/// launch. <see cref="LocalSettingsStore.ForApp"/> picks between the two.
/// </summary>
public sealed class FileSettingsStore : ISettingsStore
{
    /// <summary>The file is written indented, for a person reading it.</summary>
    private static readonly SettingsJson Indented = new(new JsonSerializerOptions { WriteIndented = true });

    private readonly Dictionary<string, StoredSetting> values;

    public FileSettingsStore(string path)
    {
        Path = path;
        values = Load(path);
    }

    /// <summary>The settings file.</summary>
    public string Path { get; }

    public bool? GetBool(string key) =>
        Read(key, "bool") is { ValueKind: JsonValueKind.True or JsonValueKind.False } value ? value.GetBoolean() : null;

    public int? GetInteger(string key) =>
        Read(key, "int") is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var number) ? number : null;

    public double? GetDouble(string key) =>
        Read(key, "double") is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) ? number : null;

    public string? GetString(string key) => Read(key, "string") is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    public IReadOnlyList<string>? GetStrings(string key) =>
        ReadArray(key, "string[]", SettingsJson.Default.StringArray) is { } strings && !strings.Contains(null) ? strings : null;

    public IReadOnlyList<int>? GetIntegers(string key) => ReadArray(key, "int[]", SettingsJson.Default.Int32Array);

    public void SetBool(string key, bool value) => Write(key, "bool", JsonSerializer.SerializeToElement(value, SettingsJson.Default.Boolean));

    public void SetInteger(string key, int value) => Write(key, "int", JsonSerializer.SerializeToElement(value, SettingsJson.Default.Int32));

    public void SetDouble(string key, double value) => Write(key, "double", JsonSerializer.SerializeToElement(value, SettingsJson.Default.Double));

    public void SetString(string key, string value) => Write(key, "string", JsonSerializer.SerializeToElement(value, SettingsJson.Default.String));

    public void SetStrings(string key, IReadOnlyList<string> value) =>
        Write(key, "string[]", JsonSerializer.SerializeToElement(value.ToArray(), SettingsJson.Default.StringArray));

    public void SetIntegers(string key, IReadOnlyList<int> value) =>
        Write(key, "int[]", JsonSerializer.SerializeToElement(value.ToArray(), SettingsJson.Default.Int32Array));

    public void Remove(string key)
    {
        if (values.Remove(key))
            Save();
    }

    /// <summary>
    /// The value under <paramref name="key"/> when it was stored as <paramref name="type"/>. A null entry (a file edited by
    /// hand) reads as missing; the getters also check the value's JSON kind, so a value that doesn't match its type reads
    /// as null instead of throwing (<see cref="Localization.LanguageSetting"/> is read in <c>Main</c>, before any window).
    /// </summary>
    private JsonElement? Read(string key, string type) => values.TryGetValue(key, out var stored) && stored?.Type == type ? stored.Value : null;

    private T? ReadArray<T>(string key, string type, JsonTypeInfo<T> info)
        where T : class
    {
        if (Read(key, type) is not { ValueKind: JsonValueKind.Array } value)
            return null;
        try
        {
            return value.Deserialize(info);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Write(string key, string type, JsonElement value)
    {
        values[key] = new StoredSetting(type, value);
        Save();
    }

    /// <summary>Through a temporary file, so a crash mid-write never leaves half a file.</summary>
    private void Save()
    {
        Directory.CreateDirectory(global::System.IO.Path.GetDirectoryName(Path)!);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(values, Indented.DictionaryStringStoredSetting));
        File.Move(temporary, Path, overwrite: true);
    }

    private static Dictionary<string, StoredSetting> Load(string path)
    {
        try
        {
            if (File.Exists(path) && JsonSerializer.Deserialize(File.ReadAllText(path), SettingsJson.Default.DictionaryStringStoredSetting) is { } stored)
                return new Dictionary<string, StoredSetting>(stored, StringComparer.Ordinal);
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Settings file {path} couldn't be read, starting empty: {error.Message}");
        }
        return new Dictionary<string, StoredSetting>(StringComparer.Ordinal);
    }
}

/// <summary>One setting in <see cref="FileSettingsStore"/>'s file: its type and its value.</summary>
public sealed record StoredSetting(string Type, JsonElement Value);

/// <summary>
/// Source-generated JSON for both settings stores, so they work under Native AOT: <see cref="FileSettingsStore"/>'s file
/// and values, <see cref="LocalSettingsStore"/>'s lists.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, StoredSetting>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class SettingsJson : JsonSerializerContext;
