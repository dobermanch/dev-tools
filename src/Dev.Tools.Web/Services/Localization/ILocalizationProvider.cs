using System.Globalization;

namespace Dev.Tools.Web.Services.Localization;

public interface ILocalizationProvider
{
    IReadOnlyCollection<CultureInfo> SupportedCultures { get; }
}