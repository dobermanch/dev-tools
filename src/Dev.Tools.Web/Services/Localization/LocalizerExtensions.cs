using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Dev.Tools.Localization;

internal static class LocalizerExtensions
{
    public static string GetToolTitle(this IStringLocalizer provider, ToolDefinition tool) 
        => provider.GetToolTitle(tool.ToolType.Name);

    public static string GetToolDescription(this IStringLocalizer provider, ToolDefinition tool) 
        => provider.GetToolDescription(tool.ToolType.Name);
    
    public static string GetKeyword(this IStringLocalizer provider, Keyword value) 
        => provider.GetEnum(value);
    
    public static string GetCategory(this IStringLocalizer provider, Category value) 
        => provider.GetEnum(value);
    
    public static string GetErrorCode(this IStringLocalizer provider, ErrorCode value) 
        => provider.GetEnum(value);
}