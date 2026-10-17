using Laching.Companion.Cadence;
using Laching.Companion.Settings;
using Laching.Companion.Shell.Desktop;

namespace Laching.Companion.Shell.App;

/// <summary>
/// The menus as data, in the app's words (<see cref="MenuCopy"/>): the tray icon's right-click menu and the desktop
/// character's. Both have Say something, Shh ▸ (or Wake up), Settings…, Support… and Quit. Once the supporter product is
/// bought, the tray's thanks instead of asking and the desktop character's drops the item. The tray's adds the desktop
/// submenu (show, hide for a while, size); the desktop character's adds Size ▸ and Hide ▸. Built fresh each time, from
/// the model's state; the platform turns the items into its native menu.
/// </summary>
public static class CompanionMenus
{
    private enum SupportItem
    {
        None,
        Ask,
        Thank,
    }

    public static IReadOnlyList<MenuItem> MenuBar<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy)
        where TWindow : notnull
        where TSettings : CompanionSettings
    {
        List<MenuItem> menu = [.. Talk(model, copy), new MenuItem.Separator()];
        menu.Add(new MenuItem.Submenu(copy.DesktopSubmenu, DesktopSubmenu(model, copy)));
        menu.Add(new MenuItem.Separator());
        menu.AddRange(App(model, copy, model.IsSupporter ? SupportItem.Thank : SupportItem.Ask));
        return menu;
    }

    public static IReadOnlyList<MenuItem> Desktop<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy)
        where TWindow : notnull
        where TSettings : CompanionSettings
    {
        List<MenuItem> menu = [.. Talk(model, copy), new MenuItem.Separator()];
        menu.Add(new MenuItem.Submenu(copy.Size, Sizes(model, copy)));
        menu.Add(new MenuItem.Submenu(copy.Hide,
        [
            new MenuItem.Command(copy.ForAnHour, () => model.HideDesktop(DesktopHide.Hour)),
            new MenuItem.Command(copy.ForTheRestOfToday, () => model.HideDesktop(DesktopHide.RestOfToday)),
            new MenuItem.Command(copy.UntilIShowItAgain, () => model.HideDesktop(DesktopHide.UntilShown)),
        ]));
        menu.Add(new MenuItem.Separator());
        menu.AddRange(App(model, copy, model.IsSupporter ? SupportItem.None : SupportItem.Ask));
        return menu;
    }

    private static IEnumerable<MenuItem> Talk<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy)
        where TWindow : notnull
        where TSettings : CompanionSettings
    {
        yield return new MenuItem.Command(copy.Say, model.SayNow);
        if (model.IsShushed)
        {
            yield return new MenuItem.Command(copy.WakeUp, model.Wake);
        }
        else
        {
            yield return new MenuItem.Submenu(copy.Shh,
            [
                new MenuItem.Command(copy.ForAnHour, () => model.Shush(ChatterCadence.Shush.Hour)),
                new MenuItem.Command(copy.ForTheRestOfToday, () => model.Shush(ChatterCadence.Shush.RestOfToday)),
                new MenuItem.Command(copy.UntilISay, () => model.Shush(ChatterCadence.Shush.UntilISay)),
            ]);
        }
    }

    private static List<MenuItem> DesktopSubmenu<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy)
        where TWindow : notnull
        where TSettings : CompanionSettings
    {
        var on = model.Settings.ShowOnDesktop;
        var hidden = model.DesktopHiddenUntil is not null;
        List<MenuItem> menu =
        [
            on && hidden
                ? new MenuItem.Command(copy.ShowNow, model.ShowDesktopNow)
                : new MenuItem.Command(copy.Show, () => model.SetShowOnDesktop(!on), IsChecked: on),
            new MenuItem.Command(copy.HideForAnHour, () => model.HideDesktop(DesktopHide.Hour), IsEnabled: on && !hidden),
            new MenuItem.Command(copy.HideForTheRestOfToday, () => model.HideDesktop(DesktopHide.RestOfToday), IsEnabled: on && !hidden),
            new MenuItem.Separator(),
        ];
        menu.AddRange(Sizes(model, copy));
        return menu;
    }

    private static List<MenuItem> Sizes<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy)
        where TWindow : notnull
        where TSettings : CompanionSettings =>
        DesktopSizes.All
            .Select(size => (MenuItem)new MenuItem.Command(
                copy.SizeLabel(size), () => model.Settings.DesktopSize = size, IsChecked: model.Settings.DesktopSize == size))
            .ToList();

    private static IEnumerable<MenuItem> App<TWindow, TSettings>(CompanionAppModel<TWindow, TSettings> model, MenuCopy copy, SupportItem support)
        where TWindow : notnull
        where TSettings : CompanionSettings
    {
        yield return new MenuItem.Command(copy.Settings, model.OpenSettings);
        if (support == SupportItem.Ask)
            yield return new MenuItem.Command(copy.Support, model.OpenSupport);
        else if (support == SupportItem.Thank)
            yield return new MenuItem.Command(copy.ThanksForSupporting, model.OpenSupport);
        yield return new MenuItem.Separator();
        yield return new MenuItem.Command(copy.Quit, model.Quit);
    }
}
