using Dev.Tools.Web.Services.Layout;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Layout;

public partial class MainLayout
{
    private MudThemeProvider _themeProvider = null!;

    [Inject] private IMessenger Messenger { get; set; } = null!;
    
    [Inject] private ILayoutService LayoutService { get; set; } = null!;

    private bool ObserveSystemThemeChange => true;

    private bool IsDarkMode => LayoutService.IsDarkMode;

    private bool IsDrawerOpen => LayoutService.IsDrawerOpen;

    private string LightModeIcon =>
        LayoutService.ThemeMode switch
        {
            ThemeMode.Dark => Icons.Material.Outlined.DarkMode,
            ThemeMode.Light => Icons.Material.Outlined.LightMode,
            _ => Icons.Material.Outlined.AutoMode
        };


    protected override void OnInitialized()
    {
        Messenger.Subscribe<LayoutChangedNotification>(HandlerUpdateRequest);

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LayoutService.InitAsync(await _themeProvider.GetSystemPreference());
            await _themeProvider.WatchSystemPreference(LayoutService.SetSystemModeAsync);
        }
    }

    private async Task DrawerToggle() 
        => await LayoutService.ToggleDrawerAsync();

    private async Task ChangeThemeModeAsync() 
        => await LayoutService.ToggleDarkModeAsync();

    private void HandlerUpdateRequest(LayoutChangedNotification _) 
        => StateHasChanged();
}
