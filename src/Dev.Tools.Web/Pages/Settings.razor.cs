using Dev.Tools.Web.Components.Layout;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages;

public partial class Settings
{
    [Inject] 
    private WebContext WebContext { get; set; } = null!;
    

    private Dev.Tools.Web.Services.Layout.ThemeMode _themeMode;
    private string? _culture;
    private DtSectionList _sectionListRef = null!;

    protected override void OnInitialized()
    {
        _themeMode = WebContext.Preferences.Preferences.Layout.ThemeMode ?? Services.Layout.ThemeMode.System;
        _culture = WebContext.Preferences.Preferences.Localization.Culture;
    }

    private async Task SaveLayout()
    {
        await WebContext.Preferences.UpdateLayoutAsync(WebContext.Preferences.Preferences.Layout with
        {
            ThemeMode = _themeMode
        }, CancellationToken.None);
    }

    private async Task SaveLocalization()
    {
        var pref = WebContext.Preferences.Preferences with
        {
            Localization = new UserPreferences.LocalizationSettings
            {
                Culture = _culture
            }
        };

        await WebContext.Preferences.UpdatePreferencesAsync(pref, CancellationToken.None);
    }

    private async Task ScrollTo(string id)
    {
        await WebContext.JsService.ScrollToIdAsync(id, 30, CancellationToken.None);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && _sectionListRef is not null)
        {
            StateHasChanged();
        }
    }
}
