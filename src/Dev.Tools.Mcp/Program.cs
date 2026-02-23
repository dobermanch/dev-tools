using Dev.Tools;
using Dev.Tools.Localization;
using Dev.Tools.Mcp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: GenerateToolsMcpTool]

var useHttp = args.Contains("--http")
              || Environment.GetEnvironmentVariable("MCP_TRANSPORT") == "http";

if (useHttp)
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    builder.Services
        .AddDevTools()
        .AddDevToolsLocalization()
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly();

    var app = builder.Build();
    app.MapMcp("/mcp");
    await app.RunAsync();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddConsole(opts =>
    {
        opts.LogToStandardErrorThreshold = LogLevel.Trace;
    });
    builder.Services
        .AddDevTools()
        .AddDevToolsLocalization()
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    await builder.Build().RunAsync();
}
