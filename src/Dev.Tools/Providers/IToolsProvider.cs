namespace Dev.Tools.Providers;

/// <summary>
/// Finds tools and their definitions. The generic <c>GetTool&lt;T&gt;</c> and
/// <c>GetToolDefinition&lt;T&gt;</c> are extensions over the <see cref="Type"/> lookups
/// (<see cref="ToolsProviderExtensions"/>), so an implementation has only the real lookups to provide.
/// </summary>
public interface IToolsProvider
{
    /// <summary>
    /// The tool of the given type, for a host that holds a <see cref="Type"/> rather than a
    /// compile-time type argument - one that resolved a <see cref="ToolDefinition"/> at run time.
    /// </summary>
    ITool GetTool(Type toolType);

    /// <summary>The tool a name or an alias refers to.</summary>
    ITool GetTool(string nameOrAlias);

    IReadOnlyCollection<ToolDefinition> GetToolDefinitions();

    /// <summary>The definition a name or an alias refers to.</summary>
    ToolDefinition GetToolDefinition(string nameOrAlias);

    ToolDefinition GetToolDefinition(Type toolType);

    IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Category category);

    IReadOnlyCollection<ToolDefinition> GetToolDefinitions(Keyword keyword);
}
