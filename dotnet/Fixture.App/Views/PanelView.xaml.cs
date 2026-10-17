using Laching.Windows.Companion.Views;
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
        // Press and hold the character: Shh for an hour, as the apps' panels do.
        HoldGesture.Attach(Figure, Model.ShushForAnHour);
    }

    public FixtureModel Model { get; }
}
