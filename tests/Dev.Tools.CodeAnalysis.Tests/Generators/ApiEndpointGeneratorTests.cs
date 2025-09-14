using Dev.Tools.Api.Core;
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
            Content = """
                      [ToolDefinition(
                          Name = "tool-test",
                          Aliases = ["tt"],
                          Keywords = [Keyword.Value1, Keyword.Value2],
                          Categories = [Category.Value1]
                      )]
                      public sealed class TestTool : ITool<int, long> {
                        public void Test1() {
                            throw new ToolException("testCode"); 
                        }
                        public void Test2() {
                          throw new ToolException(Errors.TextEmpty); 
                        }
                      }
                      
                      public interface ITool<TArgs, TResult> {}
                      
                      public class Errors {
                        public const string TextEmpty = "TextEmpty";
                      }
                      
                      public sealed class ToolException(string errorCode) : System.Exception
                      {
                          public string ErrorCode { get; } = errorCode;
                      }
                      """
        };
        
        var reference = GetMetadataReference(CodeDefinitions.ToolsAssemblyName, [
            toolCode,
            CodeDefinitions.ToolDefinitionAttribute,
            CodeDefinitions.ToolsArgs,
            CodeDefinitions.ToolsResult,
            CodeDefinitions.Keywords,
            CodeDefinitions.Category
        ]);
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools.Tests",
            Usings = [
                typeof(GenerateToolsApiEndpointAttribute).Namespace,
            ],
            Placeholders =
            {
                ["GenerateApiEndpoints"] = nameof(GenerateToolsApiEndpointAttribute)
            },
            Content = """
                      [assembly:{GenerateApiEndpoints}]
                      """
        };
        await Verify<ApiEndpointGenerator>(code, reference);
    }
}