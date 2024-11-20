using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dev.Tools.Analyzers.Tests.Analyzers;

public class ToolNameAnalyzerTests
{
    [Fact]
    public async Task Should_not_report_diagnostic_When_name_is_valid_and_unique()
    {
        var context = new CSharpAnalyzerTest<ToolNameAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                       using System;
                       namespace MyApp {
                           public class ToolDefinitionAttribute: Attribute {
                                public string Name { get; set; }
                           }
                       
                           [ToolDefinition(Name = "tool1")]
                           public class MyClass1 {}
                       
                           [ToolDefinition(Name = "tool2")]
                           public class MyClass2 {}
                       }
                       """
        };

        await context.RunAsync();
    }

    [Fact]
    public async Task Should_report_diagnostic_When_name_is_not_unique()
    {
        var context = new CSharpAnalyzerTest<ToolNameAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = """
                       using System;
                       namespace MyApp {
                           public sealed class ToolDefinitionAttribute : Attribute
                           {
                               public string Name { get; set; }
                           }

                           [ToolDefinition(Name = {|#0:"tool1"|})]
                           public class MyClass1 {}

                           [ToolDefinition(Name = {|#1:"tool1"|})] 
                           public class MyClass2 {}
                       }
                       """
        };

        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolNameAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(0));
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolNameAnalyzer.NotUniqueRule)
            .WithArguments("tool1")
            .WithLocation(1));

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
        var context = new CSharpAnalyzerTest<ToolNameAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck,
            TestCode = """
                       using System;
                       namespace MyApp {
                           public sealed class ToolDefinitionAttribute : Attribute
                           {
                               public string Name { get; set; }
                           }
                       """
                       + $"[ToolDefinition(Name = {{|#0:\"{name}\"|}})]"
                       + """
                           public class MyClass1 {}
                       }
                       """
        };
        
        context.ExpectedDiagnostics.Add(new DiagnosticResult(ToolNameAnalyzer.NotValidRule)
            .WithArguments(name)
            .WithLocation(0));

        await context.RunAsync();
    }
}