using System.Globalization;

namespace Deskling.Core.Input;

/// <summary>
/// A global keyboard shortcut: a key code plus Carbon modifier flags, the same shape on both platforms. On macOS
/// the key code is a Carbon virtual key code; on Windows it's a Win32 virtual-key code and the Windows package maps
/// the modifier masks onto <c>RegisterHotKey</c>'s <c>MOD_*</c> flags (command ↔ Win, option ↔ Alt, control ↔ Ctrl,
/// shift ↔ Shift). Apps persist it as the text of <see cref="Serialize"/> (the Swift package uses Codable).
/// </summary>
public sealed record KeyShortcut(uint KeyCode, uint Modifiers, string Key)
{
    // Carbon modifier masks (cmdKey, shiftKey, optionKey, controlKey).
    public const uint Command = 0x0100;
    public const uint Shift = 0x0200;
    public const uint Option = 0x0800;
    public const uint Control = 0x1000;

    /// <summary>
    /// The shortcut in the Mac menu bar's notation, e.g. "⌃⌥⌘B": control, option, shift, command, then
    /// <see cref="Key"/> (what the key looked like when it was recorded, e.g. "B" or "F5").
    /// </summary>
    public string DisplayString
    {
        get
        {
            var s = "";
            if ((Modifiers & Control) != 0)
                s += "⌃";
            if ((Modifiers & Option) != 0)
                s += "⌥";
            if ((Modifiers & Shift) != 0)
                s += "⇧";
            if ((Modifiers & Command) != 0)
                s += "⌘";
            return s + Key;
        }
    }

    /// <summary>Needs ⌘, ⌃ or ⌥ (Win, Ctrl or Alt on Windows) so it can't swallow ordinary typing.</summary>
    public bool IsValid => (Modifiers & (Command | Control | Option)) != 0;

    /// <summary>"keyCode:modifiers:key" in decimal, e.g. "11:4352:B" for ⌃⌘B.</summary>
    public string Serialize() => string.Create(CultureInfo.InvariantCulture, $"{KeyCode}:{Modifiers}:{Key}");

    /// <summary>The inverse of <see cref="Serialize"/>: null for anything malformed or not <see cref="IsValid"/>.</summary>
    public static KeyShortcut? Parse(string? text)
    {
        var parts = text?.Split(':', 3);
        if (
            parts is not [var keyCode, var modifiers, var key]
            || !uint.TryParse(keyCode, NumberStyles.None, CultureInfo.InvariantCulture, out var code)
            || !uint.TryParse(modifiers, NumberStyles.None, CultureInfo.InvariantCulture, out var mods)
        )
            return null;
        var shortcut = new KeyShortcut(code, mods, key);
        return shortcut.IsValid ? shortcut : null;
    }
}
