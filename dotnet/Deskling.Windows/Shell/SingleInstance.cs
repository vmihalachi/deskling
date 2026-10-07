using Microsoft.Windows.AppLifecycle;

namespace Deskling.Windows.Shell;

/// <summary>
/// Keeps one copy of the app running. A second launch (Start menu, a notification click) hands its activation to
/// the running copy, which gets it as <c>AppInstance.GetCurrent().Activated</c>, and exits.
/// </summary>
public static class SingleInstance
{
    /// <summary>
    /// Registers this process under <paramref name="key"/>, or redirects the launch to the process that already
    /// holds it. True means this is the copy to run; false means the launch was handed over and <c>Main</c> should
    /// return without starting the app. Call on the STA main thread after <c>ComWrappersSupport.InitializeComWrappers()</c>
    /// and before <c>Application.Start</c>.
    /// </summary>
    /// <param name="key">Any constant string; one per app.</param>
    public static bool Claim(string key)
    {
        var instance = AppInstance.FindOrRegisterForKey(key);
        if (instance.IsCurrent)
            return true;
        var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
        // Waiting on the STA thread pumps COM, which the redirect needs.
        Task.Run(() => instance.RedirectActivationToAsync(activation).AsTask()).Wait();
        return false;
    }
}
