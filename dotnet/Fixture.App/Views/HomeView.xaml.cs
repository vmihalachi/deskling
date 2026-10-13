using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>Home: the status, the bubble, the actions, and the hand checks' controls. The tray glyph waves when it appears.</summary>
public sealed partial class HomeView : UserControl
{
    public HomeView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
        Loaded += (_, _) => Model.WaveFromHome();
    }

    public FixtureModel Model { get; }
}
