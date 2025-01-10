using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Dev.Tools.CodeAnalysis.Generators;

[Generator]
public class ApiEndpointGenerator : IIncrementalGenerator
{
    public static readonly CodeBlock Attribute = new()
    {
        Namespace = "Dev.Tools",
        TypeName = "GenerateApiEndpointsAttribute",
        SyntaxTypeName = "GenerateApiEndpoints",
        GeneratorType = typeof(ApiEndpointGenerator),
        Content = """
                  [AttributeUsage(AttributeTargets.Assembly)]
                  internal class {TypeName} : System.Attribute;
                  """
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            ctx => ctx.AddSource(Attribute.OutputFileName, Attribute)
        );

        var assembliesWithGenerateApiAttribute = context
            .SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => IsAssemblyAttribute(s),
                transform: (ctx, _) => GetAssemblyAttribute(ctx))
            .Where(c => c != null);

        var compilationAndAttributes = context
            .CompilationProvider
            .Combine(assembliesWithGenerateApiAttribute.Collect());

        context.RegisterSourceOutput(compilationAndAttributes, (spc, source) =>
        {
            var (compilation, attributes) = source;
            if (!attributes.Any())
            {
                return;
            }

            //Debugger.Launch();
            var tools = compilation
                .References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Where(it => it.Name == ToolsDefinitionGenerator.Attribute.Namespace)
                .SelectMany(it => it.GlobalNamespace
                    .GetAllTypes(t => t.HasAttribute(ToolsDefinitionGenerator.Attribute.TypeFullName)))
                .Select(it => new TypeInfo
                {
                    ToolName = it.GetAttributes()
                        .Where(x => x.AttributeClass?.ToDisplayString() ==
                                    ToolsDefinitionGenerator.Attribute.TypeFullName)
                        .SelectMany(x => x.NamedArguments)
                        .Where(x => x.Key == "Name")
                        .Select(x => x.Value.Value?.ToString())
                        .FirstOrDefault() ?? "",
                    ToolType = it.ToDisplayString(),
                    ToolTypeName = it.Name,
                    ToolArgsType = it.GetMembers()
                        .OfType<ITypeSymbol>()
                        .Where(x => x.BaseType?.ToDisplayString() == "Dev.Tools.Core.ToolArgs")
                        .Select(x => x.ToDisplayString())
                        .First(),
                    ToolResultType = it.GetMembers()
                        .OfType<ITypeSymbol>()
                        .Where(x => x.BaseType?.ToDisplayString() == "Dev.Tools.Core.ToolResult")
                        .Select(x => x.ToDisplayString())
                        .First()
                })
                .ToArray();

            //Debugger.Launch();
            foreach (var tool in tools)
            {
                var code = GenerateApiController(tool);
                spc.AddSource(code.OutputFileName, code);
            }
        });
    }

    private CodeBlock GenerateApiController(TypeInfo tool)
    {
        return new()
        {
            Namespace = "Dev.Tools.Endpoints",
            TypeName = tool.ToolTypeName + "Endpoint",
            GeneratorType = typeof(ApiEndpointGenerator),
            Placeholders = new()
            {
                ["ToolName"] = tool.ToolName,
                ["ToolType"] = tool.ToolType,
                ["ToolArgsType"] = tool.ToolArgsType,
                ["ToolResultType"] = tool.ToolResultType
            },
            Usings = [
                "Dev.Tools.Api.Core",
                "Microsoft.AspNetCore.Mvc",
                "System.Net",
                "System.Net.Mime"
            ],
            Content = """
                      public class {TypeName}(Dev.Tools.Providers.IToolsProvider toolProvider) : EndpointBase
                      {
                          [HttpPost("{ToolName}")]
                          [ProducesResponseType<{ToolResultType}>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
                          [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
                          [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
                          public async Task<IActionResult> HandleAsync([FromBody] {ToolArgsType} request, CancellationToken cancellationToken)
                          {
                              try 
                              {  
                                  var tool = toolProvider.GetTool<{ToolType}>();  
                                  {ToolResultType} result = await tool.RunAsync(request, cancellationToken);
                                  if (result.HasErrors)
                                  {
                                      return Problem(type: result.ErrorCodes[0], statusCode: (int)HttpStatusCode.BadRequest);
                                  }
                          
                                  return Ok(result);
                              }
                              catch (Exception e)
                              {
                                  return Problem(title: "Failed to run tool", detail: e.Message, statusCode: (int)HttpStatusCode.InternalServerError);
                              }
                          }
                      }
                      """
        };
    }

    private static bool IsAssemblyAttribute(SyntaxNode node)
    {
        var result = node is AttributeListSyntax attributeListSyntax &&
                     attributeListSyntax.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) == true;

        return result;
    }

    private static SyntaxNode? GetAssemblyAttribute(GeneratorSyntaxContext context)
    {
        var attributeListSyntax = (AttributeListSyntax)context.Node;
        var result = attributeListSyntax.Attributes
            .FirstOrDefault(attr => ModelExtensions
                .GetTypeInfo(context.SemanticModel, attr).Type?.Name == "GenerateApiEndpointsAttribute");
        return result;
    }


    private record TypeInfo
    {
        public string ToolName { get; set; } = null!;
        public string ToolTypeName { get; set; } = null!;
        public string ToolType { get; set; } = null!;
        public string ToolArgsType { get; set; } = null!;
        public string ToolResultType { get; set; } = null!;
    }
}