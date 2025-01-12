using System.Globalization;

namespace Dev.Tools.Web.Services.Localization;

internal sealed class LocalizationProvider : ILocalizationProvider
{
    public IReadOnlyCollection<CultureInfo> SupportedCultures { get; } = [];
    
    public string GetLocalizedString(string key)
    {
        return key;
    }
}