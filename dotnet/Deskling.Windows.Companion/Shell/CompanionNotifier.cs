using System.Runtime.InteropServices;
using Deskling.Companion.Shell.App;
using Deskling.Windows.Shell;
using Microsoft.Windows.AppNotifications;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// The character's line as a Windows notification (<see cref="ICompanionNotifying"/>, Swift's <c>CompanionNotifier</c>)
/// over <see cref="AppNotifier{TAction}"/>, with the app's buttons: each action id travels in the notification and
/// comes back through <see cref="OnAction"/> on the UI thread. One tag, so a new line replaces the last instead of piling
/// up in Notification Center. Posted silently. A click on the banner itself does nothing but bring Windows' focus (as on
/// the Mac). Windows never asks for permission: it is on unless the user turned the app's notifications off.
/// </summary>
public sealed class CompanionNotifier : ICompanionNotifying, IDisposable
{
    /// <summary>The banner's own click, which no button carries.</summary>
    private const string BannerAction = "banner";

    private readonly AppNotifier<string> notifier;
    private readonly string title;
    private readonly IReadOnlyList<NotificationButton> buttons;
    private readonly SynchronizationContext ui;
    private bool available = true;

    /// <param name="title">The notification's first line (the app's or the character's name), already localized.</param>
    /// <param name="actions">The buttons, in order: action id (short, stable, ASCII; not "banner") and localized title.</param>
    /// <param name="tag">Identifies the app's notification, so a new one replaces the last.</param>
    /// <param name="ui">Where <see cref="OnAction"/> runs; the current thread's context by default.</param>
    public CompanionNotifier(
        string title,
        IReadOnlyList<(string Id, string Title)> actions,
        string tag = "line",
        SynchronizationContext? ui = null
    )
    {
        this.title = title;
        this.ui =
            ui
            ?? SynchronizationContext.Current
            ?? throw new InvalidOperationException("Create the notifier on the UI thread, or pass its SynchronizationContext.");
        buttons = [.. actions.Select(action => new NotificationButton(action.Title, action.Id))];
        var keys = actions.ToDictionary(action => action.Id, action => action.Id);
        keys[BannerAction] = BannerAction;
        notifier = new AppNotifier<string>(keys, BannerAction, tag, group: tag);
        notifier.Invoked += action => this.ui.Post(_ => Raise(action), null);
    }

    public Action<string>? OnAction { get; set; }

    /// <summary>Registers with Windows; a process Windows won't register (no identity it accepts) posts nothing.</summary>
    public void Configure()
    {
        try
        {
            notifier.Register();
        }
        catch (COMException)
        {
            available = false;
        }
    }

    /// <summary>The click Windows launched the app with (it wasn't running), as an action.</summary>
    public void Activated(AppNotificationActivatedEventArgs args) => Raise(notifier.ActionFrom(args));

    public void RequestAuthorization(Action<bool> completion) => completion(IsAllowed);

    public void RefreshAuthorization(Action<NotificationPermission> completion) =>
        completion(IsAllowed ? NotificationPermission.Authorized : NotificationPermission.Denied);

    public void Post(string line)
    {
        if (available)
            _ = PostAsync(line);
    }

    public void Dispose() => notifier.Dispose();

    private bool IsAllowed => available && !notifier.IsDenied;

    private async Task PostAsync(string line)
    {
        try
        {
            await notifier.PostAsync(title, line, buttons, sound: false);
        }
        catch (COMException)
        {
            // Notifications switched off between the check and the post: the line goes unseen, as it would on the Mac.
        }
    }

    private void Raise(string action)
    {
        if (action != BannerAction)
            OnAction?.Invoke(action);
    }
}
