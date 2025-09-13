using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ApiEndpointGeneratorTests: GeneratorTestsBase
{
    [Test, Skip("Just for now")]
    public async Task Should_Generate_Tools_Provider()
    {
        var toolCode = new CodeBlock
        {
            Namespace = "Dev.Tools.Tests",
            TypeName = "TestTool",
            Usings = [
                CodeDefinitions.ToolsArgs.Namespace,
                CodeDefinitions.ToolsResult.Namespace,
                CodeDefinitions.ToolDefinitionAttribute.Namespace
            ],
            Placeholders =
            {
                ["ToolDefinition"] = CodeDefinitions.ToolDefinitionAttribute.SyntaxTypeName,
                ["ToolArgs"] = CodeDefinitions.ToolsArgs.SyntaxTypeName,
                ["ToolResult"] = CodeDefinitions.ToolsResult.SyntaxTypeName,
            },
            Content = """
                      [{ToolDefinition}(Name = "tool-test")]
                      public sealed class TestTool {
                        public record Args : {ToolArgs};
                        public record Result : {ToolResult};
                      }
                      """
        };
        
        var reference = GetMetadataReference(CodeDefinitions.ToolsAssemblyName, [
            toolCode,
            CodeDefinitions.ToolDefinitionAttribute,
            CodeDefinitions.ToolsArgs,
            CodeDefinitions.ToolsResult
        ]);
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools.Tests",
            Usings = [
                CodeDefinitions.GenerateApiEndpointsAttribute.Namespace,
            ],
            Placeholders =
            {
                ["GenerateApiEndpoints"] = CodeDefinitions.GenerateApiEndpointsAttribute.SyntaxTypeName
            },
            Content = """
                      [assembly:{GenerateApiEndpoints}]
                      """
        };
        await Verify<ApiEndpointGenerator>(code, reference);
    }
}