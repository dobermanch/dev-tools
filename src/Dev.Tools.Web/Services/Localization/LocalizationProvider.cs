using System.Globalization;
using Dev.Tools.Web.Locals;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Services.Localization;

internal sealed class LocalizationProvider : ILocalizationProvider
{
    private readonly Localizer _localizer;
    private readonly IStringLocalizer<Resources> _localizer1;
    private readonly IPreferencesService _preferencesService;
    private readonly IMessenger _messenger;

    public LocalizationProvider(IStringLocalizer<Resources> localizer, 
        IPreferencesService preferencesService,
        IMessenger messenger)
    {
        _localizer1 = localizer;
        _preferencesService = preferencesService;
        _messenger = messenger;
        _localizer = new Localizer(localizer);
        
        CurrentCulture = preferencesService.Preferences.Localization.Culture is null
            ? SupportedCultures.First()
            : new CultureInfo(preferencesService.Preferences.Localization.Culture);
    }

    public IReadOnlyCollection<CultureInfo> SupportedCultures { get; } =
    [
        new("en-US"),
        new("uk-UA")
    ];

    public CultureInfo CurrentCulture { get; private set; }

    public async Task SetCurrentCultureInfo(CultureInfo culture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(culture);
        
        var preference = _preferencesService.Preferences with
        {
            Localization = new UserPreferences.LocalizationSettings
            {
                Culture = culture.Name
            }
        };

        CurrentCulture = culture;
        await _preferencesService.UpdatePreferencesAsync(preference, CancellationToken.None);
        await _messenger.Publish(new LocalHasChangedNotification(), CancellationToken.None);
    }

    public ILocalizer CreateScopedLocalizer(string prefix) 
        => new Localizer(_localizer1, prefix);

    public string GetString(string key, params object[] args) 
        => _localizer.GetString(key, args);
}

public interface ILocalizer
{
    string GetString(string key, params object[] args);
}

internal class Localizer(IStringLocalizer<Resources> localizer, string? prefix = null) : ILocalizer
{
    private readonly string _keyPrefix = prefix is null ? string.Empty : $"{prefix}_";

    public string GetString(string key, params object[] args)
    {
        var localizedString = localizer.GetString($"{_keyPrefix}{key}", args);
        return localizedString.ResourceNotFound ? $"::{key.Split("_")[^2]}::" : localizedString.Value;
    }
}