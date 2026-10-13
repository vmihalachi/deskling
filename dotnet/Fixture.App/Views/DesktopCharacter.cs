using Deskling.Windows.Companion.Views;

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
}
