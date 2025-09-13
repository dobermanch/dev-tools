using Dev.Tools.CodeAnalysis.Core;
using Dev.Tools.CodeAnalysis.Generators;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public class ValueListGeneratorTests : GeneratorTestsBase
{
    [Test]
    public async Task Should_Generate_GetValues_Method()
    {
        var code = new CodeBlock
        {
            Namespace = "Dev.Tools",
            TypeName = "Category",
            Header = "",
            GeneratorType = typeof(ValuesListGenerator),
            Content = """
                      [GenerateValues]
                      public partial record struct Category(string Value)
                      {
                          public const string None = nameof(None);
                          public const string Converter = nameof(Converter);
                      }
                      """
        };
        await Verify<ValuesListGenerator>(code);
    }
}