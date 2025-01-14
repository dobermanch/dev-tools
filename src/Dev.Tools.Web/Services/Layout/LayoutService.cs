using Dev.Tools.Web.Services.Preferences;

namespace Dev.Tools.Web.Services.Layout;

internal sealed class LayoutService(IMessenger messenger, IPreferencesService preferencesService) : ILayoutService
{
    private bool _systemIsDarkMode;

    public bool IsDrawerOpen { get; private set; }

    public ThemeMode ThemeMode { get; private set; }

    public bool IsDarkMode => ThemeMode == ThemeMode.System 
        ? _systemIsDarkMode 
        : ThemeMode == ThemeMode.Dark;

    public async Task InitAsync(bool isDarkMode, CancellationToken cancellationToken)
    {
        _systemIsDarkMode = isDarkMode;
        await ApplyUserPreferencesAsync(cancellationToken);
        await NotifyMajorUpdateOccuredAsync(cancellationToken);
    }

    public async Task SetSystemModeAsync(bool isDarkMode)
    {
        _systemIsDarkMode = isDarkMode;
        if (ThemeMode == ThemeMode.System)
        {
            await NotifyMajorUpdateOccuredAsync(CancellationToken.None);
        }
    }

    public async Task ToggleDarkModeAsync(CancellationToken cancellationToken)
    {
        ThemeMode = ThemeMode switch
        {
            ThemeMode.System => ThemeMode.Light,
            ThemeMode.Light => ThemeMode.Dark,
            _ => ThemeMode.System
        };
        
        await StoreUserPreferencesAsync(cancellationToken);
    }

    public async Task ToggleDrawerAsync(CancellationToken cancellationToken)
    {
        IsDrawerOpen = !IsDrawerOpen;
        await StoreUserPreferencesAsync(cancellationToken);
    }

    private Task ApplyUserPreferencesAsync(CancellationToken cancellationToken)
    {
        ThemeMode = preferencesService.Preferences.Layout.ThemeMode ?? ThemeMode.System;
        IsDrawerOpen = preferencesService.Preferences.Layout.IsDrawerOpen ?? true;
        return Task.CompletedTask;
    }

    private async Task StoreUserPreferencesAsync(CancellationToken cancellationToken)
    {
        await preferencesService.UpdateLayoutAsync(new UserPreferences.LayoutSettings
        {
            ThemeMode = ThemeMode,
            IsDrawerOpen = IsDrawerOpen
        }, cancellationToken);
    }

    private async Task NotifyMajorUpdateOccuredAsync(CancellationToken cancellationToken = default) 
        => await messenger.Publish(new LayoutChangedNotification(), cancellationToken);
}
