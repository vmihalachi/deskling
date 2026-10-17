using System.Reflection;
using System.Runtime.Versioning;
using Laching.Windows.Companion.Shell;

namespace Laching.Windows.Companion.Tests;

public sealed class PackageTests
{
    /// <summary>
    /// The package requires Windows 11, as <c>Laching.Windows</c> does: the SDK writes <c>TargetPlatformMinVersion</c>
    /// into the assembly as the OS it supports, which is what an app built on it gets.
    /// </summary>
    [Fact]
    public void RequiresWindows11()
    {
        var supported = typeof(CompanionApp).Assembly.GetCustomAttribute<SupportedOSPlatformAttribute>();
        Assert.Equal("Windows10.0.22000.0", supported?.PlatformName);
    }
}
