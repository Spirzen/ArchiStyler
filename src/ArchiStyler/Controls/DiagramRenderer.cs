using ArchiStyler.Helpers;
using ArchiStyler.Models;
using ArchiStyler.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace ArchiStyler.Controls;

public static class DiagramRenderer
{
    private const double HitLineThickness = 14;

    public static void AddRelationVisual(
        Canvas canvas,
        Point start,
        Point end,
        RelationKind kind,
        TargetLanguage language,
        RelationDefinition? rel = null,
        Guid? relationId = null,
        bool interactive = false,
        bool selected = false)
    {
        if (Distance(start, end) < 2) return;

        var (brush, dashed) = StyleFor(kind);
        var thickness = selected ? 3.5 : 2.5;
        var tag = new RelationVisualTag { RelationId = relationId, Part = RelationVisualPart.Shape };

        if (interactive && relationId is not null)
        {
            canvas.Children.Add(new Line
            {
                StartPoint = start,
                EndPoint = end,
                Stroke = Brushes.Transparent,
                StrokeThickness = HitLineThickness,
                IsHitTestVisible = true,
                Tag = new RelationVisualTag { RelationId = relationId, Part = RelationVisualPart.HitLine }
            });
        }

        canvas.Children.Add(new Line
        {
            StartPoint = start,
            EndPoint = end,
            Stroke = brush,
            StrokeThickness = thickness,
            StrokeDashArray = dashed ? [6, 4] : null,
            IsHitTestVisible = false,
            Tag = tag
        });

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var angle = Math.Atan2(dy, dx);
        const double headLen = 12;
        var tip = end;
        var left = new Point(
            end.X - headLen * Math.Cos(angle - Math.PI / 6),
            end.Y - headLen * Math.Sin(angle - Math.PI / 6));
        var right = new Point(
            end.X - headLen * Math.Cos(angle + Math.PI / 6),
            end.Y - headLen * Math.Sin(angle + Math.PI / 6));

        if (kind == RelationKind.Inherits)
        {
            canvas.Children.Add(new Polyline
            {
                Points = [left, tip, right],
                Stroke = brush,
                StrokeThickness = thickness,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                Tag = tag
            });
        }
        else
        {
            canvas.Children.Add(new Polygon
            {
                Points = [tip, left, right],
                Fill = brush,
                Stroke = brush,
                StrokeThickness = 1,
                IsHitTestVisible = false,
                Tag = tag
            });
        }

        var label = RelationKindHelper.EdgeLabel(kind, rel, language);
        AddRelationLabel(canvas, start, end, label, brush, relationId);
    }

    private static void AddRelationLabel(
        Canvas canvas,
        Point start,
        Point end,
        string text,
        IBrush foreground,
        Guid? relationId)
    {
        var mid = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
        var label = new TextBlock
        {
            Text = text,
            FontSize = 10,
            Foreground = foreground,
            Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 28)),
            Padding = new Thickness(4, 2),
            IsHitTestVisible = false,
            Tag = new RelationVisualTag { RelationId = relationId, Part = RelationVisualPart.Label }
        };
        label.Measure(Size.Infinity);
        Canvas.SetLeft(label, mid.X - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, mid.Y - label.DesiredSize.Height / 2);
        canvas.Children.Add(label);
    }

    public static void RenderAllRelations(
        Canvas canvas,
        ProjectModel project,
        IReadOnlyList<ClassNodeViewModel> nodes,
        Func<ClassNodeViewModel, Rect> getBounds,
        TargetLanguage language,
        Guid? selectedRelationId = null)
    {
        var map = nodes.ToDictionary(n => n.Id);
        var drawn = new HashSet<string>();

        void Draw(ClassNodeViewModel from, ClassNodeViewModel to, RelationKind kind, RelationDefinition? rel = null)
        {
            var key = $"{from.Id}:{to.Id}:{kind}";
            if (!drawn.Add(key)) return;
            var (start, end) = rel is not null
                ? DiagramGeometry.ResolveEndpoints(from, to, rel, getBounds)
                : DiagramGeometry.ResolveEndpoints(from, to,
                    new RelationDefinition { FromClassId = from.Id, ToClassId = to.Id, Kind = kind }, getBounds);

            var interactive = rel is not null;
            var selected = rel is not null && rel.Id == selectedRelationId;
            AddRelationVisual(canvas, start, end, kind, language, rel, rel?.Id, interactive, selected);
        }

        foreach (var rel in project.Relations)
        {
            if (!map.TryGetValue(rel.FromClassId, out var from) ||
                !map.TryGetValue(rel.ToClassId, out var to))
                continue;
            Draw(from, to, rel.Kind, rel);
        }

        foreach (var node in nodes)
        {
            var cls = node.Model;
            if (!string.IsNullOrWhiteSpace(cls.BaseType))
            {
                var target = nodes.FirstOrDefault(n =>
                    n.Model.Name.Equals(cls.BaseType, StringComparison.Ordinal));
                if (target is not null && !project.Relations.Any(r =>
                        r.FromClassId == node.Id && r.ToClassId == target.Id && r.Kind == RelationKind.Inherits))
                    Draw(node, target, RelationKind.Inherits);
            }

            foreach (var iface in cls.ImplementedInterfaces)
            {
                var target = nodes.FirstOrDefault(n =>
                    n.Model.Name.Equals(iface, StringComparison.Ordinal));
                if (target is null) continue;
                if (project.Relations.Any(r =>
                        r.FromClassId == node.Id && r.ToClassId == target.Id && r.Kind == RelationKind.Implements))
                    continue;
                Draw(node, target, RelationKind.Implements);
            }
        }
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static (IBrush brush, bool dashed) StyleFor(RelationKind kind) =>
        kind switch
        {
            RelationKind.Inherits => (GetBrush("BrushArrowInherit"), false),
            RelationKind.Implements => (GetBrush("BrushArrowImplement"), true),
            RelationKind.Composes => (GetBrush("BrushNeonPink"), false),
            RelationKind.Aggregates => (GetBrush("BrushNeonLavender"), true),
            RelationKind.FieldReference => (GetBrush("BrushNeonCyan"), true),
            RelationKind.MethodReference => (GetBrush("BrushNeonLavender"), true),
            RelationKind.UsingImport => (GetBrush("BrushNeonPink"), true),
            _ => (GetBrush("BrushArrowUse"), true)
        };

    private static IBrush GetBrush(string key)
    {
        if (Application.Current?.TryFindResource(key, out var res) == true && res is IBrush b)
            return b;
        return Brushes.Cyan;
    }
}
