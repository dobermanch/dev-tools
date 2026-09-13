using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Dev.Tools;
using Dev.Tools.Localization;
using Dev.Tools.Providers;
using Microsoft.Extensions.DependencyInjection;

// Checks a translation file against the English the tools carry in their own source, and can
// scaffold the missing entries so a translator only has to fill in values.
//
//   dotnet run --project tools/Translations -- <culture> [--resources <dir>] [--write]
//
// Without --write nothing is touched: it prints what is missing and what is stale and exits 1 if
// either exists, which is what makes it usable from a script.
//
// --write edits the file rather than rewriting it: a missing entry goes next to the entries of its
// own tool (or under a new section comment when the tool has none yet), a stale one is taken out,
// and the order, the section comments and the indentation a translator kept stay as they were.

if (args.Length < 1 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Usage: dotnet run --project tools/Translations -- <culture> [--resources <dir>] [--write]");
    Console.WriteLine("  <culture>         the culture to check, e.g. uk-UA");
    Console.WriteLine("  --resources <dir> the directory holding the ToolResources files;");
    Console.WriteLine("                    defaults to this repository's Dev.Tools.Localization/Resources");
    Console.WriteLine("  --write           add missing entries (pre-filled with English) and drop stale ones");
    return args.Length < 1 ? 1 : 0;
}

var culture = args[0];
var write = args.Contains("--write");

var resourcesDirectory = ResourcesOption(args) ?? FindResourcesDirectory();
if (resourcesDirectory is null)
{
    Console.Error.WriteLine(
        "Could not find the resources directory from here. Pass it explicitly: --resources <dir>");
    return 1;
}

if (!Directory.Exists(resourcesDirectory))
{
    Console.Error.WriteLine($"No such directory: {resourcesDirectory}");
    return 1;
}

var path = Path.GetFullPath(Path.Combine(resourcesDirectory, $"ToolResources.{culture}.resx"));

// In catalog order - a tool's title, description, arguments, results, enums - which is the order a
// missing entry is placed in.
string[] keys;
Dictionary<string, string> english;
{
    var strings = new ToolCatalogStringLocalizer(
            new ServiceCollection().AddDevTools().BuildServiceProvider().GetRequiredService<IToolsProvider>())
        .GetAllStrings(false)
        .ToArray();

    keys = strings.Select(it => it.Name).ToArray();
    english = strings.ToDictionary(it => it.Name, it => it.Value, StringComparer.Ordinal);
}

Console.WriteLine($"English keys in the tool sources: {english.Count}");

if (!File.Exists(path))
{
    if (!write)
    {
        Console.Error.WriteLine($"No translation file at {path}. Re-run with --write to scaffold it.");
        return 1;
    }

    Save(new XDocument(new XDeclaration("1.0", "utf-8", null), Header()), path, indent: true);
    Console.WriteLine($"Created {path}");
}

// Whitespace is kept, so everything a write does not add or remove comes back out as it went in.
var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
var root = document.Root ?? throw new InvalidOperationException($"{path} has no root element");

var existing = root
    .Elements("data")
    .Where(it => it.Attribute("name") is not null)
    .ToDictionary(it => it.Attribute("name")!.Value, it => it, StringComparer.Ordinal);

var missing = keys.Where(it => !existing.ContainsKey(it)).ToArray();
var stale = existing.Keys.Where(it => !english.ContainsKey(it)).OrderBy(it => it, StringComparer.Ordinal).ToArray();

Report("Missing (no translation yet)", missing, key => english[key]);
Report("Stale (nothing asks for these any more)", stale, key => existing[key].Element("value")?.Value ?? string.Empty);

if (missing.Length == 0 && stale.Length == 0)
{
    Console.WriteLine($"{culture} is complete and has nothing stale.");
    return 0;
}

if (!write)
{
    Console.WriteLine();
    Console.WriteLine("Re-run with --write to apply.");
    return 1;
}

var indent = IndentOf(root);

foreach (var key in stale)
{
    RemoveWithIndent(existing[key]);
    existing.Remove(key);
}

if (stale.Length > 0)
{
    RemoveEmptySections(root);
}

XElement? previous = null;
string? previousSection = null;
foreach (var key in keys)
{
    var section = SectionOf(key);
    if (section != previousSection)
    {
        previous = null;
        previousSection = section;
    }

    if (existing.TryGetValue(key, out var present))
    {
        previous = present;
        continue;
    }

    // Pre-filled with English so the file stays readable and a translator edits rather than types.
    var entry = new XElement("data",
        new XAttribute("name", key),
        new XAttribute(XNamespace.Xml + "space", "preserve"),
        new XText("\n" + indent + indent),
        new XElement("value", english[key]),
        new XText("\n" + indent));

    var next = root.Elements("data").FirstOrDefault(it => SectionOf(it.Attribute("name")?.Value ?? "") == section);
    if (previous is not null)
    {
        previous.AddAfterSelf(new XText("\n" + indent), entry);
    }
    else if (next is not null)
    {
        next.AddBeforeSelf(entry, new XText("\n" + indent));
    }
    else
    {
        object[] opened =
        [
            new XText("\n\n" + indent), new XComment($" {SectionTitle(section)} "), new XText("\n" + indent), entry
        ];

        if (root.Elements().LastOrDefault() is { } last)
        {
            last.AddAfterSelf(opened);
        }
        else
        {
            root.Add(opened, new XText("\n"));
        }
    }

    existing[key] = entry;
    previous = entry;
}

Save(document, path, indent: false);

Console.WriteLine();
Console.WriteLine($"Wrote {path}: +{missing.Length} -{stale.Length}");
return 0;

// "Tools.TextStatistics.Result.Words.Name" belongs to "Tools.TextStatistics", "Enums.Keyword.Ip" to "Enums.Keyword".
static string SectionOf(string key)
{
    var first = key.IndexOf('.');
    var second = first < 0 ? -1 : key.IndexOf('.', first + 1);

    return second < 0 ? key : key[..second];
}

string SectionTitle(string section)
    => english.TryGetValue($"{section}.Name", out var title)
        ? $"{title} Tool"
        : $"Global {Regex.Replace(section[(section.IndexOf('.') + 1)..], "(?<=[a-z])(?=[A-Z])", " ")} Enums";

// The indentation the file already uses for the root's children; two spaces for a file that has none.
static string IndentOf(XElement root)
    => root.Elements()
        .Select(it => it.PreviousNode)
        .OfType<XText>()
        .Select(it => it.Value[(it.Value.LastIndexOf('\n') + 1)..])
        .FirstOrDefault() ?? "  ";

static void RemoveWithIndent(XNode node)
{
    if (node.PreviousNode is XText text && string.IsNullOrWhiteSpace(text.Value))
    {
        text.Remove();
    }

    node.Remove();
}

// A section comment with nothing left under it - the next thing is another comment, or the end.
static void RemoveEmptySections(XElement root)
{
    foreach (var comment in root.Nodes().OfType<XComment>().ToArray())
    {
        var next = comment.NodesAfterSelf().FirstOrDefault(it => it is not XText text || !string.IsNullOrWhiteSpace(text.Value));
        if (next is null or XComment)
        {
            RemoveWithIndent(comment);
        }
    }
}

// Without a byte order mark, and with line breaks written as they are rather than as this platform's.
static void Save(XDocument document, string path, bool indent)
{
    var settings = new XmlWriterSettings
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = indent,
        IndentChars = "  ",
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.None
    };

    using (var writer = XmlWriter.Create(path, settings))
    {
        document.Save(writer);
    }

    // A loaded file keeps the line break after </root> as a node of its own; a new one has none yet.
    if (document.LastNode is not XText)
    {
        File.AppendAllText(path, "\n");
    }
}

static string? ResourcesOption(string[] args)
{
    var index = Array.IndexOf(args, "--resources");

    return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : null;
}

/// <summary>
/// Locates this repository's resources by walking up for the solution file, rather than counting
/// "../" segments out of the build output - that count changes with the configuration and the
/// target framework. Returns null when run from outside the repository, where --resources is the
/// only sensible answer.
/// </summary>
static string? FindResourcesDirectory()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
         directory is not null;
         directory = directory.Parent)
    {
        if (directory.EnumerateFiles("*.slnx").Any())
        {
            return Path.Combine(directory.FullName, "src", "Dev.Tools.Localization", "Resources");
        }
    }

    return null;
}

static void Report(string heading, string[] keys, Func<string, string> value)
{
    if (keys.Length == 0)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"{heading}: {keys.Length}");
    foreach (var key in keys)
    {
        Console.WriteLine($"  {key} = {value(key)}");
    }
}

static XElement Header()
    => new("root",
        new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
        new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
        new XElement("resheader", new XAttribute("name", "reader"),
            new XElement("value",
                "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
        new XElement("resheader", new XAttribute("name", "writer"),
            new XElement("value",
                "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")));
