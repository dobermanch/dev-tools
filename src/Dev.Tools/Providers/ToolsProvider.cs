using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools.Providers;

internal sealed partial class ToolsProvider : IToolsProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ToolDefinition[]> _toolsByCategory;
    private readonly Dictionary<string, ToolDefinition[]> _toolsByKeyword;
    private readonly Dictionary<string, ToolDefinition> _toolsByName;
    private readonly Dictionary<Type, ToolDefinition> _toolsByType;

    public ToolsProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var tools = GetToolDefinitions();
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

        _toolsByType = tools.ToDictionary(it => it.ToolType, it => it);
    }

    public ToolDefinition GetToolDefinition<T>()
        where T : ITool
        => _toolsByType.TryGetValue(typeof(T), out var definition)
            ? definition
            : throw new ToolDefinitionNotFoundException(typeof(T).Name);

    public T GetTool<T>() where T : ITool 
        => ActivatorUtilities.CreateInstance<T>(_serviceProvider);

    public ToolDefinition GetToolDefinition(string name)
        => _toolsByName.TryGetValue(name, out var definition) 
            ? definition 
            : throw new ToolDefinitionNotFoundException(name);

    public IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Category category)
        => _toolsByCategory.GetValueOrDefault(category, []);

    public IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Keyword keyword)
        => _toolsByKeyword.GetValueOrDefault(keyword, []);
}