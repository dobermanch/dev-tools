using Dev.Tools;
using Dev.Tools.Console;
using Dev.Tools.Console.Core.DI;
using Dev.Tools.Console.Core;
using Dev.Tools.Localization;
using Spectre.Console.Cli;
using Microsoft.Extensions.DependencyInjection;

[assembly: GenerateToolsCliCommand]

var services = new ServiceCollection();
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

services
    .AddLogging()
    .AddDevTools()
    .AddDevToolsLocalization()
    .AddCommands(app);

return app.Run(args);