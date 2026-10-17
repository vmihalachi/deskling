namespace Laching.Core.Settings;

/// <summary>
/// Where an app's settings are kept: <c>UserDefaults</c> on the Mac, the app's local settings on Windows
/// (<c>LocalSettingsStore</c> in <c>Laching.Windows</c>), memory in tests (<see cref="InMemorySettingsStore"/>).
/// Values are typed; a getter returns null when the key is missing or holds another type. A store never touches the
/// network.
/// </summary>
public interface ISettingsStore
{
    bool? GetBool(string key);

    int? GetInteger(string key);

    double? GetDouble(string key);

    string? GetString(string key);

    IReadOnlyList<string>? GetStrings(string key);

    IReadOnlyList<int>? GetIntegers(string key);

    void SetBool(string key, bool value);

    void SetInteger(string key, int value);

    void SetDouble(string key, double value);

    void SetString(string key, string value);

    void SetStrings(string key, IReadOnlyList<string> value);

    void SetIntegers(string key, IReadOnlyList<int> value);

    void Remove(string key);
}

/// <summary>A settings store that forgets everything when it goes away: tests and previews.</summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, object> values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, object> Values => values;

    public IReadOnlyCollection<string> Keys => values.Keys;

    public bool? GetBool(string key) => values.TryGetValue(key, out var value) && value is bool typed ? typed : null;

    public int? GetInteger(string key) => values.TryGetValue(key, out var value) && value is int typed ? typed : null;

    public double? GetDouble(string key) => values.TryGetValue(key, out var value) && value is double typed ? typed : null;

    public string? GetString(string key) => values.TryGetValue(key, out var value) && value is string typed ? typed : null;

    public IReadOnlyList<string>? GetStrings(string key) =>
        values.TryGetValue(key, out var value) && value is string[] typed ? typed : null;

    public IReadOnlyList<int>? GetIntegers(string key) =>
        values.TryGetValue(key, out var value) && value is int[] typed ? typed : null;

    public void SetBool(string key, bool value) => values[key] = value;

    public void SetInteger(string key, int value) => values[key] = value;

    public void SetDouble(string key, double value) => values[key] = value;

    public void SetString(string key, string value) => values[key] = value;

    public void SetStrings(string key, IReadOnlyList<string> value) => values[key] = value.ToArray();

    public void SetIntegers(string key, IReadOnlyList<int> value) => values[key] = value.ToArray();

    public void Remove(string key) => values.Remove(key);
}
