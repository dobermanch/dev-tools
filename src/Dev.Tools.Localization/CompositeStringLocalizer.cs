using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

internal sealed class CompositeStringLocalizer(IEnumerable<IStringLocalizer> localizers, string? scopePrefix = null) : IStringLocalizer
{
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => localizers.SelectMany(localizer => localizer.GetAllStrings(includeParentCultures));

    public LocalizedString this[string name] 
        => this[name, []];

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var key = scopePrefix is null ? name : $"{scopePrefix}.{name}";
            foreach (var localizer in localizers)
            {
                var value = localizer[key];
                if (!value.ResourceNotFound)
                {
                    return value;
                }
            }

            var defaultValue = key;
            var parts = name.Split('.');
            if (parts.Length > 1)
            {
                defaultValue = $":{parts[^2]}:";
            }

            return new LocalizedString(key, defaultValue, resourceNotFound: true, searchedLocation: "<composite>");
        }
    }
}