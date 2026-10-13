using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>The tray icon's panel: the status, the bubble and the quick actions.</summary>
public sealed partial class PanelView : UserControl
{
    public PanelView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
    }

    public FixtureModel Model { get; }
}
