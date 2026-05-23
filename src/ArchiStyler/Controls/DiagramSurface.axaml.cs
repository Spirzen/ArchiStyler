using ArchiStyler.Helpers;
using ArchiStyler.Models;
using ArchiStyler.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
namespace ArchiStyler.Controls;
public partial class DiagramSurface : UserControl
{
    private const double AnchorSize = 12;
    private const double EndpointGrabRadius = 18;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 3.0;
    private const double WorldMinWidth = 2400;
    private const double WorldMinHeight = 1800;
    private const double WorldPadding = 240;
    private readonly ScaleTransform _zoomTransform = new(1, 1);
    private MainViewModel? _vm;
    private readonly Dictionary<Guid, ClassCard> _cards = new();
    private readonly Dictionary<Guid, FolderContainer> _folderContainers = new();
    private readonly Dictionary<Guid, Dictionary<ConnectionPort, Ellipse>> _anchors = new();
    private FolderNodeViewModel? _dragFolder;
    private Point _folderDragOffset;
    private Line? _previewLine;
    private PendingLink? _pendingLink;
    private PendingLinkTarget? _pendingLinkTarget;
    private ClassNodeViewModel? _dragNode;
    private Point _dragOffset;
    private bool _rebuildScheduled;
    private double _zoom = 1.0;
    private bool _spaceHeld;
    private bool _isPanning;
    private Point _panLast;
    private Guid? _selectedRelationId;
    private RelationDefinition? _relationDrag;
    private bool _dragRelationFrom;
    private bool _dragRelationTo;
    private sealed record PendingLink(Guid FromNodeId, ConnectionPort FromPort, Point StartPoint);
    private sealed record PendingLinkTarget(Guid FromNodeId, ConnectionPort FromPort, Guid ToNodeId, ConnectionPort ToPort);
    public DiagramSurface()
    {
        InitializeComponent();
        RootCanvas.RenderTransform = _zoomTransform;
        RootCanvas.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
        NavHint.Text = HelpTexts.DiagramNavigation;
        ToolTip.SetTip(ZoomInButton, "Увеличить (Ctrl + колёсико вверх)");
        ToolTip.SetTip(ZoomOutButton, "Уменьшить (Ctrl + колёсико вниз)");
        ToolTip.SetTip(ZoomResetButton, "Масштаб 100%");
        ZoomInButton.Click += (_, _) => SetZoom(_zoom * 1.15, GetViewportCenter());
        ZoomOutButton.Click += (_, _) => SetZoom(_zoom / 1.15, GetViewportCenter());
        ZoomResetButton.Click += (_, _) => SetZoom(1.0, GetViewportCenter());
        RelationPicker.Confirmed += OnRelationPickerConfirmed;
        RelationPicker.Cancelled += OnRelationPickerCancelled;
        PointerWheelChanged += OnPointerWheelChanged;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        SetZoom(1.0);
    }
    public void Initialize(MainViewModel vm)
    {
        if (_vm is not null)
            _vm.DiagramInvalidated -= OnDiagramInvalidated;
        _vm = vm;
        _vm.DiagramInvalidated += OnDiagramInvalidated;
        ScheduleRebuild();
    }
    private void OnDiagramInvalidated(object? sender, EventArgs e) => ScheduleRebuild();
    private void ScheduleRebuild()
    {
        if (_rebuildScheduled) return;
        _rebuildScheduled = true;
        Dispatcher.UIThread.Post(() =>
        {
            _rebuildScheduled = false;
            if (_pendingLink is not null || _dragNode is not null || _relationDrag is not null || _dragFolder is not null)
            {
                RefreshLinksAfterLayout();
                UpdateWorldBounds();
            }
            else
                Rebuild();
        }, DispatcherPriority.Background);
    }
    private void SetZoom(double zoom, Point? viewportAnchor = null)
    {
        var oldZoom = _zoom;
        var newZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) < 0.0001)
            return;

        var anchor = viewportAnchor ?? GetViewportCenter();
        var offset = Scroller.Offset;
        var newOffsetX = (offset.X + anchor.X) * (newZoom / oldZoom) - anchor.X;
        var newOffsetY = (offset.Y + anchor.Y) * (newZoom / oldZoom) - anchor.Y;

        _zoom = newZoom;
        _zoomTransform.ScaleX = newZoom;
        _zoomTransform.ScaleY = newZoom;
        UpdateWorldBounds();

        Scroller.Offset = new Vector(
            Math.Max(0, newOffsetX),
            Math.Max(0, newOffsetY));

        var pct = (int)Math.Round(_zoom * 100);
        ZoomLabel.Text = $"{pct}%";
        ZoomResetButton.Content = $"Сброс ({pct}%)";
    }

    private Point GetViewportCenter()
    {
        var vp = Scroller.Viewport;
        return new Point(vp.Width / 2, vp.Height / 2);
    }

    private void UpdateWorldBounds()
    {
        var maxX = WorldMinWidth;
        var maxY = WorldMinHeight;
        if (_vm is not null)
        {
            foreach (var folder in _vm.FolderNodes)
            {
                var fb = ProjectPathHelper.GetFolderBounds(folder.Model);
                maxX = Math.Max(maxX, fb.Right + WorldPadding);
                maxY = Math.Max(maxY, fb.Bottom + WorldPadding);
            }
            foreach (var node in _vm.Nodes)
            {
                var b = GetBounds(node);
                maxX = Math.Max(maxX, b.Right + WorldPadding);
                maxY = Math.Max(maxY, b.Bottom + WorldPadding);
            }
        }
        RootCanvas.Width = maxX;
        RootCanvas.Height = maxY;
        ZoomExtent.Width = maxX * _zoom;
        ZoomExtent.Height = maxY * _zoom;
    }
    private void Rebuild()
    {
        if (_vm is null) return;
        CancelPendingLink();
        HideRelationPicker();
        _dragNode = null;
        _relationDrag = null;
        _selectedRelationId = null;
        foreach (var card in _cards.Values)
            card.PointerPressed -= OnCardPointerPressed;
        foreach (var folder in _folderContainers.Values)
            UnhookFolder(folder);
        RootCanvas.Children.Clear();
        _cards.Clear();
        _folderContainers.Clear();
        _anchors.Clear();
        foreach (var folder in _vm.FolderNodes)
        {
            var container = new FolderContainer { DataContext = folder };
            HookFolder(container);
            Canvas.SetLeft(container, folder.X);
            Canvas.SetTop(container, folder.Y);
            RootCanvas.Children.Add(container);
            _folderContainers[folder.Id] = container;
        }
        foreach (var node in _vm.Nodes)
        {
            var card = new ClassCard { DataContext = node };
            card.PointerPressed += OnCardPointerPressed;
            Canvas.SetLeft(card, node.X);
            Canvas.SetTop(card, node.Y);
            RootCanvas.Children.Add(card);
            _cards[node.Id] = card;
            AddAnchors(node);
        }
        UpdateWorldBounds();
        Dispatcher.UIThread.Post(RefreshLinksAfterLayout, DispatcherPriority.Loaded);
    }
    private void RefreshLinksAfterLayout()
    {
        if (_vm is null) return;
        RemoveRelationShapes();
        var buffer = new Canvas();
        DiagramRenderer.RenderAllRelations(
            buffer, _vm.Project, _vm.Nodes.ToList(), GetBounds, _vm.Project.Language, _selectedRelationId);
        var shapes = buffer.Children.ToList();
        foreach (var shape in shapes)
        {
            buffer.Children.Remove(shape);
            if (shape is Line { Tag: RelationVisualTag tag } && tag.Part == RelationVisualPart.HitLine)
                shape.PointerPressed += OnRelationPointerPressed;
        }
        for (var i = shapes.Count - 1; i >= 0; i--)
            RootCanvas.Children.Insert(0, shapes[i]);
        UpdateAnchorPositions();
        UpdateWorldBounds();
    }
    private void RemoveRelationShapes()
    {
        foreach (var c in RootCanvas.Children.Where(c => c.Tag is RelationVisualTag).ToList())
        {
            if (c is Line line)
                line.PointerPressed -= OnRelationPointerPressed;
            RootCanvas.Children.Remove(c);
        }
    }
    private void AddAnchors(ClassNodeViewModel node)
    {
        var ports = new Dictionary<ConnectionPort, Ellipse>();
        foreach (ConnectionPort port in Enum.GetValues<ConnectionPort>())
        {
            var ellipse = new Ellipse
            {
                Width = AnchorSize,
                Height = AnchorSize,
                Fill = GetAnchorBrush(),
                Stroke = GetAnchorStroke(),
                StrokeThickness = 1.5,
                Tag = (node, port),
                Cursor = new Cursor(StandardCursorType.Cross),
                Opacity = 0.85
            };
            ellipse.PointerPressed += OnAnchorPointerPressed;
            ToolTip.SetTip(ellipse, HelpTexts.AnchorPoint);
            RootCanvas.Children.Add(ellipse);
            ports[port] = ellipse;
        }
        _anchors[node.Id] = ports;
        UpdateAnchorPositions(node);
    }
    private void UpdateAnchorPositions(ClassNodeViewModel? only = null)
    {
        if (_vm is null) return;
        var nodes = only is null ? _vm.Nodes : _vm.Nodes.Where(n => n.Id == only.Id);
        foreach (var node in nodes)
        {
            if (!_anchors.TryGetValue(node.Id, out var ports)) continue;
            var bounds = GetBounds(node);
            foreach (var (port, ellipse) in ports)
            {
                var pt = DiagramGeometry.GetPortPoint(bounds, port);
                Canvas.SetLeft(ellipse, pt.X - AnchorSize / 2);
                Canvas.SetTop(ellipse, pt.Y - AnchorSize / 2);
            }
        }
    }
    private Rect GetBounds(ClassNodeViewModel node)
    {
        if (_cards.TryGetValue(node.Id, out var card))
        {
            var w = card.Bounds.Width > 1 ? card.Bounds.Width : DiagramGeometry.DefaultCardWidth;
            var h = card.Bounds.Height > 1 ? card.Bounds.Height : DiagramGeometry.DefaultCardHeight;
            return DiagramGeometry.GetBounds(node, w, h);
        }
        return DiagramGeometry.GetBounds(node, DiagramGeometry.DefaultCardWidth, DiagramGeometry.DefaultCardHeight);
    }
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var anchor = e.GetPosition(Scroller);
        var factor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
        SetZoom(_zoom * factor, anchor);
        e.Handled = true;
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _spaceHeld = true;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Delete && _selectedRelationId is { } relId && _vm is not null)
        {
            DeleteRelation(relId);
            e.Handled = true;
        }
    }
    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            _spaceHeld = false;
    }
    private void DeleteRelation(Guid relationId)
    {
        if (_vm is null) return;
        _vm.Project.Relations.RemoveAll(r => r.Id == relationId);
        if (_selectedRelationId == relationId)
            _selectedRelationId = null;
        _vm.StatusMessage = "Связь удалена (Delete)";
        RefreshLinksAfterLayout();
    }
    private void StartPan(PointerPressedEventArgs e)
    {
        _isPanning = true;
        _panLast = e.GetPosition(Scroller);
        e.Pointer.Capture(Scroller);
        e.Handled = true;
    }
    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm is null || sender is not ClassCard card || card.DataContext is not ClassNodeViewModel node)
            return;
        var props = e.GetCurrentPoint(RootCanvas).Properties;
        if (props.IsMiddleButtonPressed)
        {
            StartPan(e);
            return;
        }
        if (_spaceHeld && props.IsLeftButtonPressed)
        {
            StartPan(e);
            return;
        }
        if (!props.IsLeftButtonPressed)
            return;
        _selectedRelationId = null;
        RefreshLinksAfterLayout();
        _vm.SelectFolder(null);
        try
        {
            _vm.SelectNode(node);
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = $"Ошибка выбора: {ex.Message}";
            return;
        }
        _dragNode = node;
        var pos = e.GetPosition(RootCanvas);
        _dragOffset = new Point(pos.X - node.X, pos.Y - node.Y);
        e.Pointer.Capture(card);
        e.Handled = true;
    }
    private void OnAnchorPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm is null || sender is not Ellipse el || el.Tag is not (ClassNodeViewModel node, ConnectionPort port))
            return;
        if (!e.GetCurrentPoint(RootCanvas).Properties.IsLeftButtonPressed)
            return;
        HideRelationPicker();
        _selectedRelationId = null;
        var bounds = GetBounds(node);
        var start = DiagramGeometry.GetPortPoint(bounds, port);
        _pendingLink = new PendingLink(node.Id, port, start);
        _previewLine = new Line
        {
            StartPoint = start,
            EndPoint = start,
            Stroke = GetAnchorStroke(),
            StrokeThickness = 2,
            StrokeDashArray = [4, 3],
            IsHitTestVisible = false
        };
        RootCanvas.Children.Add(_previewLine);
        e.Pointer.Capture(el);
        e.Handled = true;
    }
    private void OnRelationPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm is null || sender is not Line { Tag: RelationVisualTag tag } ||
            tag.Part != RelationVisualPart.HitLine || tag.RelationId is not { } relId)
            return;
        var props = e.GetCurrentPoint(RootCanvas).Properties;
        if (props.IsRightButtonPressed)
        {
            DeleteRelation(relId);
            e.Handled = true;
            return;
        }
        if (!props.IsLeftButtonPressed)
            return;
        var rel = _vm.Project.Relations.FirstOrDefault(r => r.Id == relId);
        if (rel is null) return;
        HideRelationPicker();
        _selectedRelationId = rel.Id;
        RefreshLinksAfterLayout();
        var pos = e.GetPosition(RootCanvas);
        if (!TryGetRelationEndpoints(rel, out var start, out var end))
            return;
        var distFrom = Distance(pos, start);
        var distTo = Distance(pos, end);
        if (distFrom <= EndpointGrabRadius)
        {
            _relationDrag = rel;
            _dragRelationFrom = true;
            _dragRelationTo = false;
        }
        else if (distTo <= EndpointGrabRadius)
        {
            _relationDrag = rel;
            _dragRelationFrom = false;
            _dragRelationTo = true;
        }
        else
        {
            _vm.StatusMessage = $"{RelationKindHelper.DisplayName(rel.Kind, _vm.Project.Language)} — Delete или ПКМ для удаления";
            return;
        }
        e.Pointer.Capture(RootCanvas);
        e.Handled = true;
    }
    private (ClassNodeViewModel node, ConnectionPort port)? HitTestAnchor(Point pos)
    {
        if (_vm is null) return null;
        foreach (var (nodeId, ports) in _anchors)
        {
            foreach (var (port, ellipse) in ports)
            {
                var left = Canvas.GetLeft(ellipse);
                var top = Canvas.GetTop(ellipse);
                var rect = new Rect(left - 6, top - 6, AnchorSize + 12, AnchorSize + 12);
                if (!rect.Contains(pos)) continue;
                var node = _vm.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node is not null)
                    return (node, port);
            }
        }
        return null;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isPanning)
        {
            var pos = e.GetPosition(Scroller);
            var dx = pos.X - _panLast.X;
            var dy = pos.Y - _panLast.Y;
            Scroller.Offset = new Vector(
                Math.Max(0, Scroller.Offset.X - dx),
                Math.Max(0, Scroller.Offset.Y - dy));
            _panLast = pos;
            return;
        }
        if (_relationDrag is not null)
        {
            var pos = e.GetPosition(RootCanvas);
            var anchor = HitTestAnchor(pos);
            if (_dragRelationFrom && anchor is { } fromA && fromA.node.Id == _relationDrag.FromClassId)
                _relationDrag.FromPort = fromA.port;
            else if (_dragRelationTo && anchor is { } toA && toA.node.Id == _relationDrag.ToClassId)
                _relationDrag.ToPort = toA.port;
            RefreshLinksAfterLayout();
            return;
        }
        if (_pendingLink is not null && _previewLine is not null)
        {
            _previewLine.EndPoint = e.GetPosition(RootCanvas);
            return;
        }
        if (_dragFolder is not null)
        {
            var folderPos = e.GetPosition(RootCanvas);
            var newX = Math.Max(0, folderPos.X - _folderDragOffset.X);
            var newY = Math.Max(0, folderPos.Y - _folderDragOffset.Y);
            var dx = newX - _dragFolder.X;
            var dy = newY - _dragFolder.Y;
            _dragFolder.X = newX;
            _dragFolder.Y = newY;
            _dragFolder.SyncToModel();
            if (_folderContainers.TryGetValue(_dragFolder.Id, out var folderUi))
            {
                Canvas.SetLeft(folderUi, _dragFolder.X);
                Canvas.SetTop(folderUi, _dragFolder.Y);
            }
            if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
                MoveClassesWithFolder(_dragFolder.Id, dx, dy);
            return;
        }
        if (_dragNode is null || e.Pointer.Captured is null)
            return;
        var canvasPos = e.GetPosition(RootCanvas);
        _dragNode.X = Math.Max(0, canvasPos.X - _dragOffset.X);
        _dragNode.Y = Math.Max(0, canvasPos.Y - _dragOffset.Y);
        _dragNode.SyncToModel();
        if (_cards.TryGetValue(_dragNode.Id, out var card))
        {
            Canvas.SetLeft(card, _dragNode.X);
            Canvas.SetTop(card, _dragNode.Y);
        }
        UpdateAnchorPositions(_dragNode);
        RefreshLinksAfterLayout();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            return;
        }
        if (_relationDrag is not null)
        {
            var pos = e.GetPosition(RootCanvas);
            var anchor = HitTestAnchor(pos);
            if (_dragRelationFrom && anchor is { } fromA && fromA.node.Id == _relationDrag.FromClassId)
                _relationDrag.FromPort = fromA.port;
            if (_dragRelationTo && anchor is { } toA && toA.node.Id == _relationDrag.ToClassId)
                _relationDrag.ToPort = toA.port;
            _relationDrag = null;
            _dragRelationFrom = false;
            _dragRelationTo = false;
            e.Pointer.Capture(null);
            RefreshLinksAfterLayout();
            _vm!.StatusMessage = "Точка связи перемещена";
            return;
        }
        if (_pendingLink is not null && _vm is not null)
        {
            var pos = e.GetPosition(RootCanvas);
            var target = HitTestAnchor(pos);
            if (target is { } t && t.node.Id != _pendingLink.FromNodeId &&
                !_vm.Project.Relations.Any(r =>
                    r.FromClassId == _pendingLink.FromNodeId && r.ToClassId == t.node.Id &&
                    r.FromPort == _pendingLink.FromPort && r.ToPort == t.port))
            {
                var fromNode = _vm.Nodes.FirstOrDefault(n => n.Id == _pendingLink.FromNodeId);
                if (fromNode is not null)
                {
                    _pendingLinkTarget = new PendingLinkTarget(
                        _pendingLink.FromNodeId, _pendingLink.FromPort, t.node.Id, t.port);
                    ShowRelationPicker(fromNode.Model, t.node.Model, pos);
                }
            }
            CancelPendingLink();
        }
        if (_dragNode is not null && _vm is not null)
            AssignClassToFolder(_dragNode);
        _dragNode = null;
        if (_dragFolder is not null)
        {
            _dragFolder.SyncToModel();
            _dragFolder = null;
        }
        if (e.Pointer.Captured is not null)
            e.Pointer.Capture(null);
    }

    private void AssignClassToFolder(ClassNodeViewModel node)
    {
        if (_vm is null) return;
        var bounds = GetBounds(node);
        var center = bounds.Center;
        var folder = ProjectPathHelper.FindInnermostFolderAt(_vm.Project, center);
        var newFolderId = folder?.Id;
        if (node.Model.FolderId == newFolderId)
            return;

        ProjectPathHelper.ApplyFolderAssignment(node.Model, _vm.Project, newFolderId);
        node.SyncFromModel();
        if (_cards.TryGetValue(node.Id, out var card))
            card.DataContext = node;

        _vm.StatusMessage = newFolderId is null
            ? $"«{node.Model.Name}» в корне → {ProjectPathHelper.GetEffectivePathRoot(node.Model, _vm.Project)}"
            : $"«{node.Model.Name}» в «{folder!.Name}» → {ProjectPathHelper.GetEffectivePathRoot(node.Model, _vm.Project)}";
        _vm.OnDiagramChanged();
    }

    private void ShowRelationPicker(ClassDefinition from, ClassDefinition to, Point canvasPos)
    {
        if (_vm is null) return;
        RelationPicker.Configure(from, to, _vm.Project.Language);
        RelationPicker.IsVisible = true;
        var screenPos = RootCanvas.TranslatePoint(canvasPos, this) ?? canvasPos;
        var x = Math.Clamp(screenPos.X, 8, Math.Max(8, Bounds.Width - 290));
        var y = Math.Clamp(screenPos.Y, 8, Math.Max(8, Bounds.Height - 220));
        RelationPicker.Margin = new Thickness(x, y, 0, 0);
        Focus();
    }
    private void HideRelationPicker()
    {
        RelationPicker.IsVisible = false;
        _pendingLinkTarget = null;
    }
    private void OnRelationPickerConfirmed(object? sender, RelationPickerResult result)
    {
        if (_vm is null || _pendingLinkTarget is null) return;
        var fromNode = _vm.Nodes.FirstOrDefault(n => n.Id == _pendingLinkTarget.FromNodeId);
        var toNode = _vm.Nodes.FirstOrDefault(n => n.Id == _pendingLinkTarget.ToNodeId);
        if (fromNode is null || toNode is null)
        {
            HideRelationPicker();
            return;
        }
        var rel = new RelationDefinition
        {
            FromClassId = _pendingLinkTarget.FromNodeId,
            ToClassId = _pendingLinkTarget.ToNodeId,
            FromPort = _pendingLinkTarget.FromPort,
            ToPort = _pendingLinkTarget.ToPort,
            Kind = result.Kind,
            MemberName = result.MemberName,
            CreateNewMember = result.CreateNewMember
        };
        _vm.Project.Relations.Add(rel);
        RelationKindHelper.ApplyToModel(rel, fromNode.Model, toNode.Model, _vm.Project.Language);
        fromNode.SyncFromModel();
        if (_cards.TryGetValue(fromNode.Id, out var card))
            card.DataContext = fromNode;
        _vm.StatusMessage = $"Связь: {RelationKindHelper.DisplayName(result.Kind, _vm.Project.Language)}";
        HideRelationPicker();
        RefreshLinksAfterLayout();
        _vm.OnDiagramChanged();
    }
    private void OnRelationPickerCancelled(object? sender, EventArgs e) => HideRelationPicker();
    private void CancelPendingLink()
    {
        if (_previewLine is not null)
        {
            RootCanvas.Children.Remove(_previewLine);
            _previewLine = null;
        }
        _pendingLink = null;
    }
    private void OnCanvasBackgroundPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm is null) return;
        var props = e.GetCurrentPoint(RootCanvas).Properties;
        if (props.IsMiddleButtonPressed || (_spaceHeld && props.IsLeftButtonPressed))
        {
            StartPan(e);
            return;
        }
        if (e.Source != RootCanvas) return;
        if (props.IsLeftButtonPressed)
        {
            _selectedRelationId = null;
            RefreshLinksAfterLayout();
            HideRelationPicker();
        }
        _vm.DeselectAll();
        CancelPendingLink();
        Focus();
    }

    private void HookFolder(FolderContainer container)
    {
        if (container.FindControl<Border>("TitleBar") is { } titleBar)
        {
            titleBar.Tag = container;
            titleBar.PointerPressed += OnFolderTitlePointerPressed;
        }
    }

    private void UnhookFolder(FolderContainer container)
    {
        if (container.FindControl<Border>("TitleBar") is { } titleBar)
            titleBar.PointerPressed -= OnFolderTitlePointerPressed;
    }

    private void OnFolderTitlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm is null || sender is not Border { Tag: FolderContainer container } ||
            container.DataContext is not FolderNodeViewModel folder)
            return;
        if (!e.GetCurrentPoint(RootCanvas).Properties.IsLeftButtonPressed)
            return;

        _vm.SelectFolder(folder.Model);
        _dragFolder = folder;
        var pos = e.GetPosition(RootCanvas);
        _folderDragOffset = new Point(pos.X - folder.X, pos.Y - folder.Y);
        e.Pointer.Capture(container);
        e.Handled = true;
    }

    private void MoveClassesWithFolder(Guid folderId, double dx, double dy)
    {
        if (_vm is null) return;
        foreach (var node in _vm.Nodes.Where(n => n.Model.FolderId == folderId))
        {
            node.X += dx;
            node.Y += dy;
            node.SyncToModel();
            if (_cards.TryGetValue(node.Id, out var card))
            {
                Canvas.SetLeft(card, node.X);
                Canvas.SetTop(card, node.Y);
            }
            UpdateAnchorPositions(node);
        }
        RefreshLinksAfterLayout();
    }
    private bool TryGetRelationEndpoints(RelationDefinition rel, out Point start, out Point end)
    {
        start = default;
        end = default;
        if (_vm is null) return false;
        var from = _vm.Nodes.FirstOrDefault(n => n.Id == rel.FromClassId);
        var to = _vm.Nodes.FirstOrDefault(n => n.Id == rel.ToClassId);
        if (from is null || to is null) return false;
        (start, end) = DiagramGeometry.ResolveEndpoints(from, to, rel, GetBounds);
        return true;
    }
    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RootCanvas.PointerPressed += OnCanvasBackgroundPressed;
        Focus();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RootCanvas.PointerPressed -= OnCanvasBackgroundPressed;
        if (_vm is not null)
            _vm.DiagramInvalidated -= OnDiagramInvalidated;
        base.OnDetachedFromVisualTree(e);
    }
    private static IBrush GetAnchorBrush() =>
        Application.Current?.TryFindResource("BrushNeonCyan", out var r) == true && r is IBrush b
            ? b
            : Brushes.Cyan;
    private static IBrush GetAnchorStroke() =>
        Application.Current?.TryFindResource("BrushNeonPink", out var r) == true && r is IBrush b
            ? b
            : Brushes.Magenta;
}
