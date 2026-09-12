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

var english = new ToolCatalogStringLocalizer(
        new ServiceCollection().AddDevTools().BuildServiceProvider().GetRequiredService<IToolsProvider>())
    .GetAllStrings(false)
    .ToDictionary(it => it.Name, it => it.Value, StringComparer.Ordinal);

Console.WriteLine($"English keys in the tool sources: {english.Count}");

if (!File.Exists(path))
{
    if (!write)
    {
        Console.Error.WriteLine($"No translation file at {path}. Re-run with --write to scaffold it.");
        return 1;
    }

    new XDocument(new XDeclaration("1.0", "utf-8", null), Header()).Save(path);
    Console.WriteLine($"Created {path}");
}

// Loaded without PreserveWhitespace so a rewritten file comes back out indented and diffable.
var document = XDocument.Load(path);
var root = document.Root ?? throw new InvalidOperationException($"{path} has no root element");

var existing = root
    .Elements("data")
    .Where(it => it.Attribute("name") is not null)
    .ToDictionary(it => it.Attribute("name")!.Value, it => it, StringComparer.Ordinal);

var missing = english.Keys.Where(it => !existing.ContainsKey(it)).OrderBy(it => it, StringComparer.Ordinal).ToArray();
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

foreach (var key in stale)
{
    existing[key].Remove();
}

foreach (var key in missing)
{
    // Pre-filled with English so the file stays readable and a translator edits rather than types.
    root.Add(new XElement("data",
        new XAttribute("name", key),
        new XAttribute(XNamespace.Xml + "space", "preserve"),
        new XElement("value", english[key])));
}

// Keep the file in key order so a translation diff shows only what actually changed.
var ordered = root.Elements("data").OrderBy(it => it.Attribute("name")!.Value, StringComparer.Ordinal).ToArray();
foreach (var element in ordered)
{
    element.Remove();
}

root.Add(ordered);
document.Save(path);

Console.WriteLine();
Console.WriteLine($"Wrote {path}: +{missing.Length} -{stale.Length}");
return 0;

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
