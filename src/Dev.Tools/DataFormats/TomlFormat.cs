using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Parsing;

namespace Dev.Tools.DataFormats;

/// <summary>
/// TOML through Tomlyn's untyped model. A TOML document is a table, so only an object can be written,
/// and TOML has no null. Dates and times are read as the text TOML writes them in, and a string is never
/// written back as a date; inf and nan, which JSON cannot hold, are read as the strings "inf", "-inf"
/// and "nan". An array of objects is written as [[table]] sections.
/// </summary>
internal static class TomlFormat
{
    private static readonly TomlSerializerOptions Options = new()
    {
        NewLine = TomlNewLineKind.Lf
    };

    public static JsonNode? Read(string text)
    {
        TomlTable table;
        try
        {
            // TOML forbids defining a key or a table twice, but the serializer lets the last one win when
            // reading into an untyped table, DuplicateKeyHandling or not. Validating the document against
            // the TOML rules first reports it.
            SyntaxParser.ParseStrict(text, Options, null, validate: true);
            table = TomlSerializer.Deserialize<TomlTable>(text, Options)
                    ?? throw new UnreadableDataException("TOML that is not a table");
        }
        catch (TomlException exception)
        {
            throw new UnreadableDataException("Not valid TOML", exception);
        }

        return FromToml(table);
    }

    private static JsonNode? FromToml(object? value) => value switch
    {
        TomlTable table => new JsonObject(table.Select(it => KeyValuePair.Create(it.Key, FromToml(it.Value)))),
        TomlTableArray tables => new JsonArray(tables.Select(FromToml).ToArray()),
        TomlArray items => new JsonArray(items.Select(FromToml).ToArray()),
        string text => JsonValue.Create(text),
        long number => JsonValue.Create(number),
        double number when double.IsFinite(number) => JsonValue.Create(number),
        double number => JsonValue.Create(double.IsNaN(number) ? "nan" : number > 0 ? "inf" : "-inf"),
        bool flag => JsonValue.Create(flag),
        TomlDateTime date => JsonValue.Create(date.ToString()),
        _ => throw new UnreadableDataException("A TOML value of a kind this reader does not know")
    };

    public static string Write(JsonNode? data)
    {
        if (data is not JsonObject document)
        {
            throw new UnwritableDataException("TOML from something other than an object");
        }

        try
        {
            // Tomlyn ends a [[table]] section with a blank line; the result ends with the data.
            return TomlSerializer.Serialize(ToTable(document), Options).TrimEnd('\n');
        }
        catch (TomlException exception)
        {
            throw new UnwritableDataException("Data TOML cannot write", exception);
        }
    }

    private static TomlTable ToTable(JsonObject data)
    {
        var table = new TomlTable();
        foreach (var (key, value) in data)
        {
            table[key] = ToToml(value);
        }

        return table;
    }

    private static object ToToml(JsonNode? node)
    {
        switch (node)
        {
            case null:
                throw new UnwritableDataException("A null, which TOML has no way to write");
            case JsonObject data:
                return ToTable(data);
            case JsonArray { Count: > 0 } items when items.All(it => it is JsonObject):
                var tables = new TomlTableArray();
                foreach (var item in items)
                {
                    tables.Add(ToTable((JsonObject)item!));
                }

                return tables;
            case JsonArray items:
                var array = new TomlArray();
                foreach (var item in items)
                {
                    array.Add(ToToml(item));
                }

                return array;
            default:
                var value = (JsonValue)node;
                return value.GetValueKind() switch
                {
                    JsonValueKind.String => value.GetValue<string>(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => Number(value.ToJsonString())
                };
        }
    }

    // TOML integers are 64-bit. A wider one is reported rather than rounded into a float.
    private static object Number(string text)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return integer;
        }

        if (text.AsSpan().IndexOfAny(".eE") < 0)
        {
            throw new UnwritableDataException("An integer wider than TOML's 64 bits");
        }

        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
