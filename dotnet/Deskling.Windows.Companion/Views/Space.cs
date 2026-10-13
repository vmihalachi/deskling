using Microsoft.UI.Xaml;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// The spacing scale (Swift's <c>Space</c>): one 4-point grid where each step has a job, the same numbers on both
/// platforms. Pick the step by the relationship, not by how it looks in one place.
/// </summary>
public static class Space
{
    /// <summary>4: a number and its unit, a glyph and its word, a title and the note under it.</summary>
    public const double Tight = 4;

    /// <summary>8: a heading and its text, a label and its control, the lines of one list.</summary>
    public const double Snug = 8;

    /// <summary>12: siblings in a group: rows, chips, buttons in a row.</summary>
    public const double Item = 12;

    /// <summary>16: a card's inner margin, and between the groups inside one section; the panel's margin.</summary>
    public const double Group = 16;

    /// <summary>24: between a window's sections.</summary>
    public const double Section = 24;

    /// <summary>32: the window margin.</summary>
    public const double Region = 32;
}

/// <summary>
/// Where content sits in every window (Swift's <c>WindowInset</c>), so titles, text and buttons line up from one window
/// to the next. The windows' content reaches under the title bar, so the top leaves the caption buttons' band first.
/// </summary>
public static class WindowInset
{
    /// <summary>The side margin of every window's content and its action bar.</summary>
    public const double Side = Space.Region;

    /// <summary>The caption buttons' band at the top of every window (WinUI's standard title bar).</summary>
    public const double TitleBar = 32;

    /// <summary>From the bottom of the title bar's band to the top of the window title.</summary>
    public const double Top = Space.Section;

    /// <summary>Under the last thing in a window.</summary>
    public const double Bottom = Space.Region;

    /// <summary>The tray panel has no title bar and sits by the taskbar, so it is tighter.</summary>
    public const double Panel = Space.Group;

    /// <summary><see cref="Panel"/> on every side, for the panel's content.</summary>
    public static Thickness PanelPadding { get; } = new(Panel);

    /// <summary>
    /// The shared window frame (Swift's <c>windowPadding</c>): 32 at the sides, the title bar's band and 24 at the top,
    /// <paramref name="bottom"/> under the content.
    /// </summary>
    public static Thickness Window(double bottom = Bottom) => new(Side, TitleBar + Top, Side, bottom);

    /// <summary><see cref="Window"/> with <see cref="Bottom"/> under the content, for <c>{x:Bind}</c> (which can't call it with a number).</summary>
    public static Thickness Content { get; } = Window();

    /// <summary><see cref="Window"/> over an <see cref="ActionBar"/>: 16 under the content, as the Mac's Support window.</summary>
    public static Thickness ContentOverActionBar { get; } = Window(Space.Group);
}
