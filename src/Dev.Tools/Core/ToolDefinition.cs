namespace Dev.Tools;

public sealed record ToolDefinition(
    string Name,
    string[] Aliases,
    Category[] Categories,
    Keyword[] Keywords,
    string[] ErrorCodes,
    Type ToolType,
    ToolDefinition.TypeDetails ArgsType,
    ToolDefinition.TypeDetails ReturnType
)
{
    public sealed record TypeDetails(
        Type DataType,
        TypeProperty[] Properties
    );

    public sealed record TypeProperty(
        string Name,
        Type PropertyType,
        bool IsRequired,
        bool IsNullable
    );
}