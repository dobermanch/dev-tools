using Dev.Tools.Console.Commands;
using Dev.Tools.Console.Core;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dev.Tools.Console;

internal static class ServiceCollectionExtensions
{
    // The shell convention for a command line that could not be understood; a tool that fails exits 1.
    private const int UsageErrorExitCode = 2;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommands(CommandApp app)
        {
            services.AddTransient<IToolResponseHandler, ToolResponseHandler>();
            services.AddSingleton(new PipeInputReader());
            app.Configure(config =>
            {
                // Spectre's default is to set aside an option or argument it does not recognise, so a
                // mistyped option did nothing and the command still reported success.
                config.UseStrictParsing();
                // Spectre prints a command line it cannot parse to stdout, where the next command in a
                // pipe would read it as input. The same message, pointer to the bad argument and all,
                // goes to stderr instead.
                config.SetExceptionHandler((exception, _) =>
                {
                    if (exception is CommandAppException { Pretty: { } pretty })
                    {
                        StandardError.Console.Write(pretty);
                    }
                    else
                    {
                        StandardError.Console.MarkupLineInterpolated($"[red]Error:[/] {exception.Message}");
                    }

                    return exception is CommandParseException ? UsageErrorExitCode : -1;
                });
                config.SetHelpProvider(new ToolHelpProvider(config.Settings));
                CommandConfigurator.ConfigureCommands(services, config);
            });
            
            return services;
        }
    }
}