using System.Collections.ObjectModel;
using ArchiStyler.Helpers;
using ArchiStyler.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchiStyler.ViewModels;

public partial class StartupViewModel : ViewModelBase
{
    [ObservableProperty] private TargetLanguage _selectedLanguage = TargetLanguage.CSharp;
    [ObservableProperty] private string _projectName = "MyArchitecture";
    [ObservableProperty] private string _rootPath = "";
    [ObservableProperty] private string _defaultNamespace = "App.Architecture";
    [ObservableProperty] private string _defaultPackage = "app.architecture";
    [ObservableProperty] private string? _statusMessage;

    public ObservableCollection<TargetLanguage> Languages { get; } =
        new([TargetLanguage.CSharp, TargetLanguage.Java]);

    public string LanguageLabel => EnumDisplay.FormatLanguage(SelectedLanguage);

    partial void OnSelectedLanguageChanged(TargetLanguage value)
    {
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(ShowsNamespace));
        OnPropertyChanged(nameof(ShowsPackage));
    }

    public bool ShowsNamespace => SelectedLanguage == TargetLanguage.CSharp;
    public bool ShowsPackage => SelectedLanguage == TargetLanguage.Java;

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Выберите папку проекта",
            AllowMultiple = false
        };

        // Folder picker via OpenFolderDialog on window - handled in view code-behind
        StatusMessage = "Используйте кнопку «Обзор» в окне для выбора папки.";
    }

    public bool CanStart =>
        !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(RootPath) &&
        Directory.Exists(RootPath);

    public ProjectModel BuildProject() => new()
    {
        Name = ProjectName.Trim(),
        RootPath = RootPath.Trim(),
        Language = SelectedLanguage,
        DefaultNamespace = DefaultNamespace.Trim(),
        DefaultPackage = DefaultPackage.Trim()
    };
}
