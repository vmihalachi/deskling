using System.Globalization;
using System.Runtime.InteropServices;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using ApplicationLanguages = Microsoft.Windows.Globalization.ApplicationLanguages;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// The language the app speaks (Swift's <c>LanguageRow</c>). The Mac sends the user to System Settings' per-app list;
/// Windows has no such page for every app, so this is a list in the app: Windows' language first, then each of the
/// app's in its own words ("Deutsch", "Português (Brasil)"), through the Windows App SDK's
/// <c>ApplicationLanguages.PrimaryLanguageOverride</c> (packaged or not). Windows applies a new language at the next
/// start, so a note and a restart button show once it changed: Narrator hears the note as it appears, and again as the
/// button's help text. The words are the app's; the button takes its <see cref="ButtonStyle"/>.
/// </summary>
public sealed partial class LanguageRow : UserControl
{
    private static readonly string StartLanguage = SafeOverride();

    private readonly TextBlock label = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox choices = new() { MinWidth = 160, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly TextBlock note = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    private readonly Button restart = new();
    private readonly Grid restartRow = new() { ColumnSpacing = Space.Item, Visibility = Visibility.Collapsed };
    private IReadOnlyList<string> tags = [];
    private bool updating;

    public LanguageRow()
    {
        IsTabStop = false;
        var row = new Grid { ColumnSpacing = Space.Item };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(label);
        Grid.SetColumn(choices, 1);
        row.Children.Add(choices);
        restartRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        restartRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        restartRow.Children.Add(note);
        Grid.SetColumn(restart, 1);
        restartRow.Children.Add(restart);
        var stack = new StackPanel { Spacing = Space.Snug };
        stack.Children.Add(row);
        stack.Children.Add(restartRow);
        Content = stack;
        choices.SelectionChanged += (_, _) => Choose();
        restart.Click += (_, _) => (OnRestart ?? DefaultRestart)();
        LookBinding.Keep(this, () => note.Foreground = LookBinding.Brush(this, p => p.InkSoft));
    }

    /// <summary>The languages the app has, as BCP-47 tags ("en", "de", "pt-BR"); the list follows this order after Windows' own.</summary>
    public IReadOnlyList<string> Languages
    {
        get => tags;
        set
        {
            tags = value;
            Rebuild();
        }
    }

    public LanguageCopy? Copy
    {
        get;
        set
        {
            field = value;
            label.Text = value?.Label ?? "";
            AutomationProperties.SetName(choices, value?.Label ?? "");
            note.Text = value?.RestartNote ?? "";
            restart.Content = value?.Restart;
            // The button says why it's there: the note beside it.
            AutomationProperties.SetHelpText(restart, value?.RestartNote ?? "");
            Rebuild();
        }
    }

    public Style? ButtonStyle
    {
        get => restart.Style;
        set => restart.Style = value;
    }

    /// <summary>Restarts the app in its new language; Windows App SDK's <c>AppInstance.Restart</c> when unset.</summary>
    public Action? OnRestart { get; set; }

    /// <summary>A language's name in its own words, capitalized as a list item ("Deutsch", "Español"); the tag itself when unknown.</summary>
    public static string NativeName(string tag)
    {
        try
        {
            // Predefined only: otherwise any tag makes up a culture named after itself.
            var culture = CultureInfo.GetCultureInfo(tag, predefinedOnly: true);
            var name = culture.NativeName;
            return name.Length == 0 ? tag : culture.TextInfo.ToUpper(name[0]) + name[1..];
        }
        catch (CultureNotFoundException)
        {
            return tag;
        }
    }

    private void Rebuild()
    {
        updating = true;
        choices.Items.Clear();
        choices.Items.Add(Copy?.SystemLanguage ?? "");
        foreach (var tag in tags)
            choices.Items.Add(NativeName(tag));
        var current = SafeOverride();
        var index = tags.ToList().FindIndex(t => string.Equals(t, current, StringComparison.OrdinalIgnoreCase));
        choices.SelectedIndex = index + 1;
        updating = false;
        UpdateRestart();
    }

    private void Choose()
    {
        if (updating || choices.SelectedIndex < 0)
            return;
        var tag = choices.SelectedIndex == 0 ? "" : tags[choices.SelectedIndex - 1];
        try
        {
            ApplicationLanguages.PrimaryLanguageOverride = tag;
        }
        catch (COMException) { }
        UpdateRestart();
    }

    private void UpdateRestart()
    {
        var before = restartRow.Visibility;
        restartRow.Visibility = string.Equals(SafeOverride(), StartLanguage, StringComparison.OrdinalIgnoreCase) ? Visibility.Collapsed : Visibility.Visible;
        if (Announcer.Appears(before, restartRow.Visibility))
            Announcer.Say(note, Copy?.RestartNote, Announcer.Note);
    }

    private static string SafeOverride()
    {
        try
        {
            return ApplicationLanguages.PrimaryLanguageOverride ?? "";
        }
        catch (COMException)
        {
            return "";
        }
    }

    private static void DefaultRestart() => AppInstance.Restart("");
}
