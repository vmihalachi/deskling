using Deskling.Windows.Companion.Shell;
using Microsoft.Windows.AppLifecycle;

namespace Deskling.Windows.Companion.Tests.Shell;

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
}
