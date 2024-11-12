namespace Dev.Tools.Providers;

public sealed record ToolDefinition
(
    string Name,
    string[] Aliases,
    Categories[] Categories,
    Keywords[] Keywords,
    ErrorCodes[] ErrorCodes,
    Type ToolType
);