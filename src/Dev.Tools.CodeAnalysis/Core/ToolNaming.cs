namespace Dev.Tools.CodeAnalysis.Core;

/// <summary>
/// Every name the generators derive from a tool's class name, in one place.
///
/// These used to be ten separate <c>TypeName.Replace("Tool", "")</c> calls spread over the three
/// frontend generators. <see cref="string.Replace(string,string)"/> is global, not a suffix trim, so
/// a class whose name contained "Tool" anywhere else was renamed in the middle
/// ("ToolkitTool" became "kit"). No tool in the library triggers that today, which is exactly why it
/// would have gone unnoticed until someone added one.
/// </summary>
public static class ToolNaming
{
    private const string ToolSuffix = "Tool";

    /// <summary>
    /// The class name with its "Tool" suffix removed: "HashTextTool" becomes "HashText".
    /// A name that is only the suffix, or does not end in it, is left alone.
    /// </summary>
    public static string BaseName(string typeName)
        => typeName.Length > ToolSuffix.Length && typeName.EndsWith(ToolSuffix, StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - ToolSuffix.Length)
            : typeName;

    /// <summary>API request DTO, e.g. "HashTextRequest".</summary>
    public static string RequestType(string typeName) => BaseName(typeName) + "Request";

    /// <summary>API and MCP response DTO, e.g. "HashTextResponse".</summary>
    public static string ResponseType(string typeName) => BaseName(typeName) + "Response";

    /// <summary>API controller, e.g. "HashTextEndpoint".</summary>
    public static string EndpointType(string typeName) => BaseName(typeName) + "Endpoint";

    /// <summary>Spectre command, e.g. "HashTextCommand".</summary>
    public static string CommandType(string typeName) => BaseName(typeName) + "Command";

    /// <summary>MCP tool holder, e.g. "HashTextMcpTool".</summary>
    public static string McpToolType(string typeName) => BaseName(typeName) + "McpTool";
}
