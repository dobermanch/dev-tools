using System.Collections.Frozen;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools.Providers;

internal sealed class ToolsProvider : IToolsProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<ToolDefinition> _tools;
    private readonly FrozenDictionary<Category, ToolDefinition[]> _toolsByCategory;
    private readonly FrozenDictionary<Keyword, ToolDefinition[]> _toolsByKeyword;
    private readonly FrozenDictionary<string, ToolDefinition> _toolsByNameOrAlias;
    private readonly FrozenDictionary<Type, ToolDefinition> _toolsByType;

    public ToolsProvider(IServiceProvider serviceProvider)
        : this(serviceProvider, ToolsCatalog.ToolDefinitions)
    {
    }

    internal ToolsProvider(IServiceProvider serviceProvider, IReadOnlyCollection<ToolDefinition> tools)
    {
        _serviceProvider = serviceProvider;
        _tools = tools;

        _toolsByType = _tools.ToFrozenDictionary(it => it.ToolType, it => it);

        _toolsByNameOrAlias = IndexByNameOrAlias(_tools);

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

    public ToolDefinition GetToolDefinition(Type toolType)
        => _toolsByType.TryGetValue(toolType, out var definition)
            ? definition
            : throw new ToolDefinitionNotFoundException(toolType.Name);

    public ToolDefinition GetToolDefinition(string nameOrAlias)
        => _toolsByNameOrAlias.TryGetValue(nameOrAlias, out var definition)
            ? definition
            : throw new ToolDefinitionNotFoundException(nameOrAlias);

    public ITool GetTool(string nameOrAlias)
        => Resolve(GetToolDefinition(nameOrAlias));

    public ITool GetTool(Type toolType)
        => Resolve(GetToolDefinition(toolType));

    public IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Category category)
        => _toolsByCategory.GetValueOrDefault(category, []);

    public IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Keyword keyword)
        => _toolsByKeyword.GetValueOrDefault(keyword, []);

    /// <summary>
    /// Through the container, so a tool gets the registrations <c>AddDevTools</c> made for it and
    /// whatever a host substituted for its dependencies. It used to be built with
    /// <c>ActivatorUtilities</c>, which left those registrations unused.
    /// </summary>
    private ITool Resolve(ToolDefinition definition)
        => (ITool)_serviceProvider.GetRequiredService(definition.ToolType);

    /// <summary>
    /// Names and aliases share one namespace - the CLI accepts either as a command - so they are
    /// indexed together, and a collision is reported by name rather than surfacing as the
    /// dictionary's "an item with the same key has already been added". DT1001 stops it at build time
    /// for tools compiled with the analyzer; this is the backstop for everything else.
    /// </summary>
    private static FrozenDictionary<string, ToolDefinition> IndexByNameOrAlias(IEnumerable<ToolDefinition> tools)
    {
        var entries = tools
            .SelectMany(tool => new[] { tool.Name }.Concat(tool.Aliases).Select(key => (key, tool)))
            .ToArray();

        var collisions = entries
            .GroupBy(it => it.key, StringComparer.Ordinal)
            .Where(it => it.Count() > 1)
            .Select(it => $"'{it.Key}' ({string.Join(", ", it.Select(entry => entry.tool.Name))})")
            .ToArray();

        if (collisions.Length > 0)
        {
            throw new InvalidOperationException(
                "Tool names and aliases must be unique, but these are used more than once: "
                + string.Join("; ", collisions));
        }

        return entries.ToFrozenDictionary(it => it.key, it => it.tool, StringComparer.Ordinal);
    }
}
