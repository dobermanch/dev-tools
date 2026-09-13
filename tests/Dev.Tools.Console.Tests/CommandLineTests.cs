using Dev.Tools.Console.Core;
using Dev.Tools.Console.Core.DI;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Dev.Tools.Console.Tests;

/// <summary>
/// The command line as a user meets it: the generated commands, configured the way Program.cs
/// configures them, with only the result captured instead of written to stdout.
/// </summary>
public class CommandLineTests
{
    private static async Task<(int ExitCode, string Output)> RunAsync(params string[] args)
    {
        var services = new ServiceCollection();
        var app = new CommandApp(new TypeRegistrar(services));
        var output = new StringWriter();

        services
            .AddLogging()
            .AddDevTools()
            .AddCommands(app)
            .AddTransient<IToolResponseHandler>(_ => new ToolResponseHandler(output));

        var exitCode = await app.RunAsync(args, CancellationToken.None);

        return (exitCode, output.ToString());
    }

    [Test]
    [Arguments("--encode-non-ascii")]
    [Arguments("-e")]
    public async Task ShouldTakeAMultiWordOptionInKebabCase(string option)
    {
        var (exitCode, output) = await RunAsync("html-entities", "café", option);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).IsEqualTo("caf&#233;" + Environment.NewLine);
    }

    [Test]
    public async Task ShouldTakeAnOptionValue()
    {
        var (exitCode, output) = await RunAsync("html-entities", "&lt;b&gt;", "--transcoding", "Decode");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output).IsEqualTo("<b>" + Environment.NewLine);
    }

    // Exit 2 is ours: Spectre's own handling exits -1 and prints the error to stdout, so 2 also says the
    // message went through the handler that writes it to stderr.
    private const int UsageErrorExitCode = 2;

    [Test]
    [Arguments("--no-such-option")]
    [Arguments("--encodeNonAscii")]
    public async Task ShouldFailWithoutRunningTheTool_WhenAnOptionIsUnknown(string option)
    {
        var (exitCode, output) = await RunAsync("html-entities", "café", option);

        await Assert.That(exitCode).IsEqualTo(UsageErrorExitCode);
        await Assert.That(output).IsEmpty();
    }

    [Test]
    public async Task ShouldFailWithoutRunningTheTool_WhenAnArgumentHasNowhereToGo()
    {
        var (exitCode, output) = await RunAsync("base64-encoder", "hello", "world");

        await Assert.That(exitCode).IsEqualTo(UsageErrorExitCode);
        await Assert.That(output).IsEmpty();
    }
}
