using Deskling.Companion.Shell.App;
using Deskling.Windows.Companion.Shell;
using Deskling.Windows.Shell;

namespace Deskling.Windows.Companion.Tests.Shell;

public sealed class MenuConversionTests
{
    [Fact]
    public void CommandsKeepTheirWordsActionAndCheck()
    {
        var said = 0;
        var rows = MenuConversion.ToTray(
            [new MenuItem.Command("Say something", () => said++), new MenuItem.Command("Show", () => { }, IsChecked: true)]
        );

        var say = Assert.IsType<TrayMenuItem.Command>(rows[0]);
        Assert.Equal("Say something", say.Text);
        Assert.False(say.IsChecked);
        say.Invoke();
        Assert.Equal(1, said);
        Assert.True(Assert.IsType<TrayMenuItem.Command>(rows[1]).IsChecked);
    }

    [Fact]
    public void ADisabledCommandIsADimmedLabel()
    {
        var row = MenuConversion.ToTray(new MenuItem.Command("Hide for an hour", () => { }, IsEnabled: false));

        Assert.Equal(new TrayMenuItem.Label("Hide for an hour"), row);
    }

    [Fact]
    public void SubmenusAndSeparatorsKeepTheirShape()
    {
        var rows = MenuConversion.ToTray(
        [
            new MenuItem.Submenu("Shh", [new MenuItem.Command("For an hour", () => { }), new MenuItem.Separator()]),
            new MenuItem.Separator(),
        ]);

        var shh = Assert.IsType<TrayMenuItem.Submenu>(rows[0]);
        Assert.Equal("Shh", shh.Text);
        Assert.Equal("For an hour", Assert.IsType<TrayMenuItem.Command>(shh.Items[0]).Text);
        Assert.IsType<TrayMenuItem.Separator>(shh.Items[1]);
        Assert.IsType<TrayMenuItem.Separator>(rows[1]);
    }
}
