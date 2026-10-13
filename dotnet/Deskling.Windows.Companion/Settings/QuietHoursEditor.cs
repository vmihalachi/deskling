using System.Globalization;
using Deskling.Companion.Settings;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Deskling.Windows.Companion.Settings;

/// <summary>
/// The 24 hours as chips in six columns (Swift's <c>QuietHoursEditor</c>); a chip that is on is an hour without a word.
/// The title is the app's; the chips take its <see cref="ChipStyle"/>.
/// </summary>
public sealed partial class QuietHoursEditor : UserControl
{
    private readonly TextBlock title = new();
    private readonly ToggleButton[] chips = new ToggleButton[24];
    private readonly SettingsWatch watch;
    private CompanionSettings? settings;

    public QuietHoursEditor()
    {
        IsTabStop = false;
        var grid = new Grid { ColumnSpacing = Space.Tight, RowSpacing = Space.Tight };
        for (var column = 0; column < 6; column++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var row = 0; row < 4; row++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var hour = 0; hour < 24; hour++)
        {
            var h = hour;
            var chip = new ToggleButton { Content = Label(hour, CultureInfo.CurrentCulture), HorizontalAlignment = HorizontalAlignment.Stretch };
            chip.Click += (_, _) => SetHour(h, chip.IsChecked == true);
            Grid.SetColumn(chip, hour % 6);
            Grid.SetRow(chip, hour / 6);
            grid.Children.Add(chip);
            chips[hour] = chip;
        }
        var stack = new StackPanel { Spacing = Space.Snug };
        stack.Children.Add(title);
        stack.Children.Add(grid);
        Content = stack;
        watch = new SettingsWatch(this, () => settings, Refresh, nameof(CompanionSettings.QuietHours));
    }

    public CompanionSettings? Settings
    {
        get => settings;
        set
        {
            settings = value;
            watch.Reset();
        }
    }

    public string Title
    {
        get => title.Text;
        set => title.Text = value;
    }

    /// <summary>The chips' style (a <c>ToggleButton</c> style); WinUI's toggle button when null.</summary>
    public Style? ChipStyle
    {
        get;
        set
        {
            field = value;
            foreach (var chip in chips)
                chip.Style = value;
        }
    }

    /// <summary>"3 PM" or "15", as <paramref name="culture"/> writes hours: its short time pattern without the minutes.</summary>
    public static string Label(int hour, CultureInfo culture)
    {
        var pattern = culture.DateTimeFormat.ShortTimePattern;
        var designator = pattern.Contains('t');
        // The hour as the pattern writes it: 12- or 24-hour, with or without a leading zero.
        var hourPart =
            pattern.Contains("HH") ? "HH"
            : pattern.Contains('H') ? "%H"
            : pattern.Contains("hh") ? "hh"
            : "%h";
        var text = new DateTime(2000, 1, 1, hour, 0, 0).ToString(hourPart, culture);
        if (!designator)
            return text;
        var ampm = new DateTime(2000, 1, 1, hour, 0, 0).ToString("tt", culture);
        // Most cultures put the designator after the hour; a pattern starting with it (zh, ko, ja) puts it first.
        return pattern.TrimStart().StartsWith('t') ? $"{ampm} {text}".Trim() : $"{text} {ampm}".Trim();
    }

    private void SetHour(int hour, bool on)
    {
        if (settings is null)
            return;
        var hours = new HashSet<int>(settings.QuietHours);
        if (on ? hours.Add(hour) : hours.Remove(hour))
            settings.QuietHours = hours;
    }

    private void Refresh()
    {
        if (settings is not { } s)
            return;
        for (var hour = 0; hour < 24; hour++)
            chips[hour].IsChecked = s.QuietHours.Contains(hour);
    }
}
