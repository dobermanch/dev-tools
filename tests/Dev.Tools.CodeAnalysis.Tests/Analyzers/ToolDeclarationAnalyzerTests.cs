using Dev.Tools.CodeAnalysis.Analyzers;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dev.Tools.CodeAnalysis.Tests.Analyzers;

public class ToolDeclarationAnalyzerTests
{
    /// <summary>
    /// The attributes and tool interface the analyzer looks for, declared in the test compilation so
    /// the tests do not depend on the real core assembly. They land in the Dev.Tools namespace, which
    /// is where the analyzer expects ITool&lt;,&gt; to live.
    /// </summary>
    private const string Preamble =
        """
        public class ToolDefinitionAttribute: System.Attribute {
             public string Name { get; set; }
             public string Title { get; set; }
             public string Description { get; set; }
             public string[] Aliases { get; set; }
        }

        public interface ITool<TArgs, TResult> {}

        public sealed class PipeInputAttribute : System.Attribute {}

        public sealed class PipeOutputAttribute : System.Attribute {}

        public sealed class ToolContent {}
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
              public sealed class MyClass1 : ITool<object, object> {}

              [ToolDefinition(Name = "tool2", Description = "Does the second thing")]
              public sealed class MyClass2 : ITool<object, object> {}
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
              public sealed class MyClass1 : ITool<object, object> {}

              [ToolDefinition(Name = {|#1:"tool1"|}, Description = "Does the second thing")]
              public sealed class MyClass2 : ITool<object, object> {}
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
              {modifiers} class MyClass1 : ITool<object, object> {}|}
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
              public sealed class MyClass1 : ITool<object, object> {}
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
              public sealed class MyClass1 : ITool<object, object> {}
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
              public sealed class MyClass1 : ITool<object, object> {}
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
              public sealed class MyClass1 : ITool<object, object> {}
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
              public sealed class MyClass1 : ITool<object, object> {}
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

    [Test]
    public async Task Should_report_diagnostic_When_tool_does_not_implement_ITool()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              {|#0:[ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 {}|}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotAToolRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_tool_only_implements_some_other_two_argument_interface()
    {
        // The generators used to accept any generic interface with two type arguments as the tool
        // interface, so this class would have had string and int generated as its args and result.
        var context = Analyze(
            $$""""
              {{Preamble}}

              public interface IPair<TLeft, TRight> {}

              {|#0:[ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : IPair<string, int> {}|}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotAToolRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_tool_implements_ITool_through_a_base_class()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              public abstract class ToolBase<TArgs, TResult> : ITool<TArgs, TResult> {}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ToolBase<object, object> {}
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_more_than_one_argument_is_piped()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, object>
              {
                  public sealed record Args(
                      [property: PipeInput, System.ComponentModel.Description("First")] string {|#0:First|},
                      [property: PipeInput, System.ComponentModel.Description("Second")] string {|#1:Second|});
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousPipeInputRule)
            .WithArguments("Args", 2)
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousPipeInputRule)
            .WithArguments("Args", 2)
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_more_than_one_result_is_piped()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<object, MyClass1.Result>
              {
                  public sealed record Result(
                      [property: PipeOutput, System.ComponentModel.Description("First")] string {|#0:First|},
                      [property: PipeOutput, System.ComponentModel.Description("Second")] string {|#1:Second|});
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousPipeOutputRule)
            .WithArguments("Result", 2)
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousPipeOutputRule)
            .WithArguments("Result", 2)
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_nothing_is_piped()
    {
        // Generators take no input and parsers have no single output worth piping; both are fine.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public sealed record Args(
                      [property: System.ComponentModel.Description("How many")] int Count = 1);

                  public sealed record Result(
                      [property: System.ComponentModel.Description("Scheme")] string? Scheme,
                      [property: System.ComponentModel.Description("Host")] string? Host);
              }
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_arguments_have_several_public_constructors()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, object>
              {
                  public sealed class Args
                  {
                      public {|#0:Args|}() {}
                      public {|#1:Args|}(int count) {}
                  }
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousArgsConstructorRule)
            .WithArguments("Args", 2)
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.AmbiguousArgsConstructorRule)
            .WithArguments("Args", 2)
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_only_the_result_has_several_constructors()
    {
        // The documented Result shape pairs the positional constructor with a parameterless one.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<object, MyClass1.Result>
              {
                  public sealed record Result(
                      [property: System.ComponentModel.Description("The value")] string Value)
                  {
                      public Result() : this(string.Empty) { }
                  }
              }
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_an_argument_type_cannot_be_bound()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, object>
              {
                  public sealed record Args(
                      [property: System.ComponentModel.Description("A stream")] System.IO.Stream {|#0:Input|},
                      [property: System.ComponentModel.Description("A map")] System.Collections.Generic.Dictionary<string, int> {|#1:Map|});
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.UnsupportedArgumentTypeRule)
            .WithArguments("Input", "Args", "System.IO.Stream")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.UnsupportedArgumentTypeRule)
            .WithArguments("Map", "Args", "System.Collections.Generic.Dictionary<string, int>")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_argument_types_are_bindable()
    {
        // Every argument type the library uses today, plus the obvious neighbours.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, object>
              {
                  public enum Mode { [System.ComponentModel.Description("Only")] Only }

                  public sealed record Args(
                      [property: System.ComponentModel.Description("d")] string Text,
                      [property: System.ComponentModel.Description("d")] string? OptionalText,
                      [property: System.ComponentModel.Description("d")] bool Flag,
                      [property: System.ComponentModel.Description("d")] int Count,
                      [property: System.ComponentModel.Description("d")] char Separator,
                      [property: System.ComponentModel.Description("d")] Mode Mode,
                      [property: System.ComponentModel.Description("d")] Mode? OptionalMode,
                      [property: System.ComponentModel.Description("d")] System.Guid Namespace,
                      [property: System.ComponentModel.Description("d")] System.DateTime Time,
                      [property: System.ComponentModel.Description("d")] System.DateTimeOffset? When,
                      [property: System.ComponentModel.Description("d")] string[] Words,
                      [property: System.ComponentModel.Description("d")] System.Collections.Generic.IList<string> Codes);
              }
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_report_diagnostic_When_the_piped_argument_is_content()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, object>
              {
                  public sealed record Args(
                      [property: PipeInput, System.ComponentModel.Description("d")] ToolContent Input,
                      [property: System.ComponentModel.Description("d")] bool Flag);
              }
              """");

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_content_is_not_the_piped_argument()
    {
        // Only the piped argument has a frontend that can fill it from a file, and there are no content results yet.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Description = "Does a thing")]
              public sealed class MyClass1 : ITool<MyClass1.Args, MyClass1.Result>
              {
                  public sealed record Args(
                      [property: PipeInput, System.ComponentModel.Description("d")] string Text,
                      [property: System.ComponentModel.Description("d")] ToolContent {|#0:Attachment|});

                  public sealed record Result(
                      [property: PipeOutput, System.ComponentModel.Description("d")] ToolContent {|#1:Output|});
              }
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MisplacedContentRule)
            .WithArguments("Attachment", "Args")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.MisplacedContentRule)
            .WithArguments("Output", "Result")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_an_alias_repeats_another_tools_name()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = {|#0:"hash"|}, Description = "Does the first thing")]
              public sealed class MyClass1 : ITool<object, object> {}

              [ToolDefinition(Name = "encoder", Aliases = [{|#1:"hash"|}], Description = "Does the second thing")]
              public sealed class MyClass2 : ITool<object, object> {}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("hash")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("hash")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_report_diagnostic_When_two_tools_share_an_alias()
    {
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "tool1", Aliases = [{|#0:"t"|}], Description = "Does the first thing")]
              public sealed class MyClass1 : ITool<object, object> {}

              [ToolDefinition(Name = "tool2", Aliases = ["x", {|#1:"t"|}], Description = "Does the second thing")]
              public sealed class MyClass2 : ITool<object, object> {}
              """");

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("t")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("t")
            .WithLocation(1));

        await context.RunAsync();
    }

    [Test]
    public async Task Should_not_hold_aliases_to_the_name_format()
    {
        // "64e" starts with a digit, which a name may not; as an alias it is fine.
        var context = Analyze(
            $$""""
              {{Preamble}}

              [ToolDefinition(Name = "base64-encoder", Aliases = ["64e"], Description = "Encodes")]
              public sealed class MyClass1 : ITool<object, object> {}
              """");

        await context.RunAsync();
    }
}
