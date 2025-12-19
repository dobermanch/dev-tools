using Dev.Tools.Web.Components.Layout;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages;

public partial class Settings
{
    [Inject] 
    private WebContext Context { get; set; } = null!;

    private DtSectionList _sectionListRef = null!;
    private IStringLocalizer _localizer = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Settings>();
    }

    private async Task OnThemeChangedAsync(Dev.Tools.Web.Services.Layout.ThemeMode themeMode)
    {
        await Context.Layout.SetThemeModeAsync(themeMode, CancellationToken.None);
    }

    private async Task ScrollTo(string id)
    {
        await Context.JsService.ScrollToIdAsync(id, 30, CancellationToken.None);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && _sectionListRef is not null)
        {
            StateHasChanged();
        }
    }
}
