using Dev.Tools.Generators.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.Generators;

[Generator]
public class ValuesListGenerator : IIncrementalGenerator
{
    private static readonly CodeBlock AttributeCode = new ()
    {
        Namespace = "Dev.Tools",
        TypeName = "GenerateValuesAttribute",
        SyntaxTypeName = "GenerateValues",
        GeneratorType = typeof(ValuesListGenerator),
        Content = """
                  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
                  public class {TypeName}: System.Attribute;
                  """
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(AttributeCode.OutputFileName, AttributeCode)
        );

        // Register a syntax receiver to filter syntax nodes
        var classDeclarations = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsClassWithGenerateValuesAttribute(s),
                transform: (ctx, _) => GetClassWithConstants(ctx))
            .Where(c => c != null);

        var compilationAndClasses = context
            .CompilationProvider
            .Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var (_, classes) = source;
            foreach (var info in classes.Where(it => it is not null))
            {
                if (!info!.IsPartial)
                {
                    spc.ReportDiagnostic(
                        Diagnostic.Create(
                            Diagnostics.NotPartialRule,
                            info.Location,
                            info.TypeName,
                            AttributeCode.TypeName
                        ));
                    continue;
                }

                var codeBlock = GenerateGetValuesMethod(info);
                spc.AddSource($"{info.TypeName}_GetValues.g.cs", codeBlock);
            }
        });
    }

    private static bool IsClassWithGenerateValuesAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax 
               && node.HasAttribute(AttributeCode.SyntaxTypeName);
    }

    private static TypeInfo? GetClassWithConstants(GeneratorSyntaxContext context)
    {
        var (syntax, symbol) = context.GetTypeNode();
        if (symbol is null)
        {
            return null;
        }

        var hasGenerateValuesAttribute = symbol
            .GetAttributes()
            .Any(it => it.ToString() == AttributeCode.TypeFullName);
        if (!hasGenerateValuesAttribute)
        {
            return null;
        }
        
        var constants = symbol
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => field.IsConst)
            .Select(field => field.Name)
            .ToList();

        return new TypeInfo
        {
            Syntax = syntax,
            Symbol = symbol,
            Constants = constants
        };
    }

    private static CodeBlock GenerateGetValuesMethod(TypeInfo info) =>
        new()
        {
            Namespace = info.Namespace,
            TypeName = info.TypeName,
            GeneratorType = typeof(ValuesListGenerator),
            Placeholders = new ()
            {
                ["ObjectType"] = (info.IsRecord, info.IsValueType) switch
                {
                    (true, true) => "record struct",
                    (true, false) => "record",
                    (false, true) => "struct",
                    _ => "class"
                },
                ["Constants"] = string.Join(", ", info.Constants.Select(c => $"\"{c}\""))
            },
            Content = """
                      public partial {ObjectType} {TypeName}
                      {
                          public static IEnumerable<string> GetValues()
                          {
                              return new List<string> { {Constants} };
                          }
                      }
                      """
        };

    private record TypeInfo : TypeBaseInfo
    {
        public List<string> Constants { get; set; }
    }
}