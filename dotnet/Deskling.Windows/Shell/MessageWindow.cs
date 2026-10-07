using System.Runtime.InteropServices;

namespace Deskling.Windows.Shell;

/// <summary>
/// A hidden window whose messages go to a handler, for the services Windows talks to through window messages
/// (hot keys, the tray icon, session changes). Each class name is registered once per process and every window
/// of it routes through one static procedure that dispatches by handle, so a class never points at a procedure
/// that has gone away. Messages arrive on the thread that created the window, which has to pump them.
/// </summary>
internal sealed class MessageWindow : IDisposable
{
    /// <summary>Handles one message; null lets <c>DefWindowProc</c> have it.</summary>
    public delegate nint? Handler(uint message, nint wParam, nint lParam);

    // Static, so the function pointer registered with every class stays valid for the life of the process.
    private static readonly NativeMethods.WndProc Procedure = WindowProc;
    private static readonly nint ProcedurePointer = Marshal.GetFunctionPointerForDelegate(Procedure);
    private static readonly HashSet<string> RegisteredClasses = [];
    private static readonly Dictionary<nint, MessageWindow> Instances = [];
    private static readonly object Gate = new();

    private readonly Handler handler;

    /// <summary>The window's <c>HWND</c>, or 0 when Windows refused to create it.</summary>
    public nint Handle { get; private set; }

    /// <param name="className">The window class, unique within the process.</param>
    /// <param name="messageOnly">
    /// A message-only window (parent <c>HWND_MESSAGE</c>) is cheapest but gets no broadcasts such as
    /// <c>TaskbarCreated</c>; false makes a hidden top-level window instead.
    /// </param>
    /// <param name="handler">Runs on the creating thread for every message after creation.</param>
    public MessageWindow(string className, bool messageOnly, Handler handler)
    {
        this.handler = handler;
        var instance = NativeMethods.GetModuleHandleW(null);
        RegisterClass(className, instance);
        Handle = NativeMethods.CreateWindowExW(
            0,
            className,
            "",
            0,
            0,
            0,
            0,
            0,
            messageOnly ? NativeMethods.HWND_MESSAGE : 0,
            0,
            instance,
            0
        );
        if (Handle == 0)
            return;
        lock (Gate)
            Instances[Handle] = this;
    }

    public void Dispose()
    {
        if (Handle == 0)
            return;
        lock (Gate)
            Instances.Remove(Handle);
        NativeMethods.DestroyWindow(Handle);
        Handle = 0;
    }

    private static void RegisterClass(string className, nint instance)
    {
        lock (Gate)
        {
            if (!RegisteredClasses.Add(className))
                return;
        }
        // Windows copies the name into the class atom, so the buffer is only needed during the call.
        var name = Marshal.StringToHGlobalUni(className);
        try
        {
            var windowClass = new NativeMethods.WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEXW>(),
                lpfnWndProc = ProcedurePointer,
                hInstance = instance,
                lpszClassName = name,
            };
            NativeMethods.RegisterClassExW(windowClass);
        }
        finally
        {
            Marshal.FreeHGlobal(name);
        }
    }

    private static nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        MessageWindow? window;
        lock (Gate)
            Instances.TryGetValue(hwnd, out window);
        // Messages sent while CreateWindowExW is still running (WM_NCCREATE, WM_CREATE) take the default path.
        return window?.handler(message, wParam, lParam) ?? NativeMethods.DefWindowProcW(hwnd, message, wParam, lParam);
    }
}
