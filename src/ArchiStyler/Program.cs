using Avalonia;
using System;
using System.IO;

namespace ArchiStyler;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var log = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.WriteAllText(log, e.ExceptionObject.ToString() ?? "Unknown error");
            }
            catch
            {
                // ignore logging failures
            }
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
