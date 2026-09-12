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
    /// First paragraph of <see cref="Description"/>, for places that show one line per tool
    /// (CLI command list, Web tool card). Detailed descriptions keep their later paragraphs in
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
        string Description = "",
        string DisplayName = ""
    )
    {
        /// <summary>
        /// Human label for the property: the authored <see cref="DisplayName"/> when there is one,
        /// otherwise the property name split into words. Most labels read fine when derived
        /// ("InsertLineBreaks"), but some do not ("Data" is really "Decoded Text"), which is what
        /// <see cref="DisplayName"/> is for.
        /// </summary>
        public string Label => string.IsNullOrEmpty(DisplayName) ? SplitWords(Name) : DisplayName;

        private static string SplitWords(string name)
        {
            var builder = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(name[i]);
            }

            return builder.ToString();
        }
    }

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
