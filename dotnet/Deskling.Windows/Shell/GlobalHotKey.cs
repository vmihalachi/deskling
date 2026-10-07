using Deskling.Core.Input;

namespace Deskling.Windows.Shell;

/// <summary>
/// One system-wide shortcut through <c>RegisterHotKey</c>: no keyboard hook, no permission. A message-only window
/// on the thread that creates the hot key (the UI thread, which pumps messages) receives <c>WM_HOTKEY</c> and raises
/// <see cref="Pressed"/> there. <see cref="KeyShortcut"/> keeps the Mac shape (Carbon modifier masks plus a key code
/// that is a Win32 virtual-key code on Windows); <see cref="ToWin32Modifiers"/> maps command ↔ Win, option ↔ Alt,
/// control ↔ Ctrl and shift ↔ Shift onto <c>RegisterHotKey</c>'s <c>MOD_*</c> flags.
/// </summary>
public sealed class GlobalHotKey : IDisposable
{
    private const int Id = 1;

    private readonly MessageWindow window;
    private bool registered;

    /// <param name="windowClassName">The message-only window's class name, unique within the process.</param>
    public GlobalHotKey(string windowClassName = "Deskling.HotKey")
    {
        window = new MessageWindow(windowClassName, messageOnly: true, OnMessage);
    }

    /// <summary>The shortcut was pressed. Raised on the thread that created the hot key.</summary>
    public event Action? Pressed;

    /// <summary>
    /// Replaces the shortcut; null suspends it. Returns false when another app owns the combination (the old
    /// shortcut is gone then too). A shortcut that isn't <see cref="KeyShortcut.IsValid"/> is ignored.
    /// </summary>
    public bool Register(KeyShortcut? shortcut)
    {
        if (registered)
        {
            NativeMethods.UnregisterHotKey(window.Handle, Id);
            registered = false;
        }
        if (shortcut is null || !shortcut.IsValid || window.Handle == 0)
            return true;
        registered = NativeMethods.RegisterHotKey(
            window.Handle,
            Id,
            ToWin32Modifiers(shortcut.Modifiers) | NativeMethods.MOD_NOREPEAT,
            shortcut.KeyCode
        );
        return registered;
    }

    /// <summary>Carbon modifier masks → <c>MOD_ALT</c>, <c>MOD_CONTROL</c>, <c>MOD_SHIFT</c>, <c>MOD_WIN</c>.</summary>
    public static uint ToWin32Modifiers(uint carbonModifiers)
    {
        uint mods = 0;
        if ((carbonModifiers & KeyShortcut.Option) != 0)
            mods |= NativeMethods.MOD_ALT;
        if ((carbonModifiers & KeyShortcut.Control) != 0)
            mods |= NativeMethods.MOD_CONTROL;
        if ((carbonModifiers & KeyShortcut.Shift) != 0)
            mods |= NativeMethods.MOD_SHIFT;
        if ((carbonModifiers & KeyShortcut.Command) != 0)
            mods |= NativeMethods.MOD_WIN;
        return mods;
    }

    /// <summary>The inverse of <see cref="ToWin32Modifiers"/>, for a shortcut recorder reading key state.</summary>
    public static uint FromWin32Modifiers(uint win32Modifiers)
    {
        uint mods = 0;
        if ((win32Modifiers & NativeMethods.MOD_ALT) != 0)
            mods |= KeyShortcut.Option;
        if ((win32Modifiers & NativeMethods.MOD_CONTROL) != 0)
            mods |= KeyShortcut.Control;
        if ((win32Modifiers & NativeMethods.MOD_SHIFT) != 0)
            mods |= KeyShortcut.Shift;
        if ((win32Modifiers & NativeMethods.MOD_WIN) != 0)
            mods |= KeyShortcut.Command;
        return mods;
    }

    /// <summary>
    /// The keyboard layout's name for a virtual key ("B", "F5", "Strg"), from <c>GetKeyNameText</c>, for showing a
    /// shortcut; null when the layout has none. Letters, digits and function keys come back as themselves.
    /// </summary>
    public static unsafe string? KeyName(int virtualKey)
    {
        switch (virtualKey)
        {
            case >= 0x41 and <= 0x5A:
            case >= 0x30 and <= 0x39:
                return ((char)virtualKey).ToString();
            case >= 0x70 and <= 0x87:
                return $"F{virtualKey - 0x70 + 1}";
        }
        var scan = NativeMethods.MapVirtualKeyW((uint)virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
        if (scan == 0)
            return null;
        // Navigation keys, Insert, Delete, the keypad's divide and Num Lock are the layout's extended keys.
        var extended = virtualKey is (>= 0x21 and <= 0x28) or 0x2D or 0x2E or 0x6F or 0x90;
        var lParam = (int)(scan << 16) | (extended ? 1 << 24 : 0);
        var buffer = stackalloc char[64];
        var length = NativeMethods.GetKeyNameTextW(lParam, buffer, 64);
        return length > 0 ? new string(buffer, 0, length) : null;
    }

    public void Dispose()
    {
        Register(null);
        window.Dispose();
    }

    private nint? OnMessage(uint message, nint wParam, nint lParam)
    {
        if (message != NativeMethods.WM_HOTKEY || wParam != Id)
            return null;
        Pressed?.Invoke();
        return 0;
    }
}
