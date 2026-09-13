using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Dev.Tools.DataFormats;

/// <summary>
/// YAML read and written as the events of a single document, with no reflection involved. Plain
/// scalars are typed by the YAML 1.2 core schema - an unquoted true, 42 or null is that type, a quoted
/// one is a string, and yes, no, on and off are strings - and a string that would read back as another
/// type is written quoted.
/// </summary>
internal static partial class YamlFormat
{
    // An alias copies the node it names, so a few lines can describe an enormous document ("billion
    // laughs"). Reading stops once the expanded data passes this many nodes.
    private const int MaxNodes = 100_000;

    private const string StringTag = "tag:yaml.org,2002:str";

    public static JsonNode? Read(string text)
    {
        try
        {
            var parser = new Parser(new StringReader(text));
            parser.Consume<StreamStart>();
            if (parser.TryConsume<StreamEnd>(out _))
            {
                return null;
            }

            parser.Consume<DocumentStart>();
            var data = new Reader(parser).ReadNode();
            parser.Consume<DocumentEnd>();

            if (!parser.TryConsume<StreamEnd>(out _))
            {
                throw new UnreadableDataException("A YAML stream with more than one document");
            }

            return data;
        }
        catch (YamlException exception)
        {
            throw new UnreadableDataException("Not valid YAML", exception);
        }
    }

    public static string Write(JsonNode? data)
    {
        var writer = new StringWriter(CultureInfo.InvariantCulture);
        // No line width: a long string stays on one line instead of being folded across several.
        var emitter = new Emitter(writer, 2, int.MaxValue);

        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());
        WriteNode(emitter, data);
        emitter.Emit(new DocumentEnd(true));
        emitter.Emit(new StreamEnd());

        return writer.ToString();
    }

    private static void WriteNode(IEmitter emitter, JsonNode? node)
    {
        switch (node)
        {
            case null:
                WritePlain(emitter, "null");
                break;
            case JsonObject data:
                emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, true, MappingStyle.Block));
                foreach (var (key, value) in data)
                {
                    WriteString(emitter, key);
                    WriteNode(emitter, value);
                }

                emitter.Emit(new MappingEnd());
                break;
            case JsonArray items:
                emitter.Emit(new SequenceStart(AnchorName.Empty, TagName.Empty, true, SequenceStyle.Block));
                foreach (var item in items)
                {
                    WriteNode(emitter, item);
                }

                emitter.Emit(new SequenceEnd());
                break;
            case JsonValue value when value.GetValueKind() == JsonValueKind.String:
                WriteString(emitter, value.GetValue<string>());
                break;
            default:
                // Numbers and booleans, spelled the way JSON spells them, which the core schema reads back.
                WritePlain(emitter, node.ToJsonString());
                break;
        }
    }

    private static void WritePlain(IEmitter emitter, string text)
        => emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, text, ScalarStyle.Plain, true, false));

    // A string that would read back as another type - "true", "42", "" - is double-quoted so it stays a
    // string. Text with line breaks asks for a literal block, which keeps them as they are (the emitter
    // falls back to quotes when a block cannot hold it); anything else lets the emitter pick.
    private static void WriteString(IEmitter emitter, string text)
    {
        var style = ResolvePlain(text) is not JsonValue value || value.GetValueKind() != JsonValueKind.String
            ? ScalarStyle.DoubleQuoted
            : text.Contains('\n') ? ScalarStyle.Literal : ScalarStyle.Any;

        emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, text, style, true, true));
    }

    /// <summary>A plain scalar's type by the YAML 1.2 core schema.</summary>
    private static JsonNode? ResolvePlain(string text)
    {
        switch (text)
        {
            case "" or "~" or "null" or "Null" or "NULL":
                return null;
            case "true" or "True" or "TRUE":
                return JsonValue.Create(true);
            case "false" or "False" or "FALSE":
                return JsonValue.Create(false);
        }

        if (DecimalInteger().IsMatch(text))
        {
            return Integer(BigInteger.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
        }

        if (HexadecimalInteger().IsMatch(text))
        {
            // The leading 0 keeps the value positive: BigInteger reads a first digit of 8-F as a sign bit.
            return Integer(BigInteger.Parse("0" + text[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
        }

        if (OctalInteger().IsMatch(text))
        {
            return Integer(text[2..].Aggregate(BigInteger.Zero, (total, digit) => total * 8 + (digit - '0')));
        }

        // .inf and .nan are floats to YAML, but JSON has no way to write them, so they stay strings.
        if (Float().IsMatch(text)
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && double.IsFinite(number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(text);
    }

    // Parsed from its digits, so an integer wider than a long keeps every one of them.
    private static JsonNode Integer(BigInteger value) => JsonNode.Parse(value.ToString(CultureInfo.InvariantCulture))!;

    [GeneratedRegex("^[-+]?[0-9]+$")]
    private static partial Regex DecimalInteger();

    [GeneratedRegex("^0x[0-9a-fA-F]+$")]
    private static partial Regex HexadecimalInteger();

    [GeneratedRegex("^0o[0-7]+$")]
    private static partial Regex OctalInteger();

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$")]
    private static partial Regex Float();

    private sealed class Reader(IParser parser)
    {
        // What each anchor named, and how many nodes it holds, so an alias can be counted before it is copied.
        private readonly Dictionary<string, (JsonNode? Node, int Size)> _anchors = new(StringComparer.Ordinal);
        private int _nodes;

        public JsonNode? ReadNode()
        {
            if (parser.TryConsume<AnchorAlias>(out var alias))
            {
                if (!_anchors.TryGetValue(alias.Value.Value, out var anchored))
                {
                    throw new UnreadableDataException("An alias to an anchor that is not defined before it");
                }

                Count(anchored.Size);
                return anchored.Node?.DeepClone();
            }

            var start = _nodes;
            Count(1);

            AnchorName anchor;
            JsonNode? node;
            if (parser.TryConsume<Scalar>(out var scalar))
            {
                anchor = scalar.Anchor;
                node = Resolve(scalar);
            }
            else if (parser.TryConsume<SequenceStart>(out var sequence))
            {
                anchor = sequence.Anchor;
                var items = new JsonArray();
                while (!parser.TryConsume<SequenceEnd>(out _))
                {
                    items.Add(ReadNode());
                }

                node = items;
            }
            else
            {
                var mapping = parser.Consume<MappingStart>();
                anchor = mapping.Anchor;
                var data = new JsonObject();
                while (!parser.TryConsume<MappingEnd>(out _))
                {
                    // A key is read as its text; a key that is itself a mapping or a sequence has no JSON form.
                    var key = parser.Consume<Scalar>().Value;
                    if (data.ContainsKey(key))
                    {
                        throw new UnreadableDataException("A mapping with the same key twice");
                    }

                    data[key] = ReadNode();
                }

                node = data;
            }

            if (!anchor.IsEmpty)
            {
                _anchors[anchor.Value] = (node, _nodes - start);
            }

            return node;
        }

        // A quoted scalar, one tagged !!str, or one with the non-specific tag ! is a string whatever it
        // looks like. Most scalars have no tag, and TagName.Value throws for those, so IsEmpty comes first.
        private static JsonNode? Resolve(Scalar scalar)
            => scalar.Style != ScalarStyle.Plain
               || (!scalar.Tag.IsEmpty && (scalar.Tag.IsNonSpecific || scalar.Tag.Value == StringTag))
                ? JsonValue.Create(scalar.Value)
                : ResolvePlain(scalar.Value);

        private void Count(int nodes)
        {
            _nodes += nodes;
            if (_nodes > MaxNodes)
            {
                throw new UnreadableDataException("YAML whose aliases expand past the node limit");
            }
        }
    }
}
