using WinRT;

namespace Laching.Windows.Shell;

/// <summary>
/// Reads a XAML object handed over untyped (a resource, a template child, a visual tree node or parent) as a type.
/// Under Native AOT such an object can arrive wrapped as the nearest type the trimmed app still knows, so a Style comes
/// back as a plain DependencyObject: a cast throws and <c>as</c> / <c>is</c> quietly fail. <see cref="As{T}"/> asks the
/// object itself for the type, and using it keeps that type's wrapper in the build. Debug (JIT) builds never show the
/// problem, so a Store build can crash where every Debug run worked.
/// </summary>
public static class XamlCast
{
    /// <summary><paramref name="value"/> as <typeparamref name="T"/>, or null when it's another type (or null).</summary>
    public static T? As<T>(object? value)
        where T : class
    {
        if (value is T typed)
            return typed;
        if (value is not IWinRTObject)
            return null;
        try
        {
            return value.As<T>();
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }
}
