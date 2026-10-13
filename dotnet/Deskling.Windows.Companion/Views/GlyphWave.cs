using Deskling.Companion.Figures;
using Deskling.Companion.Rules;
using Deskling.Windows.Companion.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XamlPath = Microsoft.UI.Xaml.Shapes.Path;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// A copy of the tray glyph inside a window (the Swift apps' <c>GlyphWave</c>), for Home's picture of the notification
/// area: the character's Awake glyph, or its talking frames at 8 fps while <see cref="Active"/> (as the real icon waves
/// when Home appears), in the look's ink at <see cref="GlyphSize"/>. Under reduced motion, and so in screenshot runs,
/// it holds the first talking frame. Hidden from Narrator: it pictures something else on screen.
/// </summary>
public sealed partial class GlyphWave : UserControl
{
    private readonly Canvas canvas = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(GlyphAnimation.FrameInterval) };
    private RiggedCharacter? character;
    private bool active;
    private int frame = 1;
    private double glyphSize = 18;

    public GlyphWave()
    {
        IsTabStop = false;
        Content = canvas;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        timer.Tick += (_, _) =>
        {
            frame++;
            Draw();
        };
        Loaded += (_, _) => Restart();
        Unloaded += (_, _) => timer.Stop();
        Resize();
        LookBinding.Keep(this, Draw);
    }

    /// <summary>Whose glyph; nothing is drawn while it's null.</summary>
    public RiggedCharacter? Character
    {
        get => character;
        set
        {
            character = value;
            Draw();
        }
    }

    /// <summary>Steps through the talking frames while true.</summary>
    public bool Active
    {
        get => active;
        set
        {
            if (active == value)
                return;
            active = value;
            frame = 1;
            Restart();
        }
    }

    /// <summary>The glyph's square, in DIPs; 18 by default.</summary>
    public double GlyphSize
    {
        get => glyphSize;
        set
        {
            glyphSize = value;
            Resize();
        }
    }

    private void Restart()
    {
        if (active && IsLoaded && !ReduceMotion.IsOn)
            timer.Start();
        else
            timer.Stop();
        Draw();
    }

    private void Resize()
    {
        Width = Height = canvas.Width = canvas.Height = glyphSize;
        Draw();
    }

    private void Draw()
    {
        canvas.Children.Clear();
        if (character is null || character.GlyphSize <= 0)
            return;
        var mood = active ? Mood.Talking : Mood.Awake;
        var count = Math.Max(character.GlyphFrameCount(mood), 1);
        var shown = active ? frame % count : 0;
        var scale = glyphSize / character.GlyphSize;
        foreach (var shape in character.Glyph(mood, shown))
        {
            // The stroke widths are in glyph units, so the whole path scales, as the tray's bitmap does. A transform and
            // a brush belong to one element each.
            var path = new XamlPath
            {
                Data = Geometries.Path(shape.Path, shape.EvenOdd),
                RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale },
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
            if (shape.Fill is not null)
                path.Fill = LookBinding.Brush(this, palette => palette.Ink);
            if (shape.Stroke is not null && shape.StrokeWidth > 0)
            {
                path.Stroke = LookBinding.Brush(this, palette => palette.Ink);
                path.StrokeThickness = shape.StrokeWidth;
            }
            canvas.Children.Add(path);
        }
    }
}
