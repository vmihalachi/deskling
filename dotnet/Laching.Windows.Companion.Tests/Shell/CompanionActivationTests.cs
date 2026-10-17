using Laching.Windows.Companion.Shell;
using Laching.Windows.Shell;
using Microsoft.Windows.AppLifecycle;

namespace Laching.Windows.Companion.Tests.Shell;

public sealed class CompanionActivationTests
{
    [Theory]
    [InlineData(ExtendedActivationKind.Launch)]
    [InlineData(ExtendedActivationKind.Protocol)]
    [InlineData(ExtendedActivationKind.File)]
    public void OpeningTheAppShowsHome(ExtendedActivationKind kind) => Assert.True(CompanionActivation.IsOpening(kind));

    [Fact]
    public void SignInStartsItQuietly() => Assert.False(CompanionActivation.IsOpening(ExtendedActivationKind.StartupTask));

    [Fact]
    public void ANotificationClickIsTheNotifiers() =>
        Assert.False(CompanionActivation.IsOpening(ExtendedActivationKind.AppNotification));

    [Theory]
    [InlineData(ExtendedActivationKind.Launch)]
    [InlineData(ExtendedActivationKind.AppNotification)]
    public void AClickLaunchIsKnownByItsArgument(ExtendedActivationKind kind) =>
        Assert.False(CompanionActivation.IsOpening([App, AppNotifier.ClickArgument], () => kind));

    [Fact]
    public void AClickLaunchNeverReadsTheActivation() =>
        Assert.False(
            CompanionActivation.IsOpening([App, AppNotifier.ClickArgument], () => throw new InvalidOperationException("read"))
        );

    [Theory]
    [InlineData(ExtendedActivationKind.Launch, true)]
    [InlineData(ExtendedActivationKind.StartupTask, false)]
    [InlineData(ExtendedActivationKind.AppNotification, false)]
    public void AnyOtherLaunchGoesByItsKind(ExtendedActivationKind kind, bool opening)
    {
        Assert.Equal(opening, CompanionActivation.IsOpening([App], () => kind));
        Assert.Equal(opening, CompanionActivation.IsOpening([App, "--first-run"], () => kind));
    }

    private const string App = @"C:\Program Files\Example\Example.exe";
}
