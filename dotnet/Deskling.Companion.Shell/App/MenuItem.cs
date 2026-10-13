namespace Deskling.Companion.Shell.App;

/// <summary>
/// One entry of a menu as data (<see cref="CompanionMenus"/> builds them; the platform turns them into its native menu):
/// a command, a submenu or a separator.
/// </summary>
public abstract record MenuItem
{
    private MenuItem() { }

    /// <summary>The words shown; empty for a separator (as a native separator's title).</summary>
    public abstract string Title { get; }

    /// <summary>Runs <see cref="Invoke"/>; a check mark when <see cref="IsChecked"/>; greyed out unless <see cref="IsEnabled"/>.</summary>
    public sealed record Command(string Title, Action Invoke, bool IsChecked = false, bool IsEnabled = true) : MenuItem
    {
        public override string Title { get; } = Title;
    }

    public sealed record Submenu(string Title, IReadOnlyList<MenuItem> Items) : MenuItem
    {
        public override string Title { get; } = Title;
    }

    public sealed record Separator : MenuItem
    {
        public override string Title => "";
    }
}
