namespace Dev.Tools.Providers;

internal sealed partial class ToolsProvider : IToolsProvider
{
    private readonly Dictionary<string, ToolDefinition[]> _toolsByCategory;
    private readonly Dictionary<string, ToolDefinition[]> _toolsByKeyword;
    private readonly Dictionary<string, ToolDefinition> _toolsByName;
    
    public ToolsProvider()
    {
        var tools = GetTools();
        _toolsByName = tools.ToDictionary(it => it.Name, it => it);

        _toolsByCategory = tools
            .SelectMany(tool => tool.Categories.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray());

        _toolsByKeyword = tools
            .SelectMany(tool => tool.Keywords.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray());
    }

    public ToolDefinition? GetTool(string name)
        => _toolsByName.GetValueOrDefault(name);

    public IReadOnlyCollection<ToolDefinition> GetTools(Category category)
        => _toolsByCategory.GetValueOrDefault(category, []);

    public IReadOnlyCollection<ToolDefinition> GetTools(Keyword keyword)
        => _toolsByKeyword.GetValueOrDefault(keyword, []);
}