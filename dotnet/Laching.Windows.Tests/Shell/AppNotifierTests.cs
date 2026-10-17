using Laching.Windows.Shell;
using Microsoft.Windows.AppLifecycle;

namespace Laching.Windows.Tests.Shell;

/// <summary>How a copy Windows started for a notification click knows it, and what it hands over.</summary>
public sealed class AppNotifierTests
{
    private const string App = @"C:\Program Files\Example\Example.exe";

    [Fact]
    public void AClickLaunchCarriesTheServersArgument()
    {
        Assert.True(AppNotifier.IsClickLaunch([App, AppNotifier.ClickArgument]));
        Assert.False(AppNotifier.IsClickLaunch([App]));
        Assert.False(AppNotifier.IsClickLaunch([App, "--first-run"]));
    }

    [Fact]
    public void OnlyAClickIsHandedOver() => Assert.True(AppNotifier.IsClick(ExtendedActivationKind.AppNotification));

    [Theory]
    [InlineData(ExtendedActivationKind.Launch)]
    [InlineData(ExtendedActivationKind.StartupTask)]
    [InlineData(ExtendedActivationKind.Protocol)]
    [InlineData(ExtendedActivationKind.File)]
    public void AnyOtherKindIsDropped(ExtendedActivationKind kind) => Assert.False(AppNotifier.IsClick(kind));
}
