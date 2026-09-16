using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dev.Tools.Web.Layout;

public partial class MainLayout
{
    private MudThemeProvider _themeProvider = null!;
    private IStringLocalizer _localizer = null!;
    private DotNetObjectReference<MainLayout>? _self;
    private bool? _appliedDarkMode;
    private bool _isMac;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private bool ObserveSystemThemeChange => true;

    private bool IsDarkMode => Context.Layout.IsDarkMode;

    private bool IsDrawerOpen => Context.Layout.IsDrawerOpen;

    /// <summary>What the shortcut is called on this keyboard.</summary>
    private string ShortcutLabel => _isMac ? "⌘K" : "Ctrl K";

    /// <summary>
    /// Under the wordmark: the version once there is one, and what the app is either way. A development
    /// build has no version, so it says only the latter.
    /// </summary>
    private string BrandLine => Context.AppDetails.Version is { } version
        ? $"v{version} · {_localizer["Text.Tagline"]}"
        : _localizer["Text.Tagline"];

    private string LightModeIcon =>
        Context.Layout.ThemeMode switch
        {
            ThemeMode.Dark => Icons.Material.Outlined.DarkMode,
            ThemeMode.Light => Icons.Material.Outlined.LightMode,
            _ => Icons.Material.Outlined.AutoMode
        };

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Layout.MainLayout");
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

            _self = DotNetObjectReference.Create(this);
            _isMac = await Context.JsService.RegisterCommandShortcutAsync(_self, nameof(OpenSearchFromShortcutAsync));
            StateHasChanged();
        }

        // MudBlazor themes its own components; the design tokens in tokens.css follow [data-theme]
        // on <html>, so the page around them changes with the theme too.
        if (_appliedDarkMode != IsDarkMode)
        {
            _appliedDarkMode = IsDarkMode;
            await Context.JsService.SetThemeAsync(IsDarkMode);
        }
    }

    [JSInvokable]
    public Task OpenSearchFromShortcutAsync()
        => InvokeAsync(OpenSearchDialogAsync);

    private async Task DrawerToggle()
        => await Context.Layout.ToggleDrawerAsync();

    private async Task ChangeThemeModeAsync()
        => await Context.Layout.ToggleDarkModeAsync();

    private void HandlerUpdateRequest(LayoutChangedNotification _)
        => StateHasChanged();

    private void HandlerLocalHasChanged(LocalHasChangedNotification _)
        => StateHasChanged();

    private async Task OpenSearchDialogAsync()
    {
        var options = new DialogOptions
        {
            NoHeader = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            Position = DialogPosition.TopCenter
        };
        await DialogService.ShowAsync<Components.Inputs.SearchDialog>(null, options);
    }

    public async ValueTask DisposeAsync()
    {
        if (_self is null)
        {
            return;
        }

        try
        {
            await Context.JsService.UnregisterCommandShortcutAsync();
        }
        catch (JSDisconnectedException)
        {
            // The page is going away; there is nothing left to unregister.
        }

        _self.Dispose();
    }
}
