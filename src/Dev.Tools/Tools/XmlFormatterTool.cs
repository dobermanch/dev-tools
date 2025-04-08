using System.Xml;

namespace Dev.Tools.Tools;

[ToolDefinition(
    Name = "xml-formatter",
    Aliases = ["xf"],
    Keywords = [Keyword.Text, Keyword.String, Keyword.Xml, Keyword.Format],
    Categories = [Category.Text],
    ErrorCodes = [ErrorCode.InputNotValid]
)]
public sealed class XmlFormatterTool : ToolBase<XmlFormatterTool.Args, XmlFormatterTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrWhiteSpace(args.Xml))
        {
            return Failed(ErrorCode.InputNotValid);
        }

        XmlDocument document = new XmlDocument();
        
        try
        {
            document.LoadXml(args.Xml);
        }
        catch (Exception)
        {
            return Failed(ErrorCode.InputNotValid);
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
        public required string Xml { get; init; } = null!;
        public int IndentSize { get; init; } = 2;
        public SortDirection SortKeys { get; init; } = SortDirection.None;
        public TextCase KeyFormat { get; init; } = TextCase.None;
        public bool ExcludeEmpty { get; init; }
        public bool Compact { get; init; }
    }

    public record Result : ToolResult
    {
        public string Xml { get; init; } = null!;
    }

    #endregion
}