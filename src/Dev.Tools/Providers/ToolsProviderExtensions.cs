using Dev.Tools.Providers;

// In the root namespace, like ServiceCollectionExtensions: the Web pages, the generated endpoints,
// commands and MCP tools all live under Dev.Tools.* and call these without importing Providers.
// ReSharper disable once CheckNamespace
namespace Dev.Tools;

public static class ToolsProviderExtensions
{
    extension(IToolsProvider provider)
    {
        public T GetTool<T>() where T : ITool
            => (T)provider.GetTool(typeof(T));

        public ToolDefinition GetToolDefinition<T>() where T : ITool
            => provider.GetToolDefinition(typeof(T));
    }
}
