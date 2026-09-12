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
    /// English description of what the tool does. May run to several paragraphs for detailed text:
    /// the first paragraph is used where only one line fits (the CLI command list), the full text
    /// where there is room (API schema, MCP tool list, Web page). Use a raw string literal for
    /// detailed text, and wrap it for readability - a single newline inside a paragraph is dropped
    /// when the text is read, since every frontend wraps to its own width. A blank line starts a
    /// new paragraph.
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
