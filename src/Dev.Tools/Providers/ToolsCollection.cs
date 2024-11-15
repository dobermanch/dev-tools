using System.Collections;

namespace Dev.Tools.Providers;

public sealed class ToolsCollection : IEnumerable<ToolDefinition>
{
    private readonly List<ToolDefinition> _tools;
    private readonly Dictionary<string, ToolDefinition[]> _toolsByCategory;
    private readonly Dictionary<string, ToolDefinition[]> _toolsByKeyword;
    private readonly Dictionary<string, ToolDefinition> _toolsByName;

    public ToolsCollection(IEnumerable<ToolDefinition> tools)
    {
        _tools = [..tools];

        _toolsByName = _tools.ToDictionary(it => it.Name, it => it);

        _toolsByCategory = _tools
            .SelectMany(tool => tool.Categories.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray());

        _toolsByKeyword = _tools
            .SelectMany(tool => tool.Keywords.Select(key => (key, tool)))
            .GroupBy(it => it.key)
            .ToDictionary(
                it => it.Key,
                pair => pair.Select(it => it.tool).ToArray());
    }

    public ToolDefinition? this[string name]
        => _toolsByName.GetValueOrDefault(name);

    public IReadOnlyCollection<ToolDefinition> this[Category category]
        => _toolsByCategory.GetValueOrDefault(category, []);

    public IReadOnlyCollection<ToolDefinition> this[Keyword keyword]
        => _toolsByKeyword.GetValueOrDefault(keyword, []);

    public IEnumerator<ToolDefinition> GetEnumerator()
        => _tools.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}