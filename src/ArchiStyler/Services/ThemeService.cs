using ArchiStyler.Models;
using Avalonia;
using Avalonia.Styling;

namespace ArchiStyler.Services;

public static class ThemeService
{
    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    public static event EventHandler? ThemeChanged;

    public static void SetTheme(AppTheme theme)
    {
        Current = theme;
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Dark
        };

        Application.Current.Resources["ActiveTheme"] = theme.ToString();
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Toggle() => SetTheme(Current == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
}
