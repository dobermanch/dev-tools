namespace Dev.Tools.Providers;

public readonly record struct ToolDefinition
(
    string Name,
    string[] Aliases,
    string[] Categories,
    string[] Keywords,
    string[] ErrorCodes,
    Type ToolType
);