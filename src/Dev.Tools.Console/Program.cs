using Dev.Tools.Console.Core.DI;
using Dev.Tools.Console.Commands;
using Dev.Tools.Console.Core;
using Spectre.Console.Cli;
using Microsoft.Extensions.DependencyInjection;
using Dev.Tools.Tools;

[assembly: GenerateToolsCliCommand]

var services = new ServiceCollection();
services
    .AddTransient<Base64DecoderTool>()
    .AddTransient<Base64EncoderTool>()
    .AddTransient<UuidGeneratorTool>()
    ;

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config
        .AddCommand<Base64DecoderCommand>("base64-decoder")
        .WithAlias("d64");

    config
        .AddCommand<Base64EncoderCommand>("base64-encoder")
        .WithAlias("e64");

    config
        .AddCommand<UuidGeneratorCommand>("uuid-gen")
        .WithAlias("ug");
});

return app.Run(args);
