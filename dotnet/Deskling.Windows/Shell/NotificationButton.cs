namespace Deskling.Windows.Shell;

/// <summary>
/// A button on a notification: its localized title and the action key it carries back (see <see cref="AppNotifier{TAction}"/>).
/// </summary>
public readonly record struct NotificationButton(string Title, string Action);
