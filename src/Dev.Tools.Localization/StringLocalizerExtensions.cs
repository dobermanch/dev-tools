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

    private static string NormalizeToolName(string toolName) 
        => toolName.Replace("Tool", "");
}