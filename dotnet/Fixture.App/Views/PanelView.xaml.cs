using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>The tray icon's panel: the status, the character and its bubble, and the quick actions as panel rows.</summary>
public sealed partial class PanelView : UserControl
{
    public PanelView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
        Figure.MouthLevel = () => Model.Performer.MouthLevel;
    }

    public FixtureModel Model { get; }
}
