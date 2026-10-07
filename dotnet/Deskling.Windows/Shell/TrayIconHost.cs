using System.Runtime.InteropServices;
using Deskling.Windows.System;
using Microsoft.UI.Dispatching;
using Windows.Graphics;

namespace Deskling.Windows.Shell;

/// <summary>
/// The notification-area icon (<c>Shell_NotifyIcon</c>) and its native menu, opened by a right click or the
/// keyboard's menu key, and by a left click unless <see cref="Selected"/> has a handler. The owner rebuilds the menu
/// with <see cref="SetMenu"/> and the glyph with <see cref="SetGlyph"/>; the glyph is redrawn when the taskbar
/// switches between light and dark and re-added when Explorer restarts. A hidden window on the UI thread receives
/// the icon's messages, so create the host there.
/// </summary>
public sealed class TrayIconHost : IDisposable
{
    private const uint CallbackMessage = NativeMethods.WM_APP + 1;

    /// <summary>Broadcast when Explorer (re)starts, which drops every icon.</summary>
    private static readonly uint TaskbarCreated = NativeMethods.RegisterWindowMessageW("TaskbarCreated");

    private readonly Guid iconId;
    private readonly MessageWindow window;
    private readonly AppearanceWatcher appearance = new();
    private readonly DispatcherQueue dispatcher;
    private IReadOnlyList<TrayMenuItem> menu = [];
    private Func<bool, nint>? glyph;
    private nint hicon;
    private string toolTip = "";
    private bool added;

    /// <param name="dispatcher">The UI thread's queue; menu commands and <see cref="Selected"/> run on it.</param>
    /// <param name="iconId">
    /// Identifies the icon to Windows, which remembers its taskbar settings by this GUID and ties the GUID to one
    /// executable path. Pick one per app (and another for a Dev build) and never change it.
    /// </param>
    /// <param name="windowClassName">The hidden window's class name, unique within the process.</param>
    public TrayIconHost(DispatcherQueue dispatcher, Guid iconId, string windowClassName = "Deskling.Tray")
    {
        this.dispatcher = dispatcher;
        this.iconId = iconId;
        // Hidden and top-level rather than message-only: only top-level windows get TaskbarCreated.
        window = new MessageWindow(windowClassName, messageOnly: false, OnMessage);
        // The taskbar follows Windows' mode (not the app's), so redraw when it changes.
        appearance.Changed += () => dispatcher.TryEnqueue(Redraw);
    }

    /// <summary>
    /// A left click on the icon, with its anchor point in screen pixels (for a flyout). Raised on the UI thread.
    /// With no handler, a left click opens the menu instead.
    /// </summary>
    public event Action<PointInt32>? Selected;

    /// <summary>The small-icon size at the system DPI (16 px at 100 %), for drawing glyphs.</summary>
    public static int SmallIconPixels => Math.Max(16, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSMICON));

    /// <summary>
    /// Shows the icon drawn by <paramref name="draw"/>, which gets whether the taskbar is light and returns an
    /// <c>HICON</c> that this class frees. <paramref name="toolTip"/> is cut to 127 characters.
    /// </summary>
    public void SetGlyph(Func<bool, nint> draw, string toolTip)
    {
        glyph = draw;
        this.toolTip = toolTip;
        Redraw();
    }

    public void SetMenu(IEnumerable<TrayMenuItem> items) => menu = [.. items];

    /// <summary>The icon's bounds in screen pixels, for placing a flyout next to it; false while it isn't shown.</summary>
    public bool TryGetIconRect(out RectInt32 rect)
    {
        var identifier = new NativeMethods.NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONIDENTIFIER>(),
            hWnd = window.Handle,
            guidItem = iconId,
        };
        if (added && NativeMethods.Shell_NotifyIconGetRect(identifier, out var location) == 0)
        {
            rect = new RectInt32(
                location.Left,
                location.Top,
                location.Right - location.Left,
                location.Bottom - location.Top
            );
            return true;
        }
        rect = default;
        return false;
    }

    /// <summary>Opens the menu at a screen point, as a right click does.</summary>
    public void ShowMenu(PointInt32 anchor)
    {
        if (menu.Count == 0 || window.Handle == 0)
            return;
        var commands = new List<Action>();
        var handle = Build(menu, commands);
        var align =
            NativeMethods.GetSystemMetrics(NativeMethods.SM_MENUDROPALIGNMENT) != 0
                ? NativeMethods.TPM_RIGHTALIGN
                : NativeMethods.TPM_LEFTALIGN;
        // The foreground window and the WM_NULL afterwards make the menu close when you click elsewhere.
        NativeMethods.SetForegroundWindow(window.Handle);
        var chosen = NativeMethods.TrackPopupMenuEx(
            handle,
            NativeMethods.TPM_RETURNCMD
                | NativeMethods.TPM_NONOTIFY
                | NativeMethods.TPM_RIGHTBUTTON
                | NativeMethods.TPM_BOTTOMALIGN
                | align,
            anchor.X,
            anchor.Y,
            window.Handle,
            0
        );
        NativeMethods.PostMessageW(window.Handle, NativeMethods.WM_NULL, 0, 0);
        NativeMethods.DestroyMenu(handle);
        // Queued, so a command (Quit, say) never runs inside this window's message handler.
        if (chosen > 0)
            dispatcher.TryEnqueue(() => commands[chosen - 1]());
    }

    public void Dispose()
    {
        appearance.Dispose();
        if (added)
            Notify(NativeMethods.NIM_DELETE, 0);
        window.Dispose();
        DestroyIcon(hicon);
        hicon = 0;
    }

    private void Redraw()
    {
        if (glyph is null)
            return;
        var old = hicon;
        hicon = glyph(AppearanceWatcher.IsTaskbarLight);
        Show();
        DestroyIcon(old);
    }

    /// <summary>Updates the icon and tooltip, adding the icon if it isn't there.</summary>
    private void Show()
    {
        const uint Content = NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP;
        if (window.Handle == 0)
            return;
        if (added && Notify(NativeMethods.NIM_MODIFY, Content))
            return;
        // An icon left behind with our GUID (a crash, say) makes NIM_ADD fail.
        Notify(NativeMethods.NIM_DELETE, 0);
        added =
            Notify(NativeMethods.NIM_ADD, Content | NativeMethods.NIF_MESSAGE)
            && Notify(NativeMethods.NIM_SETVERSION, 0);
    }

    private unsafe bool Notify(uint message, uint flags)
    {
        var data = new NativeMethods.NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NativeMethods.NOTIFYICONDATAW),
            hWnd = window.Handle,
            uFlags = flags | NativeMethods.NIF_GUID,
            uCallbackMessage = CallbackMessage,
            hIcon = hicon,
            uVersion = NativeMethods.NOTIFYICON_VERSION_4,
            guidItem = iconId,
        };
        // 127 characters and the terminator the zeroed buffer already has.
        toolTip.AsSpan(0, Math.Min(toolTip.Length, 127)).CopyTo(new Span<char>(data.szTip, 128));
        return NativeMethods.Shell_NotifyIconW(message, &data);
    }

    private nint? OnMessage(uint message, nint wParam, nint lParam)
    {
        if (message == CallbackMessage)
        {
            // Version 4 puts the event in lParam's low word and the icon's anchor point in wParam.
            var anchor = new PointInt32((short)(wParam & 0xFFFF), (short)((wParam >> 16) & 0xFFFF));
            switch ((uint)(lParam & 0xFFFF))
            {
                case NativeMethods.WM_LBUTTONUP when Selected is { } selected:
                    dispatcher.TryEnqueue(() => selected(anchor));
                    break;
                case NativeMethods.WM_LBUTTONUP:
                case NativeMethods.WM_CONTEXTMENU:
                    ShowMenu(anchor);
                    break;
            }
            return 0;
        }
        if (message == TaskbarCreated && TaskbarCreated != 0)
        {
            added = false;
            Redraw();
            return 0;
        }
        return null;
    }

    /// <summary>A menu for <paramref name="items"/>; a command's ID is its position in <paramref name="commands"/> plus one.</summary>
    private static nint Build(IEnumerable<TrayMenuItem> items, List<Action> commands)
    {
        var handle = NativeMethods.CreatePopupMenu();
        foreach (var item in items)
        {
            switch (item)
            {
                case TrayMenuItem.Command command:
                    commands.Add(command.Invoke);
                    NativeMethods.AppendMenuW(handle, NativeMethods.MF_STRING, (nuint)commands.Count, Escape(command.Text));
                    break;
                case TrayMenuItem.Label label:
                    NativeMethods.AppendMenuW(handle, NativeMethods.MF_STRING | NativeMethods.MF_GRAYED, 0, Escape(label.Text));
                    break;
                case TrayMenuItem.Submenu submenu:
                    var child = Build(submenu.Items, commands);
                    NativeMethods.AppendMenuW(handle, NativeMethods.MF_POPUP, (nuint)child, Escape(submenu.Text));
                    break;
                default:
                    NativeMethods.AppendMenuW(handle, NativeMethods.MF_SEPARATOR, 0, null);
                    break;
            }
        }
        return handle;
    }

    /// <summary>Menus read "&amp;" as an access-key marker.</summary>
    private static string Escape(string text) => text.Replace("&", "&&", StringComparison.Ordinal);

    private static void DestroyIcon(nint icon)
    {
        if (icon != 0)
            NativeMethods.DestroyIcon(icon);
    }
}
