using System.ComponentModel;
using ArchiStyler.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ArchiStyler.Controls;

public partial class FolderContainer : UserControl
{
    private FolderNodeViewModel? _node;

    public FolderContainer()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindNode();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnbindNode();
        base.OnDetachedFromVisualTree(e);
    }

    private void BindNode()
    {
        UnbindNode();
        _node = DataContext as FolderNodeViewModel;
        if (_node is null) return;
        _node.PropertyChanged += OnNodePropertyChanged;
        ApplySelection(_node.IsSelected);
    }

    private void UnbindNode()
    {
        if (_node is not null)
            _node.PropertyChanged -= OnNodePropertyChanged;
        _node = null;
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FolderNodeViewModel.IsSelected) || _node is null)
            return;
        ApplySelection(_node.IsSelected);
    }

    private void ApplySelection(bool selected) =>
        FolderBorder.Classes.Set("selected", selected);
}
