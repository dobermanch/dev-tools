using System.Globalization;
using Dev.Tools.Localization.Resources;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public class LocalizationProvider : ILocalizationProvider
{
    public const string FallbackCulture = "en-US";
    public const char KeyDelimiter = '.';

    private readonly bool _setThreadDefaultCulture;
    private readonly Dictionary<string, CultureInfo> _supportedCultures;

    public LocalizationProvider(
        IStringLocalizer<Locals> localLocalizer,
        IEnumerable<IStringLocalizer> localizers,
        bool setThreadDefaultCulture = false)
    {
        Localizer = new CompositeStringLocalizer(localizers);
        _setThreadDefaultCulture = setThreadDefaultCulture;

        SupportedCultures = localLocalizer.GetAllStrings(true)
            .Select(it => it.Name.Split(KeyDelimiter))
            .Where(it => it.Length > 1)
            .Select(it => it[1])
            .Select(it => new CultureInfo(it))
            .Distinct()
            .ToList();

        _supportedCultures = SupportedCultures.ToDictionary(it => it.Name);

        if (!_supportedCultures.ContainsKey(CurrentCulture.Name))
        {
            SetCulture(new CultureInfo(FallbackCulture), true);
        }
    }

    protected IStringLocalizer Localizer { get; }

    public IReadOnlyCollection<CultureInfo> SupportedCultures { get; }

    public CultureInfo CurrentCulture => CultureInfo.CurrentUICulture;

    public virtual Task SetCurrentCultureInfo(CultureInfo culture, CancellationToken cancellationToken)
    {
        SetCulture(culture);

        return Task.CompletedTask;
    }

    protected bool SetCulture(CultureInfo culture, bool fallback = false)
    {
        if (!_supportedCultures.TryGetValue(culture.Name, out var cultureInfo) && !fallback)
        {
            return false;
        }

        if (_setThreadDefaultCulture)
        {
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo ?? culture;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo ?? culture;
        }
        
        CultureInfo.CurrentCulture = cultureInfo ?? culture;
        CultureInfo.CurrentUICulture = cultureInfo ?? culture;
        
        return true;
    }

    public IStringLocalizer CreateScopedLocalizer(string prefix)
        => new ScopedStringLocalizer(Localizer, prefix);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => Localizer.GetAllStrings(includeParentCultures);

    public LocalizedString this[string name]
        => Localizer[name];

    public LocalizedString this[string name, params object[] arguments]
        => Localizer[name, arguments];
}