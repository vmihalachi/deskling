using Laching.Companion.Figures;
using Microsoft.UI.Xaml.Media;
using FigurePoint = Laching.Companion.Figures.Point;
using XamlPoint = Windows.Foundation.Point;

namespace Laching.Windows.Companion.Views;

/// <summary>Paths as XAML geometry: the character's parts and the speech bubble's outline.</summary>
internal static class Geometries
{
    public static PathGeometry Path(IReadOnlyList<PathCommand> commands, bool evenOdd = false)
    {
        var geometry = new PathGeometry { FillRule = evenOdd ? FillRule.EvenOdd : FillRule.Nonzero };
        PathFigure? figure = null;
        foreach (var command in commands)
        {
            switch (command)
            {
                case PathCommand.Move move:
                    figure = new PathFigure { StartPoint = P(move.To), IsFilled = true };
                    geometry.Figures.Add(figure);
                    break;
                case PathCommand.Line line when figure is not null:
                    figure.Segments.Add(new LineSegment { Point = P(line.To) });
                    break;
                case PathCommand.Cubic cubic when figure is not null:
                    figure.Segments.Add(new BezierSegment { Point1 = P(cubic.Control1), Point2 = P(cubic.Control2), Point3 = P(cubic.To) });
                    break;
                case PathCommand.Close when figure is not null:
                    figure.IsClosed = true;
                    break;
            }
        }
        return geometry;
    }

    private static XamlPoint P(FigurePoint point) => new(point.X, point.Y);
}
