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

        public string GetKeyword(Keyword value) 
            => provider.GetEnum(value);

        public string GetCategory(Category value) 
            => provider.GetEnum(value);

        public string GetErrorCode(ErrorCode value) 
            => provider.GetEnum(value);
    }

    extension<TPage>(ILocalizationProvider provider)
    {
        public IStringLocalizer PageLocalizer()
         => provider.CreateScopedLocalizer($"Page.{typeof(TPage).Name}");
    }
}