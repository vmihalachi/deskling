using Microsoft.UI.Dispatching;

namespace Laching.Windows.Companion.Shell;

/// <summary>The UI thread a service posts back to: the one passed in, else the current thread's, else an error saying so.</summary>
internal static class UiThread
{
    /// <summary><paramref name="ui"/>, else the current thread's context; throws when neither is there, naming <paramref name="what"/>.</summary>
    public static SynchronizationContext Resolve(SynchronizationContext? ui, string what) =>
        ui
        ?? SynchronizationContext.Current
        ?? throw new InvalidOperationException($"Create the {what} on the UI thread, or pass its SynchronizationContext.");

    /// <summary><paramref name="queue"/>, else the current thread's queue; throws when neither is there, naming <paramref name="what"/>.</summary>
    public static DispatcherQueue Resolve(DispatcherQueue? queue, string what) =>
        queue
        ?? DispatcherQueue.GetForCurrentThread()
        ?? throw new InvalidOperationException($"Create the {what} on the UI thread, or pass its DispatcherQueue.");
}
