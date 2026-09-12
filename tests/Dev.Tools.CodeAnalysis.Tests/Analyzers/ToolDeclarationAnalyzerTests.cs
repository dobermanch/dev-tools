using Dev.Tools.CodeAnalysis.Analyzers;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dev.Tools.CodeAnalysis.Tests.Analyzers;

public class ToolDeclarationAnalyzerTests
{
    /// <summary>
    /// The attribute and tool interface the analyzer looks for, declared in the test compilation so
    /// the tests do not depend on the real core assembly.
    /// </summary>
    private const string Preamble =
        """
        public class ToolDefinitionAttribute: System.Attribute {
             public string Name { get; set; }
             public string Title { get; set; }
             public string Description { get; set; }
        }

        public interface ITool<TArgs, TResult> {}
        """;

    private static CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier> Analyze(
        string content,
        Dictionary<string, string>? placeholders = null)
    {
        var block = new CodeBlock
        {
            Namespace = "Dev.Tools",
            Content = content
        };

        foreach (var placeholder in placeholders ?? [])
        {
            block.Placeholders[placeholder.Key] = placeholder.Value;
        }

        return new CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = block.ToString()
        };
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_name_is_valid_and_unique()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does the first thing")]
              public sealed class MyClass1 {}

              [ToolDefinition(Name = "tool2", Description = "Does the second thing")]
              public sealed class MyClass2 {}
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_name_is_not_unique()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = {|#0:"tool1"|}, Description = "Does the first thing")]
              public sealed class MyClass1 {}

              [ToolDefinition(Name = {|#1:"tool1"|}, Description = "Does the second thing")]
              public sealed class MyClass2 {}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    [Arguments("internal sealed")]
    [Arguments("public")]
    [Arguments("sealed")]
    public async Task Should_report_diagnostic_When_class_not_public(string modifiers)
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              {|#0:[ToolDefinition(Name = "tool1", Description = "Does a thing")]
              {modifiers} class MyClass1 {}|}
              """",
            new Dictionary<string, string> { ["modifiers"] = modifiers });

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotPublicRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    [Arguments("1tool")]
    [Arguments("_tool")]
    [Arguments("tool$")]
    [Arguments("tool name")]
    [Arguments("verylongnameforthetool")]
    public async Task Should_report_diagnostic_When_name_is_not_valid(string name)
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = {|#0:"{name}"|}, Description = "Does a thing")]
              public sealed class MyClass1 {}
              """",
            new Dictionary<string, string> { ["name"] = name });

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotValidRule)
            .WithArguments(name)
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_tool_has_no_description()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [{|#0:ToolDefinition(Name = "tool1")|}]
              public sealed class MyClass1 {}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MissingDescriptionRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task Should_report_diagnostic_When_tool_description_is_blank(string description)
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", {|#0:Description = "{description}"|})]
              public sealed class MyClass1 {}
              """",
            new Dictionary<string, string> { ["description"] = description });

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MissingDescriptionRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_description_spans_several_lines()
    {
        // Detailed text is the point of allowing multi-line descriptions.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Title = "My Tool", Description = "Summary line.\nDetail paragraph.")]
              public sealed class MyClass1 {}
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_title_spans_several_lines()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", {|#0:Title = "My\nTool"|}, Description = "Does a thing")]
              public sealed class MyClass1 {}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MultiLineTitleRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_argument_or_result_property_is_not_described()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public sealed record Args(
                      [property: System.ComponentModel.Description("Described")] string Described,
                      string {|#0:Bare|});

                  public sealed record Result(string {|#1:Value|});
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MissingPropertyDescriptionRule)
            .WithArguments("Bare", "Args")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MissingPropertyDescriptionRule)
            .WithArguments("Value", "Result")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_an_enum_value_is_not_described()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public enum Mode
                  {
                      [System.ComponentModel.Description("Described")] Described,
                      {|#0:Bare|}
                  }

                  public sealed record Args(
                      [property: System.ComponentModel.Description("Which mode")] Mode Mode);

                  public sealed record Result(
                      [property: System.ComponentModel.Description("The value")] string Value);
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MissingEnumValueDescriptionRule)
            .WithArguments("Bare", "Mode")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_everything_is_described()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Title = "My Tool", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public enum Mode
                  {
                      [System.ComponentModel.Description("The only mode")] Only
                  }

                  public sealed record Args(
                      [property: System.ComponentModel.Description("Which mode")] Mode Mode,
                      [property: System.ComponentModel.Description("Text to work on")] string Text);

                  public sealed record Result(
                      [property: System.ComponentModel.Description("The value")] string Value);
              }
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_require_descriptions_on_types_the_catalog_never_reads()
    {
        // Helper types nested in a tool are not part of its argument or result contract,
        // so they are none of the analyzer's business.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public readonly record struct Detail(char Char, string HexCode);

                  public sealed record Args(
                      [property: System.ComponentModel.Description("Text to work on")] string Text);

                  public sealed record Result(
                      [property: System.ComponentModel.Description("The details")] Detail[] Details);
              }
              """");

        await context.RunAsync();
    }
}
