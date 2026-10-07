namespace Deskling.Windows.Shell;

/// <summary>A row of the tray menu (see <see cref="TrayIconHost.SetMenu"/>). Text arrives already localized.</summary>
public abstract record TrayMenuItem
{
    /// <summary>A clickable row. <see cref="TrayIconHost"/> runs it on the UI thread after the menu closes.</summary>
    public sealed record Command(string Text, Action Invoke) : TrayMenuItem;

    /// <summary>A dimmed row that only shows text, like a status line.</summary>
    public sealed record Label(string Text) : TrayMenuItem;

    public sealed record Separator : TrayMenuItem;

    public sealed record Submenu(string Text, IReadOnlyList<TrayMenuItem> Items) : TrayMenuItem;
}
