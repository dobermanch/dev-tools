using Dev.Tools.Generators;
using Dev.Tools.Generators.Core;

namespace Dev.Tools.Tests.Generators;

public class ValueListGeneratorTests : GeneratorTestsBase
{
    [Fact]
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