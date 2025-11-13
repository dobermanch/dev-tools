using System.Globalization;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Services.Localization;

internal sealed class WebLocalizationProvider(
    IStringLocalizer<Dev.Tools.Localization.Resources.Locals> localLocalizer,
    IEnumerable<IStringLocalizer> localizers,
    IPreferencesService preferencesService,
    IMessenger messenger
) : LocalizationProvider(localLocalizer, localizers, true)
{
    public override async Task SetCurrentCultureInfo(CultureInfo culture, CancellationToken cancellationToken)
    {
        if (!SetCulture(culture))
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
        await messenger.Publish(new LocalHasChangedNotification(CurrentCulture), CancellationToken.None);
    }
}