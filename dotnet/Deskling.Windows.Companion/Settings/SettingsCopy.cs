using System.ComponentModel;
using Deskling.Companion.Phrases;
using Deskling.Companion.Rules;
using Deskling.Companion.Settings;
using Microsoft.UI.Xaml;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// The active hours editor's words: the row's label, the two pickers' labels (each on its own row, as two WinUI time
/// pickers don't fit side by side), the days' label.
/// </summary>
public sealed record ActiveHoursCopy(string ActiveHours, string From, string To, string Days);

/// <summary>
/// The language row's words: its label, the choice that follows Windows (first in the list), the note and button shown
/// once the language changed, as it applies at the next start, and the line shown when Windows refuses the restart
/// (<see cref="RestartRefused"/>: null shows nothing).
/// </summary>
public sealed record LanguageCopy(string Label, string SystemLanguage, string RestartNote, string Restart, string? RestartRefused = null);

/// <summary>The startup toggle's label, and the line shown when Windows (the user, or a policy) blocks it.</summary>
public sealed record LaunchAtStartupCopy(string Label, string Blocked);

/// <summary>The note shown while notifications are off for the app, and its button to Windows' settings.</summary>
public sealed record NotificationNoteCopy(string Text, string Open);

/// <summary>Swift's <c>CompanionSettings+Bindings</c>: the reads and writes the Settings rows make.</summary>
public static class SettingsBindings
{
    public static bool IsOn(this CompanionSettings settings, Channels channel) => settings.Channels.Contains(channel);

    /// <summary>Whether lines of <paramref name="flavor"/> are picked.</summary>
    public static bool IsOn(this CompanionSettings settings, FlavorId flavor) => settings.PhraseFlavors.Contains(flavor);

    public static void SetFlavor(this CompanionSettings settings, FlavorId flavor, bool on)
    {
        var flavors = new HashSet<FlavorId>(settings.PhraseFlavors);
        if (on ? flavors.Add(flavor) : flavors.Remove(flavor))
            settings.PhraseFlavors = flavors;
    }

    /// <summary>The muted lines still in <paramref name="bank"/> (a retired line no longer counts).</summary>
    public static int MutedCount(this CompanionSettings settings, PhraseBank bank) => settings.MutedPhraseIDs.Count(id => bank[id] is not null);
}

/// <summary>
/// A control's watch on the settings: <c>refresh</c> runs when the control loads and when one of
/// <c>properties</c> changes while it is loaded (settings outlive their windows, so the handler goes when the control
/// does).
/// </summary>
internal sealed class SettingsWatch
{
    private readonly Func<CompanionSettings?> settings;
    private readonly HashSet<string> properties;
    private readonly Action refresh;
    private CompanionSettings? watched;

    public SettingsWatch(FrameworkElement owner, Func<CompanionSettings?> settings, Action refresh, params string[] properties)
    {
        this.settings = settings;
        this.properties = [.. properties];
        this.refresh = refresh;
        owner.Loaded += (_, _) => Watch();
        owner.Unloaded += (_, _) => Unwatch();
    }

    /// <summary>The settings changed: watch the new ones (if loaded) and refresh.</summary>
    public void Reset()
    {
        if (watched is not null)
        {
            Unwatch();
            Watch();
        }
        else
            refresh();
    }

    private void Watch()
    {
        watched = settings();
        if (watched is not null)
            watched.PropertyChanged += OnChanged;
        refresh();
    }

    private void Unwatch()
    {
        if (watched is not null)
            watched.PropertyChanged -= OnChanged;
        watched = null;
    }

    private void OnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null || properties.Contains(e.PropertyName))
            refresh();
    }
}
