using System.Globalization;
using Laching.Companion.Settings;
using Laching.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using XamlRect = Windows.Foundation.Rect;
using XamlSize = Windows.Foundation.Size;

namespace Laching.Windows.Companion.Settings;

/// <summary>
/// The 24 hours as chips in six columns (Swift's <c>QuietHoursEditor</c>), or in four or three when the widest chip
/// doesn't fit a sixth of the width (<see cref="Columns"/>: a 12-hour culture's "10 AM", a large text size); a chip that
/// is on is an hour without a word. The title is the app's; the chips take its <see cref="ChipStyle"/>. Narrator reads
/// the editor as a group named by the title and each chip as its hour in full ("15:00", "3:00 PM"). The chips are one
/// Tab stop, the arrow keys moving between them.
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
        var grid = new ChipGrid();
        for (var hour = 0; hour < 24; hour++)
        {
            var h = hour;
            var chip = HighlightFill.Plain(
                new ToggleButton { Content = Label(hour, CultureInfo.CurrentCulture), HorizontalAlignment = HorizontalAlignment.Stretch }
            );
            AutomationProperties.SetName(chip, HourName(hour, CultureInfo.CurrentCulture));
            chip.Click += (_, _) => SetHour(h, chip.IsChecked == true);
            grid.Children.Add(chip);
            chips[hour] = chip;
        }
        // One Tab stop for the 24 chips, the arrows moving between them.
        grid.TabFocusNavigation = KeyboardNavigationMode.Once;
        grid.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
        // The group carries the title for Narrator.
        AutomationProperties.SetAccessibilityView(title, AccessibilityView.Raw);
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

    /// <summary>The editor's title, shown over the chips and read by Narrator as the group's name.</summary>
    public string Title
    {
        get => title.Text;
        set
        {
            title.Text = value;
            AutomationProperties.SetName(this, value);
        }
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

    /// <summary>
    /// How many columns the chips take in <paramref name="available"/> DIPs, <see cref="Space.Tight"/> apart: six, or four
    /// or three (so the 24 hours fill whole rows), the most whose columns are at least <paramref name="widest"/> wide;
    /// three when none are.
    /// </summary>
    public static int Columns(double available, double widest)
    {
        bool Fits(int columns) => (available - (columns - 1) * Space.Tight) / columns >= widest;
        return Fits(6) ? 6 : Fits(4) ? 4 : 3;
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

    /// <summary>A chip's name for Narrator: the hour as <paramref name="culture"/> writes a time ("15:00", "3:00 PM").</summary>
    public static string HourName(int hour, CultureInfo culture) => new DateTime(2000, 1, 1, hour, 0, 0).ToString("t", culture);

    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this, nameof(QuietHoursEditor));

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

    /// <summary>
    /// The chips in rows of <see cref="Columns"/> equal columns, <see cref="Space.Tight"/> apart both ways. The widest
    /// chip is measured as it would draw, with the chip style's padding and the user's text size, on every layout pass,
    /// so a new style, text size or width picks the columns again.
    /// </summary>
    private sealed partial class ChipGrid : Panel
    {
        private int columns = 6;

        protected override XamlSize MeasureOverride(XamlSize available)
        {
            var widest = 0.0;
            foreach (var chip in Children)
            {
                chip.Measure(new XamlSize(double.PositiveInfinity, double.PositiveInfinity));
                widest = Math.Max(widest, chip.DesiredSize.Width);
            }
            columns = Columns(available.Width, widest);
            var width = double.IsInfinity(available.Width) ? widest : ColumnWidth(available.Width);
            foreach (var chip in Children)
                chip.Measure(new XamlSize(width, double.PositiveInfinity));
            var rows = RowHeights();
            return new XamlSize(columns * width + (columns - 1) * Space.Tight, rows.Sum() + (rows.Length - 1) * Space.Tight);
        }

        protected override XamlSize ArrangeOverride(XamlSize final)
        {
            var width = ColumnWidth(final.Width);
            var y = 0.0;
            foreach (var (row, height) in Children.Chunk(columns).Zip(RowHeights()))
            {
                for (var column = 0; column < row.Length; column++)
                    row[column].Arrange(new XamlRect(column * (width + Space.Tight), y, width, height));
                y += height + Space.Tight;
            }
            return final;
        }

        private double ColumnWidth(double total) => Math.Max(0, (total - (columns - 1) * Space.Tight) / columns);

        private double[] RowHeights() => [.. Children.Chunk(columns).Select(row => row.Max(chip => chip.DesiredSize.Height))];
    }
}
