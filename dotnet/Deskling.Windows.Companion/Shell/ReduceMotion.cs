using Windows.UI.ViewManagement;

namespace Deskling.Windows.Companion.Shell;

/// <summary>
/// Windows' "Animation effects" setting (Settings › Accessibility › Visual effects), the twin of the Mac's Reduce
/// Motion: when it is off, the character holds still and windows appear without fading.
/// </summary>
public static class ReduceMotion
{
    private static readonly UISettings Settings = new();

    /// <summary>Read each time, so a change applies to the next animation.</summary>
    public static bool IsOn => !Settings.AnimationsEnabled;
}
