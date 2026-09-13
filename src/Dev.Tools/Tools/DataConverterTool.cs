using System.Text.Json.Nodes;
using Dev.Tools.DataFormats;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "data-converter",
    Title = "Data Format Converter",
    Description =
        """
        Converts structured data between JSON, YAML, TOML and XML, and writes any of them as CSV.
        Converting a format to itself reformats it.

        The data is read into one model - objects, arrays, strings, numbers, booleans and null - and
        written out again, so comments and the original layout are not kept. Data the target format
        cannot hold, such as a null in TOML, is reported as an error rather than changed to fit.

        YAML is read by the YAML 1.2 core schema: an unquoted true, 42 or null is that type, a quoted
        one is a string, and yes, no, on and off are strings. A stream must hold a single document, and
        aliases are expanded into copies of what they name.

        XML maps the common way: the root element is the one top-level key, attributes are keys starting
        with @, text beside attributes or child elements is #text, and an element that repeats is an
        array. Every XML value is a string and an empty element is null. Written back, data with a single
        top-level key uses it as the root element; anything else is wrapped in <root>, and array items
        with no name of their own are <item>.

        CSV needs an array of objects: the header is every key in the order first seen, a missing or null
        value is an empty cell, a nested object or array is written as JSON, and fields are quoted as
        RFC 4180 has it.
        """,
    Usage =
        "Use this rather than rewriting data by hand between formats: the quoting that decides a YAML value's type, TOML's tables and XML's attributes and repeated elements are easy to get wrong. It converts the data, not a schema - key names and values are kept as they are.",
    Aliases = [],
    Keywords = [Keyword.Convert, Keyword.Format, Keyword.Json, Keyword.Yaml, Keyword.Toml, Keyword.Xml, Keyword.Csv],
    Categories = [Category.Converter]
)]
public sealed class DataConverterTool : ToolBase<DataConverterTool.Args, DataConverterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Text))
        {
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Text));
        }

        JsonNode? data;
        try
        {
            data = args.From switch
            {
                SourceFormat.Json => JsonFormat.Read(args.Text),
                SourceFormat.Yaml => YamlFormat.Read(args.Text),
                SourceFormat.Toml => TomlFormat.Read(args.Text),
                SourceFormat.Xml => XmlFormat.Read(args.Text),
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.From))
            };
        }
        catch (UnreadableDataException)
        {
            throw new ToolException(ErrorCode.InputNotValid, nameof(Args.Text));
        }

        try
        {
            return new Result(args.To switch
            {
                TargetFormat.Json => JsonFormat.Write(data),
                TargetFormat.Yaml => YamlFormat.Write(data),
                TargetFormat.Toml => TomlFormat.Write(data),
                TargetFormat.Xml => XmlFormat.Write(data),
                TargetFormat.Csv => CsvFormat.Write(data),
                _ => throw new ToolException(ErrorCode.InputNotValid, nameof(Args.To))
            });
        }
        catch (UnwritableDataException)
        {
            throw new ToolException(ErrorCode.WrongFormat, nameof(Args.To));
        }
    }

    public enum SourceFormat
    {
        [Description("JSON")]
        Json,

        [Description("YAML, a single document")]
        Yaml,

        [Description("TOML")]
        Toml,

        [Description("XML")]
        Xml
    }

    public enum TargetFormat
    {
        [Description("JSON, indented")]
        Json,

        [Description("YAML")]
        Yaml,

        [Description("TOML; the data must be an object with no nulls")]
        Toml,

        [Description("XML, indented")]
        Xml,

        [Description("CSV; the data must be an array of objects")]
        Csv
    }

    public sealed record Args(
        [property: PipeInput,
                   Description("The data to convert, written in the From format")]
        string? Text,
        [property: Description("Format the text is written in")]
        SourceFormat From = SourceFormat.Json,
        [property: Description("Format to write the data in; the same format as From reformats it")]
        TargetFormat To = TargetFormat.Yaml
    );

    public sealed record Result(
        [property: PipeOutput,
                   Description("The data written in the To format")]
        string Text
    ) : ToolResult
    {
        public Result() : this(string.Empty)
        {
        }
    }
}
