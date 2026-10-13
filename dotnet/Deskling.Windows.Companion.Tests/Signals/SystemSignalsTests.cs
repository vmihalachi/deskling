using Deskling.Companion.Settings;
using Deskling.Windows.Companion.Shell;
using Deskling.Windows.Companion.Signals;
using Microsoft.UI.Xaml;

namespace Deskling.Windows.Companion.Tests.Signals;

public sealed class SystemSignalsTests
{
    [Fact]
    public void AppearanceReportsOnlyARealSwap()
    {
        using var appearance = new SystemAppearance(new TestUiContext());
        var reported = new List<bool>();
        appearance.Changed += reported.Add;
        var start = appearance.IsDark;

        appearance.Update(start); // an accent color change
        appearance.Update(!start);
        appearance.Update(!start);
        appearance.Update(start);

        Assert.Equal([!start, start], reported);
        Assert.Equal(start, appearance.IsDark);
    }

    [Fact]
    public void TheMuteFlagReadsWithoutThrowing()
    {
        // No output device on a CI runner: unknown (null), never a throw.
        Assert.Null(Record.Exception(() => new SystemAudioOutput().IsOutputMuted()));
    }

    [Fact]
    public void ThePowerSourceHasAStateOnlyWithABattery()
    {
        using var power = new SystemPowerSource(new TestUiContext());

        if (power.CurrentPowerState() is { } state)
            Assert.InRange(state.Percent ?? 0, 0, 100);
    }

    [Theory]
    [InlineData(AppearanceSetting.System, ElementTheme.Default)]
    [InlineData(AppearanceSetting.Light, ElementTheme.Light)]
    [InlineData(AppearanceSetting.Dark, ElementTheme.Dark)]
    public void TheAppearanceSettingIsATheme(AppearanceSetting setting, ElementTheme theme)
    {
        Assert.Equal(theme, WindowsCompanionHost<int>.Theme(setting));
    }
}
