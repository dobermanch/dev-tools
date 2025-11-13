using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Layout;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Layout;

public partial class MainLayout
{
    private MudThemeProvider _themeProvider = null!;
    
    [Inject]
    private WebContext Context { get; set; } = null!;

    private bool ObserveSystemThemeChange => true;

    private bool IsDarkMode => Context.Layout.IsDarkMode;

    private bool IsDrawerOpen => Context.Layout.IsDrawerOpen;

    private string LightModeIcon =>
        Context.Layout.ThemeMode switch
        {
            ThemeMode.Dark => Icons.Material.Outlined.DarkMode,
            ThemeMode.Light => Icons.Material.Outlined.LightMode,
            _ => Icons.Material.Outlined.AutoMode
        };

    protected override async Task OnInitializedAsync()
    {
        await Context.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        Context.Messenger.Subscribe<LayoutChangedNotification>(HandlerUpdateRequest);
        Context.Messenger.Subscribe<LocalHasChangedNotification>(HandlerLocalHasChanged);
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await Context.Layout.InitAsync(await _themeProvider.GetSystemDarkModeAsync());
            await _themeProvider.WatchSystemDarkModeAsync(Context.Layout.SetSystemModeAsync);
        }
    }

    private async Task DrawerToggle() 
        => await Context.Layout.ToggleDrawerAsync();

    private async Task ChangeThemeModeAsync() 
        => await Context.Layout.ToggleDarkModeAsync();

    private void HandlerUpdateRequest(LayoutChangedNotification _) 
        => StateHasChanged();
    
    private void HandlerLocalHasChanged(LocalHasChangedNotification _)
        => StateHasChanged();
}
