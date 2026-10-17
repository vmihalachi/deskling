using Laching.Companion.Shell.Speech;
using Microsoft.UI.Dispatching;

namespace Laching.Windows.Companion.Shell;

/// <summary>
/// <see cref="ITimers"/> over <see cref="DispatcherQueueTimer"/>s, so work runs on the UI thread between frames (the
/// model's tick, a punchline's beat, the busy watch, the glyph's frames). Create it on the UI thread or pass its queue.
/// </summary>
public sealed class DispatcherTimers : ITimers
{
    private readonly DispatcherQueue queue;

    /// <param name="queue">The UI thread's queue; the current thread's by default.</param>
    public DispatcherTimers(DispatcherQueue? queue = null)
    {
        this.queue =
            queue
            ?? DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("Create the timers on the UI thread, or pass its DispatcherQueue.");
    }

    public IDisposable After(double seconds, Action work) => Start(seconds, repeating: false, work);

    public IDisposable Every(double seconds, Action work) => Start(seconds, repeating: true, work);

    private Handle Start(double seconds, bool repeating, Action work)
    {
        var timer = queue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(Math.Max(0, seconds));
        timer.IsRepeating = repeating;
        var handle = new Handle(timer);
        timer.Tick += (_, _) =>
        {
            // A tick already queued when the handle was disposed is dropped.
            if (handle.IsDisposed)
                return;
            if (!repeating)
                handle.Dispose();
            work();
        };
        timer.Start();
        return handle;
    }

    private sealed class Handle(DispatcherQueueTimer timer) : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            timer.Stop();
        }
    }
}
