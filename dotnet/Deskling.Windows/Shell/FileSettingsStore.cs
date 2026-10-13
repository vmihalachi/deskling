using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Deskling.Core.Settings;

namespace Deskling.Windows.Shell;

/// <summary>
/// Settings in one JSON file, for an app that runs without package identity (an unpackaged Debug build), where
/// <see cref="LocalSettingsStore"/>'s <c>ApplicationData</c> doesn't exist. Every value keeps its type, as in the
/// packaged store, so a getter returns null when the key is missing or holds another type. Each change writes the
/// file at once (settings change a few times a day); a file that can't be read starts empty rather than failing the
/// launch. <see cref="LocalSettingsStore.ForApp"/> picks between the two.
/// </summary>
public sealed class FileSettingsStore : ISettingsStore
{
    private readonly Dictionary<string, StoredSetting> values;

    public FileSettingsStore(string path)
    {
        Path = path;
        values = Load(path);
    }

    /// <summary>The settings file.</summary>
    public string Path { get; }

    public bool? GetBool(string key) => Read(key, "bool") is { } value ? value.GetBoolean() : null;

    public int? GetInteger(string key) => Read(key, "int") is { } value ? value.GetInt32() : null;

    public double? GetDouble(string key) => Read(key, "double") is { } value ? value.GetDouble() : null;

    public string? GetString(string key) => Read(key, "string") is { } value ? value.GetString() : null;

    public IReadOnlyList<string>? GetStrings(string key) =>
        Read(key, "string[]") is { } value ? value.Deserialize(FileSettingsJson.Default.StringArray) : null;

    public IReadOnlyList<int>? GetIntegers(string key) =>
        Read(key, "int[]") is { } value ? value.Deserialize(FileSettingsJson.Default.Int32Array) : null;

    public void SetBool(string key, bool value) => Write(key, "bool", JsonSerializer.SerializeToElement(value, FileSettingsJson.Default.Boolean));

    public void SetInteger(string key, int value) => Write(key, "int", JsonSerializer.SerializeToElement(value, FileSettingsJson.Default.Int32));

    public void SetDouble(string key, double value) => Write(key, "double", JsonSerializer.SerializeToElement(value, FileSettingsJson.Default.Double));

    public void SetString(string key, string value) => Write(key, "string", JsonSerializer.SerializeToElement(value, FileSettingsJson.Default.String));

    public void SetStrings(string key, IReadOnlyList<string> value) =>
        Write(key, "string[]", JsonSerializer.SerializeToElement(value.ToArray(), FileSettingsJson.Default.StringArray));

    public void SetIntegers(string key, IReadOnlyList<int> value) =>
        Write(key, "int[]", JsonSerializer.SerializeToElement(value.ToArray(), FileSettingsJson.Default.Int32Array));

    public void Remove(string key)
    {
        if (values.Remove(key))
            Save();
    }

    private JsonElement? Read(string key, string type) => values.TryGetValue(key, out var stored) && stored.Type == type ? stored.Value : null;

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
        File.WriteAllText(temporary, JsonSerializer.Serialize(values, FileSettingsJson.Default.DictionaryStringStoredSetting));
        File.Move(temporary, Path, overwrite: true);
    }

    private static Dictionary<string, StoredSetting> Load(string path)
    {
        try
        {
            if (File.Exists(path) && JsonSerializer.Deserialize(File.ReadAllText(path), FileSettingsJson.Default.DictionaryStringStoredSetting) is { } stored)
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

/// <summary>Source-generated JSON for <see cref="FileSettingsStore"/>, so it works under Native AOT.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, StoredSetting>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class FileSettingsJson : JsonSerializerContext;
