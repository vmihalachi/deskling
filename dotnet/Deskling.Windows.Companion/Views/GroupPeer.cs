using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// Narrator's view of an element as a group named by its <c>AutomationProperties.Name</c> (the app's words), so the
/// controls inside come with that context ("Quiet hours, group" before the first hour). A layout panel or a
/// <c>UserControl</c> has no automation peer of its own, so its children would otherwise read with no context.
/// </summary>
internal sealed partial class GroupPeer(FrameworkElement owner, string className) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => className;
}

/// <summary>A grid Narrator reads as a group named by its <c>AutomationProperties.Name</c> (<see cref="GroupPeer"/>).</summary>
internal sealed partial class GroupGrid : Grid
{
    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this, nameof(GroupGrid));
}
