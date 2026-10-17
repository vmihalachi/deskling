namespace Laching.Companion.Shell.App;

/// <summary>
/// What the platform's app class starts (Swift's <c>CompanionLaunching</c>): a
/// <see cref="CompanionAppModel{TWindow, TSettings}"/>, whatever its window and settings types.
/// </summary>
public interface ICompanionLaunching
{
    /// <summary>Once at launch: Home unless the system started the app at sign-in (<paramref name="showHome"/> false).</summary>
    void Launch(bool showHome);

    /// <summary>The user opened the app again while it was running.</summary>
    void Reopen();
}
