using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools.Providers;

internal sealed class ToolsProvider : IToolsProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<ToolDefinition> _tools;
    private readonly FrozenDictionary<Category, ToolDefinition[]> _toolsByCategory;
    private readonly FrozenDictionary<Keyword, ToolDefinition[]> _toolsByKeyword;
    private readonly FrozenDictionary<string, ToolDefinition> _toolsByName;
    private readonly FrozenDictionary<Type, ToolDefinition> _toolsByType;

    public ToolsProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _tools = ToolsCatalog.ToolDefinitions;

        _toolsByType = _tools.ToFrozenDictionary(it => it.ToolType, it => it);

        _toolsByName = _tools.ToFrozenDictionary(it => it.Name, it => it);

        _toolsByCategory = _tools
            .SelectMany(tool => tool.Categories.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToFrozenDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray()
            );

        _toolsByKeyword = _tools
            .SelectMany(tool => tool.Keywords.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToFrozenDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray()
            );
    }

    public IReadOnlyCollection<ToolDefinition> GetToolDefinitions()
        => _tools;

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