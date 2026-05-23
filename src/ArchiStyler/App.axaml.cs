using ArchiStyler.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ArchiStyler;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            try
            {
                var log = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(log, e.Exception.ToString());
            }
            catch
            {
                // ignore
            }
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new StartupWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
