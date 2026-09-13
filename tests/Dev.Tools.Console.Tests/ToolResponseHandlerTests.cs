using System.Text;
using Dev.Tools.Console.Commands;
using Dev.Tools.Console.Core;
using Dev.Tools.Providers;
using Dev.Tools.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Dev.Tools.Console.Tests;

public class ToolResponseHandlerTests
{
    [Test]
    public async Task ShouldWriteThePipedResultUnchanged_HoweverLongItIs()
    {
        // Results used to go through Spectre, which wraps at the console width - 80 columns when
        // redirected - and so put line breaks into anything piped on to the next command.
        var tools = new ServiceCollection().AddDevTools().BuildServiceProvider().GetRequiredService<IToolsProvider>();
        var text = new string('a', 3000);
        var result = await tools.GetTool<Base64EncoderTool>().RunAsync(new Base64EncoderTool.Args(text), CancellationToken.None);
        var output = new StringWriter();

        var exitCode = new ToolResponseHandler(output)
            .ProcessResponse(result, tools.GetToolDefinition<Base64EncoderTool>(), new Base64EncoderCommand.Settings());

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString())
            .IsEqualTo(Convert.ToBase64String(Encoding.UTF8.GetBytes(text)) + Environment.NewLine);
    }

    [Test]
    [Arguments("EncodeNonAscii", "html-entities: --encode-non-ascii: Input is not valid")]
    [Arguments("Text", "html-entities: text: Input is not valid")]
    [Arguments(null, "html-entities: Input is not valid")]
    public async Task ShouldNameTheArgumentAtFaultTheWayItIsTyped(string? target, string expected)
    {
        var tools = new ServiceCollection().AddDevTools().BuildServiceProvider().GetRequiredService<IToolsProvider>();

        var description = ToolResponseHandler.Describe(
            tools.GetToolDefinition<HtmlEntitiesTool>(),
            new ToolError(ErrorCode.InputNotValid, target));

        await Assert.That(description).IsEqualTo(expected);
    }
}
