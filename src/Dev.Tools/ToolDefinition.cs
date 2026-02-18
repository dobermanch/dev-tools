namespace Dev.Tools;

public sealed record ToolDefinition(
    string Name,
    string[] Aliases,
    Category[] Categories,
    Keyword[] Keywords,
    ErrorCode[] ErrorCodes,
    Type ToolType,
    ToolDefinition.TypeDetails ArgsType,
    ToolDefinition.TypeDetails ReturnType,
    ToolDefinition.TypeDetailsBase[] ExtraTypes
)
{
    public record TypeDetailsBase(
        string Name,
        Type DataType
    );
    
    public sealed record TypeDetails (
        string Name,
        Type DataType,
        TypeProperty[] Properties
    ) : TypeDetailsBase(Name, DataType);

    public sealed record TypeProperty(
        string Name,
        Type PropertyType,
        bool IsRequired,
        bool IsNullable,
        bool IsPipeInput = false,
        bool IsPipeOutput = false
    );

    public sealed record EnumDetails(
        string Name,
        Type DataType,
        string[] Values
    ): TypeDetailsBase(Name, DataType);
}