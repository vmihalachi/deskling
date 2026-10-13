using Deskling.Companion.Shell.App;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>Home: the character, the status, the bubble, the actions, and the hand checks' controls. The tray glyph waves when it appears.</summary>
public sealed partial class HomeView : UserControl
{
    public HomeView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
        foreach (var figure in new[] { Figure, RasterFigure })
            figure.MouthLevel = () => Model.Performer.MouthLevel;
        // The desktop bubble's countdown, shown here for a look at the dial: 8 s from each new line.
        Model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Model.Bubble))
                Speech.Countdown = Model.Bubble is null ? null : new BubbleCountdown(DateTimeOffset.Now, 8);
        };
        Loaded += (_, _) => Model.WaveFromHome();
    }

    public FixtureModel Model { get; }
}
