using System.Reflection;
using Dev.Tools.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDevToolsLocalization(this IServiceCollection services, Assembly? assembly = null)
    {
        services.TryAddSingleton<ILocalizationProvider, LocalizationProvider>();
        services.RegisterResourcesForAssembly(assembly);
        services.RegisterResourcesForAssembly(Assembly.GetExecutingAssembly());
        services.AddLocalization();
        return services;
    }

    private static void RegisterResourcesForAssembly(this IServiceCollection services, Assembly? assembly)
    {
        if (assembly is null)
        {
            return;
        }
        
        const string resourceSuffix = ".resources";
        string[] resourceNames = assembly.GetManifestResourceNames();
        foreach (var resource in resourceNames)
        {
            if (!resource.EndsWith(resourceSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var resourceType = assembly.GetType(resource.Replace(resourceSuffix, string.Empty, StringComparison.Ordinal));
            if (resourceType is null)
            {
                continue;
            }

            resourceType = typeof(IStringLocalizer<>).MakeGenericType(resourceType);
            services.AddTransient<IStringLocalizer>(provider => (IStringLocalizer)provider.GetRequiredService(resourceType));
        }
    }
}