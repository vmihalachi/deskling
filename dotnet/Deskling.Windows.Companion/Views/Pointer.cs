using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XamlPath = Microsoft.UI.Xaml.Shapes.Path;
using XamlPoint = Windows.Foundation.Point;

namespace Deskling.Windows.Companion.Views;

/// <summary>
/// Home's pointer toward the tray (Swift's <c>Pointer</c>, which points up at the menu bar): a dashed curve from one
/// corner of its frame to the opposite one at the right, with a chevron at its end. Down and right by default, where the
/// taskbar's notification area usually is; <see cref="PointsUp"/> for a taskbar at the top. Decorative: Narrator skips
/// it.
/// </summary>
public sealed partial class Pointer : UserControl
{
    private readonly XamlPath curve = new() { StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeDashCap = PenLineCap.Round };
    private readonly XamlPath head = new() { StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round };
    private Brush? color;
    private double lineWidth = 2;
    private double headLength = 11;
    private bool pointsUp;

    public Pointer()
    {
        IsTabStop = false;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        var canvas = new Canvas();
        canvas.Children.Add(curve);
        canvas.Children.Add(head);
        Content = canvas;
        SizeChanged += (_, _) => Reshape();
        Restroke();
        LookBinding.Keep(this, Repaint);
    }

    /// <summary>The line's color; the look's accent when unset.</summary>
    public Brush? Color
    {
        get => color;
        set
        {
            color = value;
            Repaint();
        }
    }

    public double LineWidth
    {
        get => lineWidth;
        set
        {
            lineWidth = value;
            Restroke();
        }
    }

    /// <summary>How far the chevron's arms reach back from the tip.</summary>
    public double HeadLength
    {
        get => headLength;
        set
        {
            headLength = value;
            Reshape();
        }
    }

    /// <summary>True points up and right (a taskbar at the top), as on the Mac.</summary>
    public bool PointsUp
    {
        get => pointsUp;
        set
        {
            pointsUp = value;
            Reshape();
        }
    }

    private void Restroke()
    {
        curve.StrokeThickness = head.StrokeThickness = lineWidth;
        // Dashes of 4 and gaps of 5 points, in stroke widths as XAML counts them.
        curve.StrokeDashArray = [4 / lineWidth, 5 / lineWidth];
    }

    private void Repaint() => curve.Stroke = head.Stroke = color ?? LookBinding.Brush(this, p => p.Accent);

    private void Reshape()
    {
        var (w, h) = (ActualWidth, ActualHeight);
        if (w <= 0 || h <= 0)
            return;
        // The Mac's curve, mirrored top to bottom unless it points up.
        XamlPoint P(double x, double y) => new(x, pointsUp ? y : h - y);
        var tip = P(w - 2, 2);
        var start = P(0, h);
        var control = P(w * 0.15, h * 0.1);
        var figure = new PathFigure { StartPoint = start };
        figure.Segments.Add(new QuadraticBezierSegment { Point1 = control, Point2 = tip });
        curve.Data = new PathGeometry { Figures = { figure } };
        var arms = new PathFigure { StartPoint = P(w - 2 - headLength, 2 + 1) };
        arms.Segments.Add(new LineSegment { Point = tip });
        arms.Segments.Add(new LineSegment { Point = P(w - 2 - 3, 2 + headLength) });
        head.Data = new PathGeometry { Figures = { arms } };
    }
}
