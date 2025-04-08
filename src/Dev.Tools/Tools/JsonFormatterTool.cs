using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "json-formatter",
    Aliases = ["jf"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Json, Keyword.Format],
    Categories = [Category.Text],
    ErrorCodes = [ErrorCode.InputNotValid]
)]
public sealed class JsonFormatterTool : ToolBase<JsonFormatterTool.Args, JsonFormatterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Json))
        {
            return Failed(ErrorCode.InputNotValid);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(args.Json);
        }
        catch (Exception)
        {
            return Failed(ErrorCode.InputNotValid);
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
            TextCase.LowerCase => key.ToLowerInvariant(),
            TextCase.UpperCase => key.ToUpperInvariant(),
            TextCase.CamelCase => char.ToLower(key[0]) + key[1..],
            TextCase.PascalCase => char.ToUpper(key[0]) + key[1..],
            _ => key
        };

    #region Nested Types

    public enum TextCase
    {
        None = 0,
        LowerCase,
        UpperCase,
        CamelCase,
        PascalCase,
    }

    public enum SortDirection
    {
        None = 0,
        Ascending,
        Descending
    }

    public record Args : ToolArgs
    {
        public required string Json { get; init; } = null!;
        public int IndentSize { get; init; } = 2;
        public SortDirection SortKeys { get; init; } = SortDirection.None;
        public TextCase KeyFormat { get; init; } = TextCase.None;
        public bool ExcludeEmpty { get; init; }
        public bool Compact { get; init; }
    }

    public record Result : ToolResult
    {
        public string Json { get; init; } = null!;
    }

    #endregion
}