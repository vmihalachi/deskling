namespace Deskling.Windows.Shell;

/// <summary>
/// Whether the app runs with package identity (installed from an MSIX) or without (an unpackaged build, such as a
/// Debug run from <c>dotnet run</c>). The startup task, the Microsoft Store and <c>ApplicationData</c> need identity.
/// </summary>
public static class PackageIdentity
{
    private const int AppModelErrorNoPackage = 15700;

    /// <summary>True when the process belongs to a package.</summary>
    public static bool IsPackaged { get; } = HasPackage();

    private static bool HasPackage()
    {
        uint length = 0;
        return NativeMethods.GetCurrentPackageFullName(ref length, 0) != AppModelErrorNoPackage;
    }
}
