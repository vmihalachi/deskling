using Deskling.Companion.Shell.App;
using Deskling.Core.Input;
using Deskling.Windows.Shell;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// The "say something" shortcut (<see cref="IHotKey"/>) over <see cref="GlobalHotKey"/>: <c>RegisterHotKey</c>, no
/// keyboard hook. Create it on the UI thread; <see cref="OnPress"/> runs there.
/// </summary>
public sealed class HotKey : IHotKey, IDisposable
{
    private readonly GlobalHotKey hotKey;

    /// <param name="windowClassName">The hidden window's class name, unique within the process.</param>
    public HotKey(string windowClassName = "Deskling.Companion.HotKey")
    {
        hotKey = new GlobalHotKey(windowClassName);
        hotKey.Pressed += () => OnPress?.Invoke();
    }

    public Action? OnPress { get; set; }

    public bool Register(KeyShortcut shortcut) => hotKey.Register(shortcut);

    public void Unregister() => hotKey.Register(null);

    public void Dispose() => hotKey.Dispose();
}
