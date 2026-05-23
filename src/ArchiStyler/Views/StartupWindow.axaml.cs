using ArchiStyler.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ArchiStyler.Views;

public partial class StartupWindow : Window
{
    public StartupViewModel ViewModel { get; }

    public StartupWindow()
    {
        ViewModel = new StartupViewModel();
        DataContext = ViewModel;
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Папка проекта",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            ViewModel.RootPath = folders[0].Path.LocalPath;
    }

    private void OnStartClick(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanStart)
        {
            ViewModel.StatusMessage = "Укажите существующую папку и имя проекта.";
            return;
        }

        var project = ViewModel.BuildProject();
        var main = new MainWindow(project);
        main.Show();
        Close();
    }
}
