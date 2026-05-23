using ArchiStyler.Controls;
using ArchiStyler.Models;
using ArchiStyler.ViewModels;
using ArchiStyler.Views;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArchiStyler;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow() : this(new ProjectModel { Name = "Design", RootPath = "." })
    {
    }

    public MainWindow(ProjectModel project)
    {
        _vm = new MainViewModel(project);
        DataContext = _vm;
        InitializeComponent();
        Diagram.Initialize(_vm);
    }

    private void OnHelpClick(object? sender, RoutedEventArgs e)
    {
        var help = new HelpWindow { WindowStartupLocation = WindowStartupLocation.CenterOwner };
        help.ShowDialog(this);
    }
}
