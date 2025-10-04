using Dev.Tools.Cryptography;
using Dev.Tools.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dev.Tools;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevTools(this IServiceCollection services)
    {
        services.TryAddSingleton<IMd5Hash, Md5Hash>();
        services.AddSingleton<IToolsProvider, ToolsProvider>();

        foreach (var tool in ToolsCatalog.ToolDefinitions)
        {
            services.AddTransient(tool.ToolType);
        }

        return services;
    }
}