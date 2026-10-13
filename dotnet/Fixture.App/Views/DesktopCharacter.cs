using Deskling.Windows.Companion.Desktop;
using Deskling.Windows.Companion.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FixtureApp.Views;

/// <summary>The character on the desktop: a canvas that follows the model (code, as it lives in a window of its own).</summary>
public static class DesktopCharacter
{
    public static CharacterCanvas Make(FixtureModel model)
    {
        var canvas = new CharacterCanvas
        {
            Character = model.GlyphCharacter,
            AccessibilityLabel = "The fixture",
            MouthLevel = () => model.Performer.MouthLevel,
        };
        void Update()
        {
            canvas.Mood = model.Mood;
            canvas.Talking = model.IsVoicing;
            canvas.Line = model.Bubble;
        }
        Update();
        model.PropertyChanged += (_, _) => Update();
        return canvas;
    }

    /// <summary>
    /// The desktop character for a screenshot: its bubble, tail toward it, beside the character standing on its shadow,
    /// at the size in the settings (the desktop window itself is transparent, so it can't be shot alone).
    /// </summary>
    public static FrameworkElement Sheet(FixtureModel model)
    {
        var size = model.DesktopLayout.WindowSize(model.Settings.DesktopSize);
        var stand = DesktopCompanion.Stand(Make(model), model.DesktopLayout);
        (stand.Width, stand.Height) = (size.Width, size.Height);
        var bubble = DesktopCompanion.BubblePreview(model.Bubble ?? "…", BubbleTail.Trailing, null, model.DesktopLayout);
        bubble.VerticalAlignment = VerticalAlignment.Top;
        // The tail's tip level with the head, as the desktop places it.
        bubble.Margin = new Thickness(0, Math.Max(0, model.DesktopLayout.HeadY(new(0, 0, size.Width, size.Height)) - 30), -size.Width * model.DesktopLayout.BubbleTuck, 0);
        var row = new StackPanel { Orientation = Orientation.Horizontal, Padding = new Thickness(Space.Group) };
        row.Children.Add(bubble);
        row.Children.Add(stand);
        return row;
    }
}
