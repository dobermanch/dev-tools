using Dev.Tools.CodeAnalysis.Generators;
using Dev.Tools.CodeAnalysis.Tests.Fixtures;
using Microsoft.CodeAnalysis.CSharp;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ToolsCatalogGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task ShouldGenerateTheCatalogFromTheToolsInTheAssembly()
    {
        await Verify<ToolsCatalogGenerator>(CSharpSyntaxTree.ParseText(SampleTools.Source));
    }

    /// <summary>
    /// The pipeline has to produce the same values twice for the same input, or every consumer
    /// re-generates everything on every keystroke. This was skipped while the model carried the
    /// ISymbol and TypeDeclarationSyntax it was read from, which are never equal between runs.
    /// </summary>
    [Test]
    public async Task ShouldReuseItsCachedOutputWhenNothingChanged()
    {
        await VerifyCaching<ToolsCatalogGenerator, ToolPipeline.TrackingNames>(
            CSharpSyntaxTree.ParseText(SampleTools.Source));
    }
}
