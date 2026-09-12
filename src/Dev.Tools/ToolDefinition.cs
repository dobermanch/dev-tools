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
    ToolDefinition.TypeDetailsBase[] ExtraTypes,
    string Title = "",
    string Description = "",
    string Usage = ""
)
{
    /// <summary>
    /// First line of <see cref="Description"/>, for places that show one line per tool
    /// (CLI command list, MCP tool list). Multi-line descriptions keep their detail in
    /// <see cref="Description"/>.
    /// </summary>
    public string Summary
    {
        get
        {
            var end = Description.IndexOf('\n');
            return end < 0 ? Description : Description[..end].TrimEnd();
        }
    }

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
        bool IsPipeOutput = false,
        string Description = ""
    );

    public sealed record EnumDetails(
        string Name,
        Type DataType,
        EnumValue[] Values
    ): TypeDetailsBase(Name, DataType);

    public sealed record EnumValue(
        string Name,
        string Description = ""
    );
}
