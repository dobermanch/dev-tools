namespace Dev.Tools.Providers;

public interface IToolsProvider
{
    IReadOnlyCollection<ToolDefinition> GetTools();

    ToolDefinition? GetTool(string name);

    IReadOnlyCollection<ToolDefinition> GetTools(Category category);

    IReadOnlyCollection<ToolDefinition> GetTools(Keyword keyword);
}