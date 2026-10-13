using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// Press and hold (SwiftUI's <c>onLongPressGesture(minimumDuration: 0.6)</c>), such as the tray panel's hold on the
/// character for an hour of Shh. Touch and pen go through WinUI's <c>Holding</c>, which Windows times itself; the mouse,
/// which raises no <c>Holding</c>, through a timer from the press that a release, a cancel or leaving the element stops.
/// </summary>
public static class HoldGesture
{
    /// <summary>How long the mouse button stays down before it counts as a hold.</summary>
    public const double HoldSeconds = 0.6;

    /// <summary>Calls <paramref name="held"/> on <paramref name="element"/>'s UI thread each time it's held.</summary>
    public static void Attach(UIElement element, Action held)
    {
        element.IsHoldingEnabled = true;
        element.Holding += (_, e) =>
        {
            if (e.HoldingState != HoldingState.Started)
                return;
            e.Handled = true;
            held();
        };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(HoldSeconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            held();
        };
        element.PointerPressed += (_, e) =>
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Mouse || !e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
                return;
            timer.Start();
        };
        void Cancel(object sender, PointerRoutedEventArgs e) => timer.Stop();
        element.PointerReleased += Cancel;
        element.PointerCanceled += Cancel;
        element.PointerCaptureLost += Cancel;
        element.PointerExited += Cancel;
    }
}
