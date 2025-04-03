using System.Globalization;

namespace Dev.Tools.Web.Services.Localization;

public interface ILocalizationProvider : ILocalizer
{
    IReadOnlyCollection<CultureInfo> SupportedCultures { get; }
    
    CultureInfo CurrentCulture { get; }

    public Task SetCurrentCultureInfo(CultureInfo culture, CancellationToken cancellationToken = default);
    
    ILocalizer CreateScopedLocalizer(string prefix);
}