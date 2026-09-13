using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public static class StringLocalizerExtensions
{
    extension(IStringLocalizer provider)
    {
        public string GetLocal(CultureInfo culture) 
            => provider[$"Locals.{culture.Name}.Name"];

        public string GetToolTitle(string toolName) 
            => provider[$"Tools.{NormalizeToolName(toolName)}.Name"];

        public string GetToolDescription(string toolName) 
            => provider[$"Tools.{NormalizeToolName(toolName)}.Description"];

        public string GetToolEnum<TEnum>(string toolName, TEnum value) 
            where TEnum: Enum 
            => provider[$"Tools.{NormalizeToolName(toolName)}.Enums.{typeof(TEnum).Name}.{value.ToString()}"];

        public string GetEnum<TEnum>(TEnum value)
            where TEnum: Enum
            => provider[$"Enums.{typeof(TEnum).Name}.{value.ToString()}"];
    }

    /// <summary>
    /// The resource-key stem for a tool: its class name without the "Tool" suffix.
    /// A plain Replace would also strike the word out of the middle of a name, and the generators
    /// build the other half of these keys - keep the two trims in step.
    /// </summary>
    internal static string NormalizeToolName(string toolName)
        => toolName.Length > 4 && toolName.EndsWith("Tool", StringComparison.Ordinal)
            ? toolName[..^4]
            : toolName;
}