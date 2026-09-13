using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dev.Tools.DataFormats;

/// <summary>
/// CSV written from an array of objects: a header of every key in the order first seen, then one row
/// per object. A missing or null value is an empty cell, a nested object or array is its JSON, a field
/// holding a comma, a quote or a line break is quoted as RFC 4180 has it, and lines end with \n.
/// </summary>
internal static class CsvFormat
{
    private static readonly SearchValues<char> NeedsQuoting = SearchValues.Create(",\"\r\n");

    public static string Write(JsonNode? data)
    {
        if (data is not JsonArray rows || rows.Any(it => it is not JsonObject))
        {
            throw new UnwritableDataException("CSV from something other than an array of objects");
        }

        var objects = rows.Cast<JsonObject>().ToArray();
        var headers = objects
            .SelectMany(it => it.Select(property => property.Key))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (headers.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        AppendRow(builder, headers);
        foreach (var row in objects)
        {
            builder.Append('\n');
            AppendRow(builder, headers.Select(header => row.TryGetPropertyValue(header, out var value) ? Cell(value) : string.Empty));
        }

        return builder.ToString();
    }

    private static string Cell(JsonNode? value) => value switch
    {
        null => string.Empty,
        JsonValue text when text.GetValueKind() == JsonValueKind.String => text.GetValue<string>(),
        _ => value.ToJsonString(JsonFormat.CompactOptions)
    };

    private static void AppendRow(StringBuilder builder, IEnumerable<string> fields)
    {
        var first = true;
        foreach (var field in fields)
        {
            if (!first)
            {
                builder.Append(',');
            }

            first = false;
            if (field.AsSpan().IndexOfAny(NeedsQuoting) >= 0)
            {
                builder.Append('"').Append(field.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            }
            else
            {
                builder.Append(field);
            }
        }
    }
}
