using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Parse command line arguments
if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet exec <path-to-script> <assembly-dll-path> <output-directory>");
    Console.WriteLine("  <assembly-dll-path>: Path to Dev.Tools.dll");
    Console.WriteLine("  <output-directory>: Directory where RESX files will be generated");
    return 1;
}

string assemblyPath = args[0];
string outputDirectory = args[1];

if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Error: Assembly not found: {assemblyPath}");
    return 1;
}

try
{
    Console.WriteLine($"Loading assembly using reflection: {assemblyPath}");
    var generator = new ReflectionBasedResxGenerator(assemblyPath, outputDirectory);
    generator.Generate();
    Console.WriteLine("✓ RESX files generated successfully!");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

// ========== ReflectionBasedResxGenerator ==========

/// <summary>
/// Generates RESX files by loading the assembly and using reflection to access LocalizationKeysProvider.
/// </summary>
class ReflectionBasedResxGenerator(string assemblyPath, string outputDirectory)
{
    public void Generate()
    {
        Console.WriteLine($"Loading assembly: {assemblyPath}");

        // Create a custom assembly load context to isolate the loaded assembly
        var assemblyDir = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
        var loadContext = new CustomAssemblyLoadContext(assemblyDir);

        Assembly assembly;
        try
        {
            assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load assembly: {ex.Message}", ex);
        }

        // Find the LocalizationKeysProvider type
        var keysProviderType = assembly.GetType("Dev.Tools.LocalizationKeysProvider");
        if (keysProviderType == null)
        {
            throw new TypeLoadException("Could not find Dev.Tools.LocalizationKeysProvider type in assembly. Make sure the assembly has been built correctly.");
        }

        // Get the AllToolKeys property
        var toolKeysProperty = keysProviderType.GetProperty("AllToolKeys", BindingFlags.Public | BindingFlags.Static);
        if (toolKeysProperty == null)
        {
            throw new MissingMemberException("Could not find AllToolKeys property in LocalizationKeysProvider.");
        }

        Console.WriteLine($"Found LocalizationKeysProvider type, attempting to get AllToolKeys...");

        // Get the tool keys collection
        object? toolKeysValue;
        try
        {
            toolKeysValue = toolKeysProperty.GetValue(null);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get AllToolKeys property value: {ex.InnerException?.Message ?? ex.Message}", ex);
        }

        var toolKeys = toolKeysValue as System.Collections.IEnumerable;
        if (toolKeys == null)
        {
            throw new InvalidOperationException("AllToolKeys property returned null.");
        }

        Console.WriteLine($"Successfully retrieved localization keys");

        // Create RESX writer
        var toolResources = new SimpleResxWriter();

        // Process tool keys
        ProcessToolKeys(toolKeys, toolResources);

        // Create output directories
        Directory.CreateDirectory(outputDirectory);

        // Write RESX files
        toolResources.Save(Path.Combine(outputDirectory, "ToolResources.resx"));

        Console.WriteLine($"✓ Generated RESX files in: {outputDirectory}");
    }

    private static void ProcessToolKeys(System.Collections.IEnumerable toolKeys, SimpleResxWriter toolResources)
    {
        var toolList = toolKeys.Cast<object>().ToList();

        Console.WriteLine();
        Console.WriteLine($"Found {toolList.Count} tools");
        Console.WriteLine();

        var globalEnums = new Dictionary<string, HashSet<string>>
        {
            { "Category", new HashSet<string>() },
            { "ErrorCode", new HashSet<string>() },
            { "Keyword", new HashSet<string>() },
        };

        foreach (var toolKey in toolList)
        {
            // Get tool properties using reflection
            var toolName = GetPropertyValue<string>(toolKey, "ToolName");
            var categories = GetPropertyValue<Array>(toolKey, "Categories");
            var keywords = GetPropertyValue<Array>(toolKey, "Keywords");
            var errorCodes = GetPropertyValue<Array>(toolKey, "ErrorCodes");
            var argsProperties = GetPropertyValue<Array>(toolKey, "ArgsProperties");
            var resultProperties = GetPropertyValue<Array>(toolKey, "ResultProperties");
            var enums = GetPropertyValue<Array>(toolKey, "Enums");

            if (string.IsNullOrEmpty(toolName))
            {
                Console.WriteLine($"  Skipping tool with no name");
                continue;
            }

            Console.WriteLine($"  Tool: {toolName}");

            // Generate tool name and description keys (using colon format to match existing pattern)
            toolResources.AddResource($"Tools.{toolName}.Name", $"{toolName}");
            toolResources.AddResource($"Tools.{toolName}.Description", $"Description for {toolName}");
            Console.WriteLine($"    Tools.{toolName}.Name");
            Console.WriteLine($"    Tools.{toolName}.Description");

            // Process categories, keywords, and error codes
            if (categories != null)
            {
                ExtractEnumValuesFromStrings(categories, "Category", globalEnums["Category"]);
            }

            if (keywords != null)
            {
                ExtractEnumValuesFromStrings(keywords, "Keyword", globalEnums["Keyword"]);
            }

            if (errorCodes != null)
            {
                ExtractEnumValuesFromStrings(errorCodes, "ErrorCode", globalEnums["ErrorCode"]);
            }

            // Process args properties
            if (argsProperties is { Length: > 0 })
            {
                ProcessProperties(argsProperties, $"Tools.{toolName}.Args", toolResources);
            }

            // Process result properties
            if (resultProperties is { Length: > 0 })
            {
                ProcessProperties(resultProperties, $"Tools.{toolName}.Result", toolResources);
            }

            // Process enums
            if (enums is { Length: > 0 })
            {
                ProcessEnums(enums, $"Tools.{toolName}.Enums", toolResources);
            }

            Console.WriteLine();
        }

        // Generate global enum resources
        foreach (var values in globalEnums)
        {
            Console.WriteLine($"  Enums: {values.Key}");

            foreach (var value in values.Value)
            {
                var key = $"Enums.{values.Key}.{value}";
                toolResources.AddResource(key, $"{SplitCamelCase(value)}");
                Console.WriteLine($"    {key}");
            }

            Console.WriteLine();
        }
    }

    private static void ExtractEnumValuesFromStrings(Array enumArray, string enumName, HashSet<string> values)
    {
        foreach (var item in enumArray)
        {
            if (item == null) continue;

            var itemString = item.ToString();
            if (string.IsNullOrEmpty(itemString)) continue;

            // Remove enum type prefix (e.g., "Category.Converter" -> "Converter")
            var parts = itemString.Split('.');
            var enumValue = parts.Length > 1 ? parts[1] : itemString;

            if (!string.IsNullOrEmpty(enumValue) && enumValue != enumName)
            {
                values.Add(enumValue);
            }
        }
    }

    private static void ProcessProperties(Array properties, string keyPrefix, SimpleResxWriter toolResources)
    {
        foreach (var property in properties)
        {
            if (property == null) continue;

            var propName = GetPropertyValue<string>(property, "Name");
            if (string.IsNullOrEmpty(propName))
            {
                continue;
            }

            toolResources.AddResource($"{keyPrefix}.{propName}.Name", $"{propName}");
            toolResources.AddResource($"{keyPrefix}.{propName}.Description", $"{propName} description");

            Console.WriteLine($"      {keyPrefix}.{propName}.*");
        }
    }

    private static void ProcessEnums(Array enums, string keyPrefix, SimpleResxWriter toolResources)
    {
        foreach (var enumObj in enums)
        {
            if (enumObj == null) continue;

            var enumName = GetPropertyValue<string>(enumObj, "Name");
            var values = GetPropertyValue<Array>(enumObj, "Values");

            if (string.IsNullOrEmpty(enumName) || values == null)
            {
                continue;
            }

            Console.WriteLine($"      Enum: {keyPrefix}.{enumName}");

            foreach (var value in values)
            {
                if (value == null) continue;

                var valueStr = value.ToString();
                if (string.IsNullOrEmpty(valueStr)) continue;

                var displayValue = SplitCamelCase(valueStr);
                toolResources.AddResource($"{keyPrefix}.{enumName}.{valueStr}", $"{displayValue}");
                Console.WriteLine($"        {keyPrefix}.{enumName}.{valueStr}");
            }
        }
    }

    private static T? GetPropertyValue<T>(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property == null)
        {
            return default;
        }

        var value = property.GetValue(obj);
        return value is T typedValue ? typedValue : default;
    }

    private static string SplitCamelCase(string input)
        => string.IsNullOrEmpty(input)
            ? input
            : Regex.Replace(input, "(?<!^)([A-Z])", " $1");
}

/// <summary>
/// Custom assembly load context to isolate loaded assemblies and resolve dependencies.
/// </summary>
class CustomAssemblyLoadContext(string assemblyDirectory) : AssemblyLoadContext
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to load from the same directory as the main assembly
        var assemblyPath = Path.Combine(assemblyDirectory, assemblyName.Name + ".dll");
        if (File.Exists(assemblyPath))
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Let the default context handle it
        return null;
    }
}

// ========== SimpleResxWriter ==========

/// <summary>
/// Simple RESX file writer that creates XML-based resource files.
/// </summary>
class SimpleResxWriter
{
    private readonly Dictionary<string, string> _resources = new();

    public void AddResource(string name, string value)
        => _resources[name] = $":{value}:";

    public void Save(string filePath)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("root",
                new XComment(
                    $"""
                     This code was generated by the ResxGenerator.
                           Changes to this file may cause incorrect behavior and will be lost
                           if the code is regenerated.
                     """
                ),
                new XElement("resheader",
                    new XAttribute("name", "resmimetype"),
                    new XElement("value", "text/microsoft-resx")),
                new XElement("resheader",
                    new XAttribute("name", "version"),
                    new XElement("value", "2.0")),
                new XElement("resheader",
                    new XAttribute("name", "reader"),
                    new XElement("value", "System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                new XElement("resheader",
                    new XAttribute("name", "writer"),
                    new XElement("value", "System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")),
                _resources
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp =>
                        new XElement("data",
                            new XAttribute("name", kvp.Key),
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            new XElement("value", kvp.Value))
                    )
            )
        );

        doc.Save(filePath);
    }
}
