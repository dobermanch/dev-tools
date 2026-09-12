using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "json-formatter",
    Title = "JSON Formatter",
    Description =
        """
        Reformats JSON with control over indentation, key order, key casing and empty values.

        The document is parsed and re-serialized, so the original whitespace is lost, and input that
        is not strict JSON - comments and trailing commas included - is rejected rather than repaired.
        Compact output overrides the indent size.
        """,
    Usage =
        "Formatting only - this never changes values, just how the document is written. Renaming keys through KeyFormat changes the meaning of the document for anything that reads it by name.",
    Aliases = ["jf"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Json, Keyword.Format],
    Categories = [Category.Text]
)]
public sealed class JsonFormatterTool : ToolBase<JsonFormatterTool.Args, JsonFormatterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Json))
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(args.Json);
        }
        catch (Exception)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        var jsonObject = FormatJson(document.RootElement, args);

        var json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
        {
            WriteIndented = !args.Compact,
            IndentSize = args.IndentSize,
            // this does not work on JsonDocument
            DefaultIgnoreCondition = args.ExcludeEmpty
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never
        });

        return new Result { Json = json };
    }

    private static object FormatJson(JsonElement element, Args args)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            IDictionary<string, object> sortedDictionary =
                args.SortKeys == SortDirection.None
                    ? new Dictionary<string, object>()
                    : new SortedDictionary<string, object>(
                        args.SortKeys == SortDirection.Ascending
                            ? Comparer<string>.Default
                            : Comparer<string>.Create((x, y) => string.Compare(y, x, StringComparison.Ordinal))
                    );

            foreach (var property in element.EnumerateObject())
            {
                if (!args.ExcludeEmpty || property.Value.ValueKind != JsonValueKind.Null)
                {
                    sortedDictionary[FormatKey(property.Name, args.KeyFormat)] = FormatJson(property.Value, args);
                }
            }

            return sortedDictionary;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var sortedList = new List<object>();
            foreach (var item in element.EnumerateArray())
            {
                if (!args.ExcludeEmpty || item.ValueKind != JsonValueKind.Null)
                {
                    sortedList.Add(FormatJson(item, args));
                }
            }

            return sortedList;
        }

        return element;
    }

    private static string FormatKey(string key, TextCase format) =>
        format switch
        {
            _ when key.Length == 0 => key,
            TextCase.LowerCase => key.ToLowerInvariant(),
            TextCase.UpperCase => key.ToUpperInvariant(),
            TextCase.CamelCase => char.ToLower(key[0]) + key[1..],
            TextCase.PascalCase => char.ToUpper(key[0]) + key[1..],
            _ => key
        };

    #region Nested Types

    public enum TextCase
    {
        [Description("Leave names unchanged")]
        None = 0,

        [Description("lowercase")]
        LowerCase,

        [Description("UPPERCASE")]
        UpperCase,

        [Description("camelCase")]
        CamelCase,

        [Description("PascalCase")]
        PascalCase,
    }

    public enum SortDirection
    {
        [Description("Keep the original order")]
        None = 0,

        [Description("Ascending (A-Z)")]
        Ascending,

        [Description("Descending (Z-A)")]
        Descending
    }

    public sealed record Args(
        [property: PipeInput,
                   DisplayName("JSON"),
                   Description("JSON text to format")]
        string Json,
        [property: Description("Number of spaces per indent level; ignored when Compact is set")]
        int IndentSize = 2,
        [property: Description("Order to sort object keys in")]
        SortDirection SortKeys = SortDirection.None,
        [property: Description("Case format applied to every object key")]
        TextCase KeyFormat = TextCase.None,
        [property: Description("Drop null, empty string, empty array and empty object values")]
        bool ExcludeEmpty = false,
        [property: Description("Write the document on a single line with no extra whitespace")]
        bool Compact = false
    );

    public sealed record Result : ToolResult
    {
        [PipeOutput]
        [DisplayName("Formatted JSON")]
        [Description("The reformatted JSON document")]
        public string Json { get; init; } = null!;
    }

    #endregion
}