using System.Runtime.InteropServices;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Deskling.Windows.Shell;

/// <summary>
/// Windows app notifications with buttons that route back as <typeparamref name="TAction"/>, also when Windows
/// launched the app to handle the click. One notification at a time: posting replaces the one still in
/// Notification Center. Titles and bodies arrive already localized. One per process, since it owns
/// <c>AppNotificationManager.Default</c>.
/// </summary>
/// <typeparam name="TAction">The app's notion of what a click means (an enum, say).</typeparam>
public sealed class AppNotifier<TAction> : IDisposable
    where TAction : notnull
{
    private const string ActionKey = "action";

    private readonly IReadOnlyDictionary<string, TAction> actions;
    private readonly string defaultAction;
    private readonly string tag;
    private readonly string group;
    private readonly AppNotificationManager manager = AppNotificationManager.Default;

    /// <param name="actions">Action keys (short, stable, ASCII: they travel in the notification's arguments) to actions.</param>
    /// <param name="defaultAction">
    /// The key for a click on the notification itself, and for arguments the map doesn't know; must be in
    /// <paramref name="actions"/>.
    /// </param>
    /// <param name="tag">Identifies the app's notification, so a new one replaces the last.</param>
    /// <param name="group">The notification's group, paired with <paramref name="tag"/>.</param>
    public AppNotifier(IReadOnlyDictionary<string, TAction> actions, string defaultAction, string tag, string group)
    {
        if (!actions.ContainsKey(defaultAction))
            throw new ArgumentException("The default action must be one of the actions.", nameof(defaultAction));
        this.actions = actions;
        this.defaultAction = defaultAction;
        this.tag = tag;
        this.group = group;
    }

    /// <summary>A click on the notification or one of its buttons. Raised on a background thread.</summary>
    public event Action<TAction>? Invoked;

    /// <summary>Notifications are turned off for the app (or entirely), so nothing posted can show.</summary>
    public bool IsDenied => manager.Setting != AppNotificationSetting.Enabled;

    /// <summary>Subscribes and registers with Windows; call once at launch, before posting.</summary>
    public void Register()
    {
        manager.NotificationInvoked += OnInvoked;
        manager.Register();
    }

    /// <summary>The action a notification click carried, for the activation Windows launched the app with.</summary>
    public TAction ActionFrom(AppNotificationActivatedEventArgs args) =>
        args.Arguments.TryGetValue(ActionKey, out var raw) && actions.TryGetValue(raw, out var action)
            ? action
            : actions[defaultAction];

    /// <summary>
    /// Posts the notification, replacing the previous one. Up to five <paramref name="buttons"/>, each carrying one of
    /// the action keys; <paramref name="sound"/> false posts it silently.
    /// </summary>
    public async Task PostAsync(string title, string body, IReadOnlyList<NotificationButton> buttons, bool sound = true)
    {
        var builder = new AppNotificationBuilder()
            .AddArgument(ActionKey, defaultAction)
            .SetTag(tag)
            .SetGroup(group)
            .AddText(title);
        if (body.Length > 0)
            builder.AddText(body);
        foreach (var button in buttons)
            builder.AddButton(new AppNotificationButton(button.Title).AddArgument(ActionKey, button.Action));
        if (!sound)
            builder.MuteAudio();

        await RemoveAsync();
        manager.Show(builder.BuildNotification());
    }

    /// <summary>Takes the posted notification out of Notification Center, if it is still there.</summary>
    public async Task RemoveAsync()
    {
        try
        {
            await manager.RemoveByTagAndGroupAsync(tag, group);
        }
        catch (COMException)
        {
            // Nothing to remove.
        }
    }

    public void Dispose()
    {
        manager.NotificationInvoked -= OnInvoked;
        manager.Unregister();
    }

    private void OnInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) =>
        Invoked?.Invoke(ActionFrom(args));
}
