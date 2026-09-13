using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace Dev.Tools.DataFormats;

/// <summary>
/// XML mapped to objects the common way. The root element is the one top-level key; attributes are
/// keys prefixed with <c>@</c>; text beside attributes or child elements is <c>#text</c>; an element
/// that repeats is an array. Every value is a string, an empty element (<c>&lt;a/&gt;</c>) is null, and
/// comments, processing instructions and the declaration are not kept. Names are kept as written,
/// namespace prefix included, and a name that is not a valid XML name is encoded the .NET way
/// (<c>first name</c> is <c>first_x0020_name</c>) on the way out and decoded on the way in.
/// </summary>
internal static class XmlFormat
{
    private const string AttributePrefix = "@";
    private const string TextKey = "#text";
    private const string RootName = "root";
    private const string ItemName = "item";
    private const string Indentation = "  ";

    public static JsonNode? Read(string text)
    {
        XDocument document;
        try
        {
            // No DTD and no resolver: an XML document cannot make the tool read a file or a URL, or
            // expand entities into something enormous.
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            document = XDocument.Load(reader);
        }
        catch (XmlException exception)
        {
            throw new UnreadableDataException("Not valid XML", exception);
        }

        var root = document.Root ?? throw new UnreadableDataException("XML without a root element");

        return new JsonObject { [NameOf(root)] = ReadElement(root) };
    }

    private static JsonNode? ReadElement(XElement element)
    {
        var attributes = element.Attributes().ToArray();
        var children = element.Elements().ToArray();
        // CDATA sections are text too: XCData is an XText.
        var texts = element.Nodes().OfType<XText>().ToArray();

        if (attributes.Length == 0 && children.Length == 0)
        {
            return element.IsEmpty ? null : JsonValue.Create(string.Concat(texts.Select(it => it.Value)));
        }

        var data = new JsonObject();
        foreach (var attribute in attributes)
        {
            data[AttributePrefix + NameOf(attribute)] = attribute.Value;
        }

        // Grouped by name in the order each name first appears; elements of different names that were
        // interleaved lose their relative order.
        foreach (var group in children.GroupBy(NameOf, StringComparer.Ordinal))
        {
            var values = group.Select(ReadElement).ToArray();
            data[group.Key] = values.Length == 1 ? values[0] : new JsonArray(values);
        }

        if (texts.Any(it => !string.IsNullOrWhiteSpace(it.Value)))
        {
            data[TextKey] = string.Concat(texts.Select(it => it.Value));
        }

        return data;
    }

    private static string NameOf(XElement element)
        => Qualified(element.GetPrefixOfNamespace(element.Name.Namespace), element.Name.LocalName);

    private static string NameOf(XAttribute attribute)
    {
        if (attribute.IsNamespaceDeclaration)
        {
            return attribute.Name.Namespace == XNamespace.None ? "xmlns" : "xmlns:" + attribute.Name.LocalName;
        }

        var prefix = attribute.Name.Namespace == XNamespace.None
            ? null
            : attribute.Parent?.GetPrefixOfNamespace(attribute.Name.Namespace);

        return Qualified(prefix, attribute.Name.LocalName);
    }

    private static string Qualified(string? prefix, string localName)
    {
        var name = XmlConvert.DecodeName(localName);
        return string.IsNullOrEmpty(prefix) ? name : prefix + ":" + name;
    }

    public static string Write(JsonNode? data)
    {
        var builder = new StringBuilder();

        if (data is JsonObject { Count: 1 } document && document.First() is { Value: not JsonArray } root)
        {
            WriteElement(builder, root.Key, root.Value, 0, inline: false);
        }
        else
        {
            WriteElement(builder, RootName, data, 0, inline: false);
        }

        return builder.ToString().TrimEnd('\n');
    }

    private static void WriteElement(StringBuilder builder, string name, JsonNode? node, int depth, bool inline)
    {
        var tag = ElementName(name);
        StartLine(builder, depth, inline);
        builder.Append('<').Append(tag);

        switch (node)
        {
            case null:
                builder.Append(" />");
                break;

            case JsonArray items:
                // An array with no key of its own - at the top, or inside another array.
                builder.Append('>');
                foreach (var item in items)
                {
                    WriteElement(builder, ItemName, item, depth + 1, inline);
                }

                StartLine(builder, depth, inline);
                builder.Append("</").Append(tag).Append('>');
                break;

            case JsonObject data:
                foreach (var (key, value) in data.Where(it => it.Key.StartsWith(AttributePrefix, StringComparison.Ordinal)))
                {
                    builder.Append(' ').Append(ElementName(key[AttributePrefix.Length..])).Append("=\"");
                    AppendEscaped(builder, ScalarText(value), attribute: true);
                    builder.Append('"');
                }

                var text = data.TryGetPropertyValue(TextKey, out var textNode) ? ScalarText(textNode) : null;
                var children = data
                    .Where(it => it.Key != TextKey && !it.Key.StartsWith(AttributePrefix, StringComparison.Ordinal))
                    .ToArray();

                if (children.Length == 0 && text is null)
                {
                    builder.Append(" />");
                    break;
                }

                builder.Append('>');
                if (text is not null)
                {
                    AppendEscaped(builder, text, attribute: false);
                }

                // Beside text, children are written without line breaks or indentation, which would
                // otherwise become part of the text when the document is read again.
                var childrenInline = inline || text is not null;
                foreach (var (key, value) in children)
                {
                    if (value is JsonArray repeated)
                    {
                        foreach (var item in repeated)
                        {
                            WriteElement(builder, key, item, depth + 1, childrenInline);
                        }
                    }
                    else
                    {
                        WriteElement(builder, key, value, depth + 1, childrenInline);
                    }
                }

                if (children.Length > 0)
                {
                    StartLine(builder, depth, childrenInline);
                }

                builder.Append("</").Append(tag).Append('>');
                break;

            default:
                builder.Append('>');
                AppendEscaped(builder, ScalarText(node), attribute: false);
                builder.Append("</").Append(tag).Append('>');
                break;
        }
    }

    private static void StartLine(StringBuilder builder, int depth, bool inline)
    {
        if (inline)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('\n');
        }

        for (var i = 0; i < depth; i++)
        {
            builder.Append(Indentation);
        }
    }

    // A value written as an attribute or as text: a string as it is, a number or boolean as JSON spells
    // it, null as nothing. An object or an array has no place there.
    private static string ScalarText(JsonNode? node) => node switch
    {
        null => string.Empty,
        JsonValue value when value.GetValueKind() == JsonValueKind.String => value.GetValue<string>(),
        JsonValue value => value.ToJsonString(),
        _ => throw new UnwritableDataException("An object or array where XML needs an attribute value or text")
    };

    private static string ElementName(string name)
    {
        if (name.Length == 0)
        {
            throw new UnwritableDataException("An empty key, which cannot be an XML name");
        }

        var separator = name.IndexOf(':');
        return separator > 0 && separator < name.Length - 1
            ? XmlConvert.EncodeLocalName(name[..separator]) + ":" + XmlConvert.EncodeLocalName(name[(separator + 1)..])
            : XmlConvert.EncodeLocalName(name);
    }

    private static void AppendEscaped(StringBuilder builder, string text, bool attribute)
    {
        try
        {
            XmlConvert.VerifyXmlChars(text);
        }
        catch (XmlException exception)
        {
            throw new UnwritableDataException("A character XML 1.0 cannot hold", exception);
        }

        foreach (var ch in text)
        {
            builder.Append(ch switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' when attribute => "&quot;",
                // A line break or tab in an attribute is normalised to a space when read, unless it is a reference.
                '\n' when attribute => "&#10;",
                '\r' => "&#13;",
                '\t' when attribute => "&#9;",
                _ => ch.ToString()
            });
        }
    }
}
