using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Localization;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDevToolsLocalization(Assembly? assembly = null)
        {
            services.TryAddSingleton<ILocalizationProvider, LocalizationProvider>();
            services.RegisterResourcesForAssembly(assembly);
            services.RegisterResourcesForAssembly(Assembly.GetExecutingAssembly());
            services.AddLocalization();

            LocalizationProvider.SetProviderResolver(() =>
                services.BuildServiceProvider().GetRequiredService<ILocalizationProvider>());

            return services;
        }

        private void RegisterResourcesForAssembly(Assembly? assembly)
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

                var resourceType =
                    assembly.GetType(resource.Replace(resourceSuffix, string.Empty, StringComparison.Ordinal));
                if (resourceType is null)
                {
                    continue;
                }

                resourceType = typeof(IStringLocalizer<>).MakeGenericType(resourceType);
                services.AddTransient<IStringLocalizer>(provider =>
                    (IStringLocalizer)provider.GetRequiredService(resourceType));
            }
        }
    }
}