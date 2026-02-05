namespace Dev.Tools.Console.Core;

public interface IToolResponseHandler
{
    int ProcessResponse(ToolResult result, ToolDefinition definition, SettingsBase settings);
    int ProcessError(Exception exception, ToolDefinition definition, SettingsBase settings);
}