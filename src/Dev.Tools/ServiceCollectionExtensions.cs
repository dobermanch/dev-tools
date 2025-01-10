using Dev.Tools.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        services.AddSingleton<IToolsProvider, ToolsProvider>();

        return services;
    }
}