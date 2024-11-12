using System.Reflection;

namespace Dev.Tools.Providers;

internal sealed class DefaultToolsProvider : IToolsProvider
{
    private ToolsCollection? _tools;

    public ToolsCollection GetTools()
    {
        if (_tools is not null)
        {
            return _tools;
        }

        var tools = GetType()
            .Assembly
            .GetTypes()
            .Where(it => it is { IsClass: true, IsAbstract: false, IsPublic: true } && typeof(ITool).IsAssignableFrom(it))
            .Select(type => (type, attr: type.GetCustomAttribute<ToolDefinitionAttribute>()))
            .Where(it => it.attr is not null)
            .Select(it => new ToolDefinition(
                Name: it.attr!.Name,
                Aliases: it.attr.Aliases,
                Categories: it.attr.Categories,
                Keywords: it.attr.Keywords,
                ErrorCodes: it.attr.ErrorCodes,
                ToolType: it.type
            ))
            .ToArray();

        return _tools = new ToolsCollection(tools);
    }
}