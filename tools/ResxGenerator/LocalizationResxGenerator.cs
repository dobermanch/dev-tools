using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ResxGenerator;

/// <summary>
/// Generates RESX files by parsing the generated ToolsCatalog.g.cs source file using Roslyn.
/// </summary>
class LocalizationResxGenerator(string assemblyPath, string outputDirectory)
{
    public void Generate()
    {
        // Find the generated ToolsCatalog.g.cs file
        var toolsCatalogFile = FindGeneratedFile("ToolsCatalog.g.cs");
        if (toolsCatalogFile == null)
        {
            throw new FileNotFoundException("Could not find generated ToolsCatalog.g.cs file. Make sure the Dev.Tools project has been built.");
        }

        Console.WriteLine($"Reading generated source: {toolsCatalogFile}");

        // Parse the C# file using Roslyn
        var sourceCode = File.ReadAllText(toolsCatalogFile);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        // Create RESX writers
        var toolResources = new SimpleResxWriter();

        // Parse and generate
        ParseAndGenerate(root, toolResources);

        // Create output directories
        Directory.CreateDirectory(outputDirectory);
        var resourcesDir = Path.Combine(outputDirectory, "Resources");
        Directory.CreateDirectory(resourcesDir);

        // Write RESX files
        toolResources.Save(Path.Combine(resourcesDir, "ToolResources.resx"));

        Console.WriteLine($"✓ Generated RESX files in: {resourcesDir}");
    }

    private string? FindGeneratedFile(string fileName)
    {
        // Assembly is at: {project}/bin/{config}/{tfm}/Assembly.dll
        // obj is at: {project}/obj/{config}/{tfm}/generated/...
        // So we need to go up 3 levels from assembly directory to get to project root
        var assemblyDir = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
        var projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));

        var objDir = Path.Combine(projectDir, "obj");
        if (!Directory.Exists(objDir))
        {
            Console.WriteLine($"Warning: obj directory not found at {objDir}");
            Console.WriteLine($"Assembly path: {assemblyPath}");
            Console.WriteLine($"Assembly dir: {assemblyDir}");
            Console.WriteLine($"Project dir: {projectDir}");
            return null;
        }

        var files = Directory.GetFiles(objDir, fileName, SearchOption.AllDirectories);
        return files.Length > 0 ? files[0] : null;
    }

    private static void ParseAndGenerate(SyntaxNode root, SimpleResxWriter toolResources)
    {
        // Find the ToolsCatalog class
        var catalogClass = root
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == "ToolsCatalog");

        if (catalogClass == null)
        {
            Console.Error.WriteLine("Error: Could not find ToolsCatalog class");
            return;
        }

        // Find the ToolDefinitions property which contains all tool definitions
        var allProperty = catalogClass
            .Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == "ToolDefinitions");

        if (allProperty == null)
        {
            Console.Error.WriteLine("Error: Could not find ToolDefinitions property in ToolsCatalog");
            return;
        }

        var expressions = allProperty
            .DescendantNodes()
            .OfType<CollectionExpressionSyntax>()
            .Select(it => it.Elements
                .OfType<ExpressionElementSyntax>()
                .Select(x => x.Expression)
                .ToList())
            .FirstOrDefault();
        
        if (expressions is not { Count: > 0 })
        {
            expressions = allProperty
                .DescendantNodes()
                .OfType<ArrayCreationExpressionSyntax>()
                .Select(it => it.Initializer?.Expressions.ToList() ?? [])
                .FirstOrDefault();
        }

        if (expressions is { Count: > 0 })
        {
            GenerateToolKeysFromExpressions(expressions, toolResources);
        }
        else
        {
            Console.Error.WriteLine("Error: Could not find collection expression or array initializer in ToolDefinitions property");
        }
    }

    private static void GenerateToolKeysFromExpressions(IEnumerable<ExpressionSyntax> expressions, SimpleResxWriter toolResources)
    {
        var toolList = expressions.OfType<ObjectCreationExpressionSyntax>().ToList();

        Console.WriteLine();
        Console.WriteLine($"Found {toolList.Count} tools");
        Console.WriteLine();
        
        var globalEnums = new Dictionary<string, HashSet<string>>
        {
            { "Category", [] },
            { "ErrorCode", [] },
            { "Keyword", [] },
        };

        foreach (var objectCreation in toolList)
        {
            if (objectCreation.ArgumentList == null)
            {
                continue;
            }

            string? className = null;

            // Extract tool class name from named arguments: ToolType: typeof(...)
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                var argName = argument.NameColon?.Name.Identifier.Text;

                // Extract class name from typeof(Namespace.ClassName)
                if (argName == "ToolType" && argument.Expression is TypeOfExpressionSyntax typeofExpr)
                {
                    className = typeofExpr.Type.ToString().Split('.').Last().Replace("Tool", "");
                }
            }

            if (string.IsNullOrEmpty(className))
            {
                Console.WriteLine($"  Skipping tool with no class name (tool={objectCreation})");
                continue;
            }

            Console.WriteLine($"  Tool: {className}");

            // Generate tool name and description keys
            toolResources.AddResource($"Tools.{className}.Name", className);
            toolResources.AddResource($"Tools.{className}.Description", $"Description for {className}");
            Console.WriteLine($"    Tools.{className}.Name");
            Console.WriteLine($"    Tools.{className}.Description");

            // Process ArgsType and ResultType from named arguments
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                switch (argument.NameColon?.Name.Identifier.Text)
                {
                    case "Categories":
                        ExtractEnumValues(argument.Expression, "Category", globalEnums["Category"]);
                        break;
                    case "Keywords":
                        ExtractEnumValues(argument.Expression, "Keyword", globalEnums["Keyword"]);
                        break;
                    case "ErrorCodes":
                        ExtractEnumValues(argument.Expression, "ErrorCode", globalEnums["ErrorCode"]);
                        break;
                    case "ArgsType":
                        ProcessTypeDetails(argument.Expression, $"Tools.{className}.Args", toolResources);
                        break;
                    case "ReturnType":
                        ProcessTypeDetails(argument.Expression, $"Tools.{className}.Result", toolResources);
                        break;
                    case "ExtraTypes":
                        ProcessToolEnums(argument.Expression, $"Tools.{className}.Enums", toolResources);
                        break;
                }
            }

            Console.WriteLine();
        }

        foreach (var values in globalEnums)
        {
            Console.WriteLine($"  Enums: {values.Key}");
            
            foreach (var value in values.Value)
            {
                var key = $"Enums.{values.Key}.{value}";
                toolResources.AddResource(key, SplitCamelCase(value));
                Console.WriteLine($"    {key}");
            }
            
            Console.WriteLine();
        }
    }

    private static void ExtractEnumValues(ExpressionSyntax expression, string enumName, HashSet<string> values)
    {
        IEnumerable<ExpressionSyntax> expressionSyntax = GetCollectionCreationExpressionSyntax(expression);
        foreach (var element in expressionSyntax)
        {
            if (element is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name.Identifier.Text != enumName)
            {
                values.Add(memberAccess.Name.Identifier.Text);
            }
        }
    }

    private static void ProcessTypeDetails(ExpressionSyntax expression, string keyPrefix, SimpleResxWriter toolResources)
    {
        // Find the TypeDetails object creation
        var typeDetailsCreation = expression.DescendantNodesAndSelf()
            .OfType<ObjectCreationExpressionSyntax>()
            .FirstOrDefault(it => it.Type.ToString().Contains("TypeDetails"));

        foreach (var argument in typeDetailsCreation?.ArgumentList?.Arguments ?? [])
        {
            if (argument.NameColon?.Name.Identifier.Text == "Properties")
            {
                ProcessProperties(argument.Expression, keyPrefix, toolResources);
            }
        }
    }

    private static void ProcessProperties(ExpressionSyntax expression, string keyPrefix, SimpleResxWriter toolResources)
    {
        IEnumerable<ExpressionSyntax> propertyExpressions = GetCollectionCreationExpressionSyntax(expression);
        foreach (var propExpr in propertyExpressions)
        {
            if (propExpr is not ObjectCreationExpressionSyntax propCreation)
            {
                continue;
            }

            // Extract property name from first argument
            var arguments = propCreation.ArgumentList?.Arguments;
            if (arguments == null || arguments.Value.Count == 0)
            {
                continue;
            }

            var firstArg = arguments.Value[0].Expression;
            if (firstArg is LiteralExpressionSyntax literal)
            {
                var propName = literal.Token.ValueText;
                toolResources.AddResource($"{keyPrefix}.{propName}.Name", propName);
                toolResources.AddResource($"{keyPrefix}.{propName}.Description", $"{propName} description");

                Console.WriteLine($"      {keyPrefix}.{propName}.*");
            }
        }
    }

    private static void ProcessToolEnums(ExpressionSyntax expression, string keyPrefix, SimpleResxWriter enumResources)
    {
        IEnumerable<ExpressionSyntax> enumExpressions = GetCollectionCreationExpressionSyntax(expression);
        foreach (var enumExpr in enumExpressions)
        {
            if (enumExpr is not ObjectCreationExpressionSyntax enumCreation)
            {
                continue;
            }

            // Extract enum name and values from arguments
            var arguments = enumCreation.ArgumentList?.Arguments;
            if (arguments == null || arguments.Value.Count < 3)
            {
                continue;
            }

            // First argument is enum name
            var enumNameArg = arguments.Value[0].Expression;
            if (enumNameArg is not LiteralExpressionSyntax enumNameLiteral)
            {
                continue;
            }

            var enumName = enumNameLiteral.Token.ValueText;
            Console.WriteLine($"      Enum: {keyPrefix}.{enumName}");

            // Third argument is array of values
            var valuesArg = arguments.Value[2].Expression;
            IEnumerable<ExpressionSyntax> valueExpressions = GetCollectionCreationExpressionSyntax(valuesArg);
            
            foreach (var valueExpr in valueExpressions)
            {
                if (valueExpr is LiteralExpressionSyntax valueLiteral)
                {
                    var value = valueLiteral.Token.ValueText;
                    var displayValue = SplitCamelCase(value);

                    enumResources.AddResource($"{keyPrefix}.{enumName}.{value}", displayValue);
                    Console.WriteLine($"        {keyPrefix}.{enumName}.{value}");
                }
            }
        }
    }
    
    private static IEnumerable<ExpressionSyntax> GetCollectionCreationExpressionSyntax(ExpressionSyntax? expression)
        // Handle array/collection expressions like [Category.Foo, Category.Bar] or new[] { Category.Foo }
        => expression switch
        {
            CollectionExpressionSyntax collection => collection.Elements.OfType<ExpressionElementSyntax>().Select(it => it.Expression),
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            ArrayCreationExpressionSyntax array => array.Initializer?.Expressions ?? [],
            _ => []
        };

    private static string SplitCamelCase(string input)
        => string.IsNullOrEmpty(input)
            ? input
            : Regex.Replace(input, "(?<!^)([A-Z])", " $1");
}