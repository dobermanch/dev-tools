using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public static class StringLocalizerExtensions
{
    public static string GetLocal(this IStringLocalizer provider, CultureInfo culture) 
        => provider[$"Locals.{culture.Name}.Name"];

    public static string GetToolTitle(this IStringLocalizer provider, string toolName) 
        => provider[$"Tools.{NormalizeToolName(toolName)}.Name"];

    public static string GetToolDescription(this IStringLocalizer provider, string toolName) 
        => provider[$"Tools.{NormalizeToolName(toolName)}.Description"];
    
    public static string GetEnum<T>(this IStringLocalizer provider, T value)
        where T: Enum
        => provider[$"Enums.{typeof(T).Name}.{value.ToString()}"];

    private static string NormalizeToolName(string toolName) 
        => toolName.Replace("Tool", "");
}