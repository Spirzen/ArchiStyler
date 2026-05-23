using ArchiStyler.Helpers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ArchiStyler.Views;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        HelpContent.Text = HelpTexts.FullHelp;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
