using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>Onboarding, plain: the first run (<c>--first-run</c>) or Settings opens it.</summary>
public sealed partial class OnboardingView : UserControl
{
    public OnboardingView(FixtureModel model)
    {
        Model = model;
        InitializeComponent();
    }

    public FixtureModel Model { get; }
}
