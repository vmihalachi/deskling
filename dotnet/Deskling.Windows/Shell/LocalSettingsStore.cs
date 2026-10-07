using System.Text.Json;
using System.Text.Json.Serialization;
using Deskling.Core.Settings;
using Windows.Storage;

namespace Deskling.Windows.Shell;

/// <summary>
/// Settings storage in the package's <c>ApplicationData.LocalSettings</c> (needs package identity). Scalars,
/// strings and <c>DateTimeOffset</c> are stored natively; <c>int[]</c> and <c>string[]</c> go in a composite value as
/// JSON, because settings values can't be arrays.
/// </summary>
public sealed class LocalSettingsStore : ISettingsStore
{
    private const string TypeField = "type";
    private const string JsonField = "json";

    private readonly IDictionary<string, object> values;

    /// <summary>The app's local settings container.</summary>
    public LocalSettingsStore()
        : this(ApplicationData.Current.LocalSettings) { }

    /// <summary>A container of your choice, such as a sub-container for one feature.</summary>
    public LocalSettingsStore(ApplicationDataContainer container)
    {
        values = container.Values;
    }

    public object? Get(string key)
    {
        if (!values.TryGetValue(key, out var value))
            return null;
        if (value is not ApplicationDataCompositeValue composite)
            return value;
        if (composite[JsonField] is not string json)
            return null;
        return composite[TypeField] switch
        {
            "int[]" => JsonSerializer.Deserialize(json, SettingsJson.Default.Int32Array),
            "string[]" => JsonSerializer.Deserialize(json, SettingsJson.Default.StringArray),
            _ => null,
        };
    }

    public void Set(string key, object value)
    {
        values[key] = value switch
        {
            int[] ints => Composite("int[]", JsonSerializer.Serialize(ints, SettingsJson.Default.Int32Array)),
            string[] strings => Composite("string[]", JsonSerializer.Serialize(strings, SettingsJson.Default.StringArray)),
            _ => value,
        };
    }

    public void Remove(string key) => values.Remove(key);

    private static ApplicationDataCompositeValue Composite(string type, string json) =>
        new() { [TypeField] = type, [JsonField] = json };
}

/// <summary>Source-generated JSON for the array settings, so it works under Native AOT.</summary>
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class SettingsJson : JsonSerializerContext;
