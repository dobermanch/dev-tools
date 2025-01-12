namespace Dev.Tools.Web.Services.Localization;

public static class LocalizationProviderExtensions
{
    public static string GetToolTitle(this ILocalizationProvider provider, Type tool)
    {
        return provider.GetLocalizedString($"{tool.Name}_Title");
    }
    
    public static string GetToolDescription(this ILocalizationProvider provider, Type tool)
    {
        return provider.GetLocalizedString($"{tool.Name}_Description");
    }
    
    public static string GetToolKeyword(this ILocalizationProvider provider, string keyword)
    {
        return provider.GetLocalizedString($"ToolKeyword_{keyword}");
    }
    
    public static string GetToolCategory(this ILocalizationProvider provider, string category)
    {
        return provider.GetLocalizedString($"ToolCategory_{category}");
    }
}