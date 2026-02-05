using Newtonsoft.Json;
using Spectre.Console;

namespace Dev.Tools.Console.Core;

internal sealed class ToolResponseHandler : IToolResponseHandler
{
    public int ProcessResponse(ToolResult result, ToolDefinition definition, SettingsBase settings)
    {
        AnsiConsole.WriteLine(JsonConvert.SerializeObject(result));
        return 0;
    }

    public int ProcessError(Exception exception, ToolDefinition definition, SettingsBase settings)
    {
        AnsiConsole.WriteException(exception);
        return -1;
    }
}