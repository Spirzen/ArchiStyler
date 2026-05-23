using ArchiStyler.Models;
using ArchiStyler.ViewModels;
using Avalonia;

namespace ArchiStyler.Controls;

public static class DiagramGeometry
{
    public const double DefaultCardWidth = 200;
    public const double DefaultCardHeight = 100;

    public static Rect GetBounds(ClassNodeViewModel node, double width, double height) =>
        new(node.X, node.Y, width, height);

    public static Point GetPortPoint(Rect bounds, ConnectionPort port) =>
        port switch
        {
            ConnectionPort.North => new Point(bounds.X + bounds.Width / 2, bounds.Y),
            ConnectionPort.East => new Point(bounds.Right, bounds.Y + bounds.Height / 2),
            ConnectionPort.South => new Point(bounds.X + bounds.Width / 2, bounds.Bottom),
            ConnectionPort.West => new Point(bounds.X, bounds.Y + bounds.Height / 2),
            _ => bounds.Center
        };

    public static (ConnectionPort port, Point point) PickNearestPort(Rect bounds, Point canvasPoint)
    {
        var ports = new[]
        {
            (ConnectionPort.North, GetPortPoint(bounds, ConnectionPort.North)),
            (ConnectionPort.East, GetPortPoint(bounds, ConnectionPort.East)),
            (ConnectionPort.South, GetPortPoint(bounds, ConnectionPort.South)),
            (ConnectionPort.West, GetPortPoint(bounds, ConnectionPort.West))
        };

        var best = ports[0];
        var bestDist = double.MaxValue;
        foreach (var p in ports)
        {
            var d = Distance(p.Item2, canvasPoint);
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        return best;
    }

    public static (Point start, Point end) ResolveEndpoints(
        ClassNodeViewModel from,
        ClassNodeViewModel to,
        RelationDefinition rel,
        Func<ClassNodeViewModel, Rect> getBounds)
    {
        var fromBounds = getBounds(from);
        var toBounds = getBounds(to);

        if (rel.FromPort is { } fp && rel.ToPort is { } tp)
            return (GetPortPoint(fromBounds, fp), GetPortPoint(toBounds, tp));

        var fromCenter = fromBounds.Center;
        var toCenter = toBounds.Center;
        var fromPort = PickNearestPort(fromBounds, toCenter);
        var toPort = PickNearestPort(toBounds, fromCenter);
        return (fromPort.point, toPort.point);
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
