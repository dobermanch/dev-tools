using System.Globalization;

namespace Dev.Tools.Web.Services.Localization;

public static class LocalizerExtensions
{
    public static string GetString(this ILocalizer provider, CultureInfo culture)
    {
        return provider.GetString($"Locals_{culture.Name}_Title");
    }
    
    public static string GetToolTitle(this ILocalizer provider, Type tool)
    {
        return provider.GetString($"{tool.Name}_Title");
    }
    
    public static string GetToolDescription(this ILocalizer provider, Type tool)
    {
        return provider.GetString($"Tool_{tool.Name}_Description");
    }
    
    public static string GetToolKeyword(this ILocalizer provider, string keyword)
    {
        return provider.GetString($"Tool_Keyword_{keyword}_Title");
    }
    
    public static string GetToolCategory(this ILocalizer provider, string category)
    {
        return provider.GetString($"Tool_Category_{category}_Title");
    }
    
    public static string GetErrorCode(this ILocalizer provider, string code)
    {
        return provider.GetString($"Tool_Category_{code}_Description");
    }
}