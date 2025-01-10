namespace Dev.Tools.Providers;

public interface IToolsProvider
{
    T GetTool<T>() where T : ITool;
    
    IReadOnlyCollection<ToolDefinition> GetToolDefinitions();

    ToolDefinition GetToolDefinition(string name);

    ToolDefinition GetToolDefinition<T>() where T : ITool;

    IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Category category);

    IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Keyword keyword);
}