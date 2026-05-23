using ArchiStyler.Helpers;
using ArchiStyler.Models;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArchiStyler.Controls;

public partial class RelationPickerFlyout : UserControl
{
    private ClassDefinition? _from;
    private ClassDefinition? _to;
    private TargetLanguage _language;
    private IReadOnlyList<RelationKind> _kinds = [];

    public event EventHandler<RelationPickerResult>? Confirmed;
    public event EventHandler? Cancelled;

    public RelationPickerFlyout()
    {
        InitializeComponent();
        OkButton.Click += OnOk;
        CancelButton.Click += OnCancel;
        KindCombo.SelectionChanged += (_, _) => UpdateMemberPanel();
    }

    public void Configure(ClassDefinition from, ClassDefinition to, TargetLanguage language)
    {
        _from = from;
        _to = to;
        _language = language;
        _kinds = RelationKindHelper.GetAvailableKinds(from, to);

        TitleText.Text = $"{from.Name} → {to.Name}";
        KindCombo.ItemsSource = _kinds.Select(k => new KindItem(k, RelationKindHelper.DisplayName(k, language))).ToList();
        KindCombo.SelectedIndex = 0;
        UpdateMemberPanel();
    }

    private void UpdateMemberPanel()
    {
        if (KindCombo.SelectedItem is not KindItem item || _from is null)
        {
            MemberPanel.IsVisible = false;
            return;
        }

        var needsMember = item.Kind is RelationKind.FieldReference or RelationKind.MethodReference;
        MemberPanel.IsVisible = needsMember;
        if (!needsMember) return;

        MemberLabel.Text = item.Kind == RelationKind.FieldReference
            ? "Поле (существующее или новое)"
            : "Метод (существующий или новый)";

        var members = (item.Kind == RelationKind.FieldReference
                ? RelationKindHelper.GetFieldCandidates(_from)
                : RelationKindHelper.GetMethodCandidates(_from))
            .Select(m => m.Name)
            .ToList();

        MemberCombo.ItemsSource = members;
        MemberCombo.SelectedItem = members.FirstOrDefault();
        NewMemberBox.Text = item.Kind == RelationKind.FieldReference ? "_dependency" : "Use" + (_to?.Name ?? "");
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        if (KindCombo.SelectedItem is not KindItem item || _from is null || _to is null)
            return;

        string? memberName = null;
        var createNew = false;

        if (item.Kind is RelationKind.FieldReference or RelationKind.MethodReference)
        {
            var typed = NewMemberBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(typed))
            {
                memberName = typed;
                createNew = true;
            }
            else if (MemberCombo.SelectedItem is string existing)
            {
                memberName = existing;
                createNew = false;
            }
            else
            {
                return;
            }
        }

        Confirmed?.Invoke(this, new RelationPickerResult(item.Kind, memberName, createNew));
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);

    private sealed record KindItem(RelationKind Kind, string Label)
    {
        public override string ToString() => Label;
    }
}

public sealed record RelationPickerResult(RelationKind Kind, string? MemberName, bool CreateNewMember);
