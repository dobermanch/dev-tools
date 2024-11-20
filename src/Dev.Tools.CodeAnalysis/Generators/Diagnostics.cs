using Microsoft.CodeAnalysis;

namespace Dev.Tools.CodeAnalysis.Generators;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor NotPartialRule = new(
        "DT2001",
        "Class should be partial",
        "The '{0}' class marked with {1} attribute should be partial",
        "Generation",
        DiagnosticSeverity.Error,
        description: "Class should be partial.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);
    
    public static readonly DiagnosticDescriptor NotPublicRule = new(
        "DT2002",
        "Class should be public and sealed",
        "The '{0}' class marked with {1} attribute should be public and sealed",
        "Generation",
        DiagnosticSeverity.Error,
        description: "Class should be public and sealed.",
        isEnabledByDefault: true,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);
}