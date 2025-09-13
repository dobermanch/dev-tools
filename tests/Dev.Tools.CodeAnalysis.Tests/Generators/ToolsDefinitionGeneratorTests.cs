using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ToolsDefinitionGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task Should_Generate_Tools_Provider()
    {
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "TestTool",
            Header = "",
            GeneratorType = typeof(ToolsDefinitionGenerator),
            Content = """
                      [ToolDefinition(
                          Name = "tool-test",
                          Aliases = ["tt"],
                          Keywords = ["test", "keywords"],
                          Categories = ["default"],
                          ErrorCodes = ["unknown"]
                      )]
                      public sealed class TestTool {}
                      """
        };
        await Verify<ToolsDefinitionGenerator>(code);
    }
}