using ArchiStyler.Helpers;
using ArchiStyler.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiStyler.ViewModels;

public partial class FolderNodeViewModel : ViewModelBase
{
    private readonly ProjectModel _project;

    public FolderDefinition Model { get; }

    public FolderNodeViewModel(FolderDefinition model, ProjectModel project)
    {
        Model = model;
        _project = project;
    }

    public Guid Id => Model.Id;

    public string Title => string.IsNullOrWhiteSpace(Model.Name) ? "Папка" : Model.Name;

    public string PathSubtitle => ProjectPathHelper.GetFolderDisplayPath(_project, Model.Id);

    public string ExportSubtitle =>
        _project.Language == TargetLanguage.CSharp
            ? ProjectPathHelper.CombineNamespace(_project.DefaultNamespace, ProjectPathHelper.GetFolderSegments(_project, Model.Id))
            : ProjectPathHelper.CombineNamespace(_project.DefaultPackage, ProjectPathHelper.GetFolderSegments(_project, Model.Id));

    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
    [ObservableProperty] private bool _isSelected;

    public void SyncFromModel()
    {
        X = Model.X;
        Y = Model.Y;
        Width = Model.Width;
        Height = Model.Height;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PathSubtitle));
        OnPropertyChanged(nameof(ExportSubtitle));
    }

    public void SyncToModel()
    {
        Model.X = X;
        Model.Y = Y;
        Model.Width = Width;
        Model.Height = Height;
    }
}
