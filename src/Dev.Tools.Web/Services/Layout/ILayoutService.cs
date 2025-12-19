namespace Dev.Tools.Web.Services.Layout;

public interface ILayoutService
{
    bool IsDrawerOpen { get; }

    bool IsDarkMode { get; }

    ThemeMode ThemeMode { get; }

    Task InitAsync(bool isDarkMode, CancellationToken cancellationToken = default);

    Task SetSystemModeAsync(bool isDarkMode);

    Task ToggleDarkModeAsync(CancellationToken cancellationToken = default);

    Task SetThemeModeAsync(ThemeMode mode, CancellationToken cancellationToken = default);

    Task ToggleDrawerAsync(CancellationToken cancellationToken = default);
}