using System.Xml;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "xml-formatter",
    Title = "XML Formatter",
    Description =
        """
        Reformats XML with control over indentation, element order, element name casing and empty elements.

        The document is parsed and re-serialized, so the original whitespace is lost and malformed
        XML is rejected rather than repaired. Compact output overrides the indent size.
        """,
    Usage =
        "Formatting only - this never changes element values, just how the document is written. Renaming elements through KeyFormat changes the meaning of the document for anything that reads it by name.",
    Aliases = ["xf"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Xml, Keyword.Format],
    Categories = [Category.Text]
)]
public sealed class XmlFormatterTool : ToolBase<XmlFormatterTool.Args, XmlFormatterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Xml))
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        XmlDocument document = new XmlDocument();
        
        try
        {
            document.LoadXml(args.Xml);
        }
        catch (Exception)
        {
            throw new ToolException(ErrorCode.InputNotValid);
        }

        var xmlNode = Format(document.DocumentElement!, args, document);
        
        return new Result { Xml = SerializeXml(xmlNode, args) };
    }
    
    private static XmlNode Format(XmlElement element, Args args, XmlDocument doc)
    {
        if (element is not { HasChildNodes: true, FirstChild.NodeType: XmlNodeType.Element })
        {
            XmlElement xmlElement = doc.CreateElement(FormatKey(element.Name, args.KeyFormat));
            xmlElement.InnerText = element.InnerText;
            foreach (XmlAttribute child in element.Attributes)
            {
                xmlElement.SetAttribute(
                    FormatKey(child.LocalName, args.KeyFormat),
                    child.NamespaceURI,
                    child.Value
                );
            }
            
            return xmlElement;
        }

        IDictionary<string, XmlNode> sortedDictionary =
            args.SortKeys == SortDirection.None
                ? new Dictionary<string, XmlNode>()
                : new SortedDictionary<string, XmlNode>(
                    args.SortKeys == SortDirection.Ascending
                        ? Comparer<string>.Default
                        : Comparer<string>.Create((x, y) => string.Compare(y, x, StringComparison.Ordinal))
                );

        for (var index = 0; index < element.ChildNodes.Count; index++)
        {
            var child = (XmlElement)element.ChildNodes[index]!;
            if (!args.ExcludeEmpty || !string.IsNullOrWhiteSpace(child.InnerText))
            {
                sortedDictionary[$"{child.Name}{index}"] = Format(child, args, doc);
            }
        }

        return CreateElement(element, doc, sortedDictionary.Values, args);
    }

    private static XmlElement CreateElement(XmlElement element, XmlDocument doc, IEnumerable<XmlNode> sortedList, Args args)
    {
        XmlElement xmlElement = doc.CreateElement(FormatKey(element.Name, args.KeyFormat));
        foreach (XmlAttribute child in element.Attributes)
        {
            xmlElement.SetAttribute(
                FormatKey(child.LocalName, args.KeyFormat),
                child.NamespaceURI,
                child.Value
            );
        }
        
        foreach (var node in sortedList)
        {
            xmlElement.AppendChild(node);
        }

        return xmlElement;
    }

    static string SerializeXml(XmlNode xmlDoc, Args args)
    {
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
        {
            Indent = !args.Compact,
            IndentChars = new string(' ', args.IndentSize), 
            Async = true,
            CloseOutput = true,
            OmitXmlDeclaration = true,
            NamespaceHandling = NamespaceHandling.OmitDuplicates,
            ConformanceLevel = ConformanceLevel.Document
        });
        xmlDoc.WriteTo(xmlWriter);
        xmlWriter.Flush();
        return stringWriter.ToString();
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
                   DisplayName("XML"),
                   Description("XML text to format")]
        string Xml,
        [property: Description("Number of spaces per indent level; ignored when Compact is set")]
        int IndentSize = 2,
        [property: DisplayName("Sort Elements"),
                   Description("Order to sort sibling elements in")]
        SortDirection SortKeys = SortDirection.None,
        [property: DisplayName("Element Name Format"),
                   Description("Case format applied to every element name")]
        TextCase KeyFormat = TextCase.None,
        [property: Description("Drop elements with no value and no children")]
        bool ExcludeEmpty = false,
        [property: Description("Write the document on a single line with no extra whitespace")]
        bool Compact = false
    );

    public record Result : ToolResult
    {
        [PipeOutput]
        [DisplayName("Formatted XML")]
        [Description("The reformatted XML document")]
        public string Xml { get; init; } = null!;
    }

    #endregion
}