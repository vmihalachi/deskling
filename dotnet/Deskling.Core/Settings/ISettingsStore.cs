namespace Deskling.Core.Settings;

/// <summary>
/// Key-value storage for an app's preferences. Values are <c>int</c>, <c>bool</c>, <c>string</c>, <c>int[]</c>,
/// <c>string[]</c> or <c>DateTimeOffset</c>, the types Windows' <c>ApplicationData.LocalSettings</c> stores natively
/// (the Windows package wraps arrays in a composite value). A store never touches the network.
/// </summary>
public interface ISettingsStore
{
    object? Get(string key);

    void Set(string key, object value);

    void Remove(string key);
}

/// <summary>A store that keeps its values in a dictionary, for tests and previews.</summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, object> values = [];

    public object? Get(string key) => values.GetValueOrDefault(key);

    public void Set(string key, object value) => values[key] = value;

    public void Remove(string key) => values.Remove(key);
}
