using Dev.Tools.CodeAnalysis.Analyzers;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dev.Tools.CodeAnalysis.Tests.Analyzers;

public class ToolDeclarationAnalyzerTests
{
    [Fact]
    public async Task Should_not_report_diagnostic_When_name_is_valid_and_unique()
    {
        var context = new CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = new CodeBlock
            {
                Namespace = "Dev.Tools",
                Content = """
                          public class ToolDefinitionAttribute: System.Attribute {
                               public string Name { get; set; }
                          }

                          [ToolDefinition(Name = "tool1")]
                          public sealed class MyClass1 {}

                          [ToolDefinition(Name = "tool2")]
                          public sealed class MyClass2 {}
                          """
            }.ToString()
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task Should_report_diagnostic_When_name_is_not_unique()
    {
        var context = new CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = new CodeBlock
            {
                Namespace = "Dev.Tools",
                Content = """
                          public class ToolDefinitionAttribute: System.Attribute {
                               public string Name { get; set; }
                          }

                          [ToolDefinition(Name = {|#0:"tool1"|})]
                          public sealed class MyClass1 {}

                          [ToolDefinition(Name = {|#1:"tool1"|})] 
                          public sealed class MyClass2 {}
                          """
            }.ToString()
        };

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(1));

        await context.RunAsync();
    }
    
    [Theory]
    [InlineData("internal sealed")]
    [InlineData("public")]
    [InlineData("sealed")]
    public async Task Should_report_diagnostic_When_class_not_public(string modifiers)
    {
        var context = new CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = new CodeBlock
            {
                Namespace = "Dev.Tools",
                Placeholders =
                {
                    ["modifiers"] = modifiers
                },
                Content = """
                          public class ToolDefinitionAttribute: System.Attribute {
                               public string Name { get; set; }
                          }

                          {|#0:[ToolDefinition(Name = "tool1")]
                          {modifiers} class MyClass1 {}|}
                          """
            }.ToString()
        };

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotPublicRule)
            .WithArguments("MyClass1")
            .WithLocation(0));

        await context.RunAsync();
    }

    [Theory]
    [InlineData("1tool")]
    [InlineData("_tool")]
    [InlineData("tool$")]
    [InlineData("tool name")]
    [InlineData("verylongnameforthetool")]
    public async Task Should_report_diagnostic_When_name_is_not_valid(string name)
    {
        var context = new CSharpAnalyzerTest<ToolDeclarationAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = new CodeBlock
            {
                Namespace = "Dev.Tools",
                Placeholders =
                {
                    ["name"] = name
                },
                Content = """"
                          public class ToolDefinitionAttribute: System.Attribute {
                               public string Name { get; set; }
                          }

                          [ToolDefinition(Name = {|#0:"{name}"|})]
                          public sealed class MyClass1 {}
                          """"
            }.ToString()
        };

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolDeclarationAnalyzer.NotValidRule)
            .WithArguments(name)
            .WithLocation(0));

        await context.RunAsync();
    }
}