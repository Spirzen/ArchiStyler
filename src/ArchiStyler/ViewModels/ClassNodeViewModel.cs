using ArchiStyler.Helpers;
using ArchiStyler.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiStyler.ViewModels;

public partial class ClassNodeViewModel : ViewModelBase
{
    private readonly ProjectModel? _project;

    public ClassDefinition Model { get; }

    public ClassNodeViewModel(ClassDefinition model, ProjectModel? project = null)
    {
        Model = model;
        _project = project;
    }

    public Guid Id => Model.Id;

    public string Title => Model.DisplayTitle;

    public string Subtitle =>
        _project is null
            ? EnumDisplay.FormatRole(Model.Role)
            : $"{EnumDisplay.FormatRole(Model.Role)} · {ProjectPathHelper.GetClassLocationLabel(Model, _project)}";

    public string ExportPath =>
        _project is null
            ? ""
            : ProjectPathHelper.GetRelativeFilePath(
                Model, _project, _project.Language == TargetLanguage.CSharp ? ".cs" : ".java");

    public string MembersPreview
    {
        get
        {
            var lines = Model.Members.Take(6).Select(m => m.Kind switch
            {
                MemberKind.Method => $"  {m.Name}()",
                MemberKind.Property => $"  {m.Name} {{ }}",
                MemberKind.Field => $"  {m.Type} {m.Name}",
                _ => $"  {m.Name}"
            });
            var text = string.Join("\n", lines);
            if (Model.Members.Count > 6)
                text += "\n  …";
            return text;
        }
    }

    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private bool _isSelected;

    public void SyncFromModel()
    {
        X = Model.X;
        Y = Model.Y;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(MembersPreview));
        OnPropertyChanged(nameof(ExportPath));
    }

    public void SyncToModel()
    {
        Model.X = X;
        Model.Y = Y;
    }
}
