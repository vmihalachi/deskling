using System.Reflection;
using System.Runtime.Versioning;
using Laching.Windows.Shell;

namespace Laching.Windows.Tests;

public sealed class PackageTests
{
    /// <summary>
    /// The package requires Windows 11: the SDK writes <c>TargetPlatformMinVersion</c> into the assembly as the OS it
    /// supports, which is what an app built on it gets.
    /// </summary>
    [Fact]
    public void RequiresWindows11()
    {
        var supported = typeof(SingleInstance).Assembly.GetCustomAttribute<SupportedOSPlatformAttribute>();
        Assert.Equal("Windows10.0.22000.0", supported?.PlatformName);
    }
}
