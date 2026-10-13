using Microsoft.UI.Dispatching;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// One 30 fps timer for every moving character on the UI thread, so the characters on screen (Home, the panel, the
/// desktop) move in the same frames: each frame costs a composition commit, whatever moved. Running only while someone
/// listens. Not <c>CompositionTarget.Rendering</c>, which comes every frame the display draws and keeps the UI thread
/// rendering at its rate (measured: half again the CPU). UI thread only.
/// </summary>
internal static class FrameClock
{
    public const double FrameSeconds = 1.0 / 30;

    private static readonly List<Action> Listeners = [];
    private static DispatcherQueueTimer? timer;

    public static void Add(Action tick)
    {
        if (Listeners.Contains(tick))
            return;
        Listeners.Add(tick);
        if (timer is null)
        {
            timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(FrameSeconds);
            timer.IsRepeating = true;
            timer.Tick += (_, _) =>
            {
                foreach (var listener in Listeners.ToArray())
                    listener();
            };
        }
        if (!timer.IsRunning)
            timer.Start();
    }

    public static void Remove(Action tick)
    {
        Listeners.Remove(tick);
        if (Listeners.Count == 0)
            timer?.Stop();
    }
}
