using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public interface ILocalizationProvider : IStringLocalizer
{
    IReadOnlyCollection<CultureInfo> SupportedCultures { get; }
    
    CultureInfo CurrentCulture { get; }

    public Task SetCurrentCultureInfo(CultureInfo culture, CancellationToken cancellationToken);
    
    IStringLocalizer CreateScopedLocalizer(string prefix);
}