using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Dev.Tools.Localization;

internal static class LocalizerExtensions
{
    extension(IStringLocalizer provider)
    {
        public string GetToolTitle(ToolDefinition tool) 
            => provider.GetToolTitle(tool.ToolType.Name);

        public string GetToolDescription(ToolDefinition tool) 
            => provider.GetToolDescription(tool.ToolType.Name);
        
        public string GetToolEnum<TEnum>(ToolDefinition tool, TEnum value)
            where TEnum: Enum 
            => provider.GetToolEnum(tool.ToolType.Name, value);

        public string GetKeyword(Keyword value) 
            => provider.GetEnum(value);

        public string GetCategory(Category value) 
            => provider.GetEnum(value);

        public string GetErrorCode(ErrorCode value) 
            => provider.GetEnum(value);
    }

    extension<TComponent>(ILocalizationProvider provider)
    {
        public IStringLocalizer PageLocalizer()
         => provider.CreateScopedLocalizer($"Page.{typeof(TComponent).Name}");
        
        public IStringLocalizer ComponentLocalizer()
            => provider.CreateScopedLocalizer($"Component.{typeof(TComponent).Name}");
    }
}