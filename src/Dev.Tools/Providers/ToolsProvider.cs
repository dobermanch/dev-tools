namespace Dev.Tools.Providers;

internal sealed partial class ToolsProvider : IToolsProvider
{
    private ToolsCollection? _tools;

    public ToolsCollection GetTools()
    {
        return _tools ??= new ToolsCollection(GetToolDefinitions());
    }
}