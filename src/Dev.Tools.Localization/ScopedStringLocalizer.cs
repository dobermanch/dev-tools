using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

internal sealed class ScopedStringLocalizer(IStringLocalizer localizer, string? scopePrefix = null) : IStringLocalizer
{
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => localizer.GetAllStrings(includeParentCultures);

    public LocalizedString this[string name] 
        => this[name, []];

    public LocalizedString this[string name, params object[] arguments]
        => localizer[scopePrefix is null ? name : $"{scopePrefix}{LocalizationProvider.KeyDelimiter}{name}", arguments];
}