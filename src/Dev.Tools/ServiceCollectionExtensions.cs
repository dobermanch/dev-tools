using Dev.Tools.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        var toolsProvider = new ToolsProvider();
        services.AddSingleton<IToolsProvider>(toolsProvider);

        foreach (var tool in toolsProvider.GetTools())
        {
            services.AddTransient(tool.ToolType);
        }

        return services;
    }
}