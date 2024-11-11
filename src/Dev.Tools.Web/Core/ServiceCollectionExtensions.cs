using Dev.Tools.Web.Core.Serializer;
using Dev.Tools.Web.Core.Storage;
using Dev.Tools.Web.Core.WeakReferences;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dev.Tools.Web.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreComponents(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSerializer()
            .AddWeakReferences(configuration)
            .AddMessenger()
            .AddStorage();
        
        return services;
    }

    private static IServiceCollection AddSerializer(this IServiceCollection services)
    {
        services
            .AddSingleton<DefaultJsonSerializer>()
            .TryAddSingleton<IJsonSerializer>(p => p.GetRequiredService<DefaultJsonSerializer>());
        return services;
    }

    private static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services
            .AddSingleton<BrowserStorageProvider>()
            .TryAddSingleton<IStorageProvider>(p => p.GetRequiredService<BrowserStorageProvider>());
        return services;
    }

    private static IServiceCollection AddMessenger(this IServiceCollection services)
        => services
            .AddMediatR(config =>
            {
                config.MediatorImplementationType = typeof(Messenger);
                config.RegisterServicesFromAssemblyContaining<Program>();
            })
            .AddSingleton(provider => (IMessenger)provider.GetRequiredService<IMediator>());

    private static IServiceCollection AddWeakReferences(this IServiceCollection services, IConfiguration configuration) 
        => services
            .AddOptions()
            .Configure<WeakReferencesOptions>(options =>
            {
                var section = configuration.GetSection(WeakReferencesOptions.SectionName);
                if (section.Exists())
                {
                    section.Bind(options);
                }
            })
            .AddSingleton<WeakReferenceManager>()
            .AddHostedService(provider => provider.GetRequiredService<WeakReferenceManager>());
}