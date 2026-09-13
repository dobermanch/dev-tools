using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class McpToolGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task ShouldGenerateAnMcpToolPerTool()
    {
        await Verify<McpToolGenerator>(
            ConsumerAssembly("Dev.Tools.Mcp.Core", "GenerateToolsMcpToolAttribute"),
            ToolsAssembly);
    }

    [Test]
    public async Task ShouldGenerateNothingWithoutTheAssemblyAttribute()
    {
        await VerifyNoOutput<McpToolGenerator>(EmptyAssembly, ToolsAssembly);
    }
}
