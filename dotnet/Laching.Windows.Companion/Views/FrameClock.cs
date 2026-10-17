using Microsoft.UI.Dispatching;

namespace Laching.Windows.Companion.Views;

/// <summary>
/// One timer for every moving character on the UI thread, so the characters on screen (Home, the panel, the desktop)
/// move in the same frames: each frame costs a composition commit, whatever moved. 30 frames a second while any
/// character needs them (talking, the spring back from Shh), else 15 for the idle sway and blinks, which is half the CPU
/// and looks the same. Running only while someone listens. Not <c>CompositionTarget.Rendering</c>, which comes every
/// frame the display draws and keeps the UI thread rendering at its rate (measured: half again the CPU). UI thread only.
/// </summary>
internal static class FrameClock
{
    public const double FastSeconds = 1.0 / 30;
    public const double SlowSeconds = 1.0 / 15;

    /// <summary>Each listener, and whether it wants the fast rate.</summary>
    private static readonly Dictionary<Action, bool> Listeners = [];
    private static DispatcherQueueTimer? timer;

    /// <summary>Adds <paramref name="tick"/>, or changes the rate it wants.</summary>
    public static void Add(Action tick, bool fast)
    {
        Listeners[tick] = fast;
        if (timer is null)
        {
            timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.IsRepeating = true;
            timer.Tick += (_, _) =>
            {
                foreach (var listener in Listeners.Keys.ToArray())
                    listener();
            };
        }
        var interval = TimeSpan.FromSeconds(Listeners.ContainsValue(true) ? FastSeconds : SlowSeconds);
        if (timer.Interval != interval)
            timer.Interval = interval;
        if (!timer.IsRunning)
            timer.Start();
    }

    public static void Remove(Action tick)
    {
        Listeners.Remove(tick);
        if (Listeners.Count == 0)
            timer?.Stop();
        else if (timer is not null && !Listeners.ContainsValue(true))
            timer.Interval = TimeSpan.FromSeconds(SlowSeconds);
    }
}
