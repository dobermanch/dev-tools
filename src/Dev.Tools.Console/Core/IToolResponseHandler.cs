using Dev.Tools.Core;

namespace Dev.Tools.Console.Core;

public interface IToolResponseHandler
{
    int ProcessResponse(ToolResult result, ToolDefinition definition);
    int ProcessError(Exception exception, ToolDefinition definition);
}