using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ShellNative = Laching.Windows.Companion.Shell.NativeMethods;

namespace Laching.Windows.Companion.Desktop;

/// <summary>
/// A system backdrop that is a fully transparent brush, so only the content draws (with the window's
/// <c>WS_EX_LAYERED</c>, its per-pixel alpha reaches the desktop). The system compositor needs a
/// <c>Windows.System.DispatcherQueue</c>, which WinUI's UI thread lacks: one is made the first time.
/// </summary>
internal sealed partial class TransparentBackdrop : SystemBackdrop
{
    private static global::Windows.UI.Composition.Compositor? compositor;

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        if (compositor is null)
        {
            if (global::Windows.System.DispatcherQueue.GetForCurrentThread() is null)
            {
                // The current thread (DQTYPE_THREAD_CURRENT), single-threaded apartment (DQTAT_COM_STA).
                var options = new ShellNative.DispatcherQueueOptions { dwSize = 12, threadType = 2, apartmentType = 2 };
                ShellNative.CreateDispatcherQueueController(options, out _);
            }
            compositor = new global::Windows.UI.Composition.Compositor();
        }
        connectedTarget.SystemBackdrop = compositor.CreateColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    /// <summary>
    /// Nothing to do: the brush is transparent in every theme and contrast theme. The base class's handler, which WinUI
    /// calls when Windows switches theme or contrast, throws E_INVALIDARG for this target and ended the app.
    /// </summary>
    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot) { }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        disconnectedTarget.SystemBackdrop = null;
        base.OnTargetDisconnected(disconnectedTarget);
    }
}
