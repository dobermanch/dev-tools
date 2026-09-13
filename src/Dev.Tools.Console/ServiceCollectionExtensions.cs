using Dev.Tools.Console.Commands;
using Dev.Tools.Console.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Dev.Tools.Console;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommands(CommandApp app)
        {
            services.AddTransient<IToolResponseHandler, ToolResponseHandler>();
            services.AddSingleton(new PipeInputReader());
            app.Configure(config =>
            {
                config.SetHelpProvider(new ToolHelpProvider(config.Settings));
                CommandConfigurator.ConfigureCommands(services, config);
            });
            
            return services;
        }
    }
}