using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages;

public partial class Settings
{
    [Inject]
    private WebContext Context { get; set; } = null!;

    private IStringLocalizer _localizer = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Settings>();
    }

    private async Task OnThemeChangedAsync(Dev.Tools.Web.Services.Layout.ThemeMode themeMode)
    {
        await Context.Layout.SetThemeModeAsync(themeMode, CancellationToken.None);
    }
}
