using Dev.Tools.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTools(this IServiceCollection services)
    {
        var toolsProvider = new DefaultToolsProvider();
        services.AddSingleton<IToolsProvider>(toolsProvider);

        foreach (var tool in toolsProvider.GetTools())
        {
            var interfaces = tool
                .ToolType
                .GetInterfaces()
                .Where(it => typeof(ITool).IsAssignableFrom(it))
                .ToArray();

            services.AddTransient(tool.ToolType);
            foreach (var interfaceType in interfaces)
            {
                services.AddKeyedTransient(interfaceType, tool.Name, tool.ToolType);
            }
        }

        return services;
    }
}