using System.ComponentModel;
using ArchiStyler.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ArchiStyler.Controls;

public partial class ClassCard : UserControl
{
    private ClassNodeViewModel? _node;

    public ClassCard()
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
        _node = DataContext as ClassNodeViewModel;
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
        if (e.PropertyName != nameof(ClassNodeViewModel.IsSelected))
            return;
        if (!ReferenceEquals(sender, _node) || _node is null)
            return;
        if (VisualRoot is null)
            return;

        ApplySelection(_node.IsSelected);
    }

    private void ApplySelection(bool selected)
    {
        if (CardBorder is null) return;

        if (selected)
        {
            if (!CardBorder.Classes.Contains("selected"))
                CardBorder.Classes.Add("selected");
        }
        else
        {
            CardBorder.Classes.Remove("selected");
        }
    }
}
