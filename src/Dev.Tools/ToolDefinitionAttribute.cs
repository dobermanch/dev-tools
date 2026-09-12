namespace Dev.Tools;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ToolDefinitionAttribute : Attribute
{
    public required string Name { get; set; }

    /// <summary>
    /// English display name, e.g. "UUID Generator". Falls back to <see cref="Name"/> when not set.
    /// Translations live in the Web frontend; every other frontend shows this text.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// English description of what the tool does. May span multiple lines for detailed text:
    /// the first line is used where only one line fits (CLI command list), the full text where
    /// there is room (API schema, Web page). Use a raw string literal for multi-line text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional guidance for a model calling this tool through the MCP server: when to prefer it,
    /// when not to use it, what the result looks like. Appended to the MCP tool description only,
    /// so it never reaches the Web UI, and only tools that need steering pay for the tokens.
    /// </summary>
    public string? Usage { get; set; }

    public string[] Aliases { get; set; } = [];
    public Category[] Categories { get; set; } = [];
    public Keyword[] Keywords { get; set; } = [];
}
