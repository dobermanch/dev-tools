namespace Dev.Tools.Mcp.Core;

public static class ToolErrorMessage
{
    /// <summary>
    /// "text-encryptor failed: text: Text cannot be empty; key: Input is not valid". English, with each
    /// argument named the way the tool's input schema names it, so a model knows what to change. The MCP
    /// SDK reports the message of an <c>McpException</c> as a failed tool call.
    /// </summary>
    public static string For(string toolName, IReadOnlyList<ToolError> errors)
        => $"{toolName} failed: "
           + string.Join("; ", errors.Select(it => it.Target is null
               ? it.Message
               : $"{char.ToLowerInvariant(it.Target[0])}{it.Target[1..]}: {it.Message}"));
}
