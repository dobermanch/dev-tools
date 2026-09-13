using System.ComponentModel;
using System.Reflection;
using Dev.Tools.Providers;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

/// <summary>
/// Serves the English text the tools carry in their own source, under the same keys the RESX files
/// use. Placed last in <see cref="CompositeStringLocalizer"/>, so a translation wins where one
/// exists and English is what shows through where one does not - which is every key of every tool
/// added after this, since authoring a tool no longer involves touching a RESX file at all.
///
/// It is also the key inventory a translation file is checked against - see tools/Translations.
/// </summary>
public sealed class ToolCatalogStringLocalizer(IToolsProvider tools) : IStringLocalizer
{
    private readonly Lazy<IReadOnlyDictionary<string, string>> _strings = new(() => Build(tools));

    public LocalizedString this[string name]
        => _strings.Value.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, resourceNotFound: false, searchedLocation: nameof(ToolCatalogStringLocalizer))
            : new LocalizedString(name, name, resourceNotFound: true, searchedLocation: nameof(ToolCatalogStringLocalizer));

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = this[name];

            return value.ResourceNotFound
                ? value
                : new LocalizedString(name, string.Format(value.Value, arguments), resourceNotFound: false);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => _strings.Value.Select(it => new LocalizedString(it.Key, it.Value, resourceNotFound: false));

    private static IReadOnlyDictionary<string, string> Build(IToolsProvider tools)
    {
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var tool in tools.GetToolDefinitions())
        {
            // The RESX keys are built from the class name without its "Tool" suffix,
            // the same shape StringLocalizerExtensions asks for - and by the same trim, so a
            // lookup and the inventory it is checked against cannot drift apart.
            var prefix = $"Tools.{StringLocalizerExtensions.NormalizeToolName(tool.ToolType.Name)}";

            strings[$"{prefix}.Name"] = tool.Title;
            strings[$"{prefix}.Description"] = tool.Description;

            AddProperties(strings, $"{prefix}.Args", tool.ArgsType.Properties);
            AddProperties(strings, $"{prefix}.Result", tool.ReturnType.Properties);

            foreach (var type in tool.ExtraTypes.OfType<ToolDefinition.EnumDetails>())
            {
                foreach (var value in type.Values)
                {
                    strings[$"{prefix}.Enums.{type.Name}.{value.Name}"] = value.Description;
                }
            }
        }

        AddEnum<Category>(strings);
        AddEnum<Keyword>(strings);
        AddEnum<ErrorCode>(strings);

        return strings;
    }

    private static void AddProperties(
        Dictionary<string, string> strings,
        string prefix,
        IEnumerable<ToolDefinition.TypeProperty> properties)
    {
        foreach (var property in properties)
        {
            strings[$"{prefix}.{property.Name}.Name"] = property.Label;
            strings[$"{prefix}.{property.Name}.Description"] = property.Description;
        }
    }

    private static void AddEnum<TEnum>(Dictionary<string, string> strings)
        where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();
            var description = typeof(TEnum)
                .GetField(name, BindingFlags.Public | BindingFlags.Static)
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description;

            // Members whose label is already their name carry no attribute - see Category, Keyword.
            strings[$"Enums.{typeof(TEnum).Name}.{name}"] = string.IsNullOrEmpty(description) ? name : description!;
        }
    }
}
