using System.Globalization;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Services.Localization;

internal sealed class WebLocalizationProvider(
    IStringLocalizer<Dev.Tools.Localization.Resources.Locals> localLocalizer,
    IEnumerable<IStringLocalizer> localizers,
    IPreferencesService preferencesService,
    NavigationManager navigationManager
) : LocalizationProvider(localLocalizer, localizers, true)
{
    public override async Task SetCurrentCultureInfo(CultureInfo culture, bool reload, CancellationToken cancellationToken)
    {
        var previousCulture = CurrentCulture.Name;

        if (!SetCulture(culture))
        {
            return;
        }

        if (!reload)
        {
            return;
        }

        var preference = preferencesService.Preferences with
        {
            Localization = new UserPreferences.LocalizationSettings
            {
                Culture = CurrentCulture.Name
            }
        };

        await preferencesService.UpdatePreferencesAsync(preference, CancellationToken.None);

        // Reload the page so the WASM runtime loads satellite assemblies for the new culture.
        // Skip reload if the culture didn't actually change (e.g. during startup initialization).
        if (!string.Equals(previousCulture, CurrentCulture.Name, StringComparison.Ordinal))
        {
            navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
        }
    }
}