namespace Dev.Tools.Providers;

public sealed record ToolDefinition
(
    string Name,
    string[] Aliases,
    string[] Categories,
    string[] Keywords,
    string[] ErrorCodes,
    Type ToolType
);