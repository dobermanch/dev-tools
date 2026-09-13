using Dev.Tools.CodeAnalysis.Core;

namespace Dev.Tools.CodeAnalysis.Generators;

/// <summary>
/// Everything the generators know about one tool, as plain data.
///
/// Nothing here is a Roslyn object. The model used to carry the <c>ITypeSymbol</c> and
/// <c>TypeDeclarationSyntax</c> it was built from, which pins a whole compilation in the
/// incremental cache and makes every model instance unequal to the one before it - so the pipeline
/// re-ran from scratch on every keystroke, and the caching test that would have caught it was
/// skipped. Symbols are read once, in <see cref="ToolModelFactory"/>, and only their answers survive.
/// </summary>
public sealed record ToolModel(
    string Name,
    string TypeName,
    string Namespace,
    string Title,
    string Description,
    string Usage,
    EquatableArray<string> Aliases,
    EquatableArray<string> Categories,
    EquatableArray<string> Keywords,
    EquatableArray<string> ErrorCodes,
    TypeModel Args,
    TypeModel Result) : IEquatable<ToolModel>
{
    /// <summary>The English display name, falling back to the tool's name when none was authored.</summary>
    public string DisplayTitle => string.IsNullOrEmpty(Title) ? Name : Title;

    /// <summary>
    /// What a model calling the tool through MCP should read: the description plus the usage note,
    /// when the author wrote one. Only MCP pays for the extra tokens.
    /// </summary>
    public string ModelDescription
        => string.IsNullOrEmpty(Usage) ? Description : Description + "\n\n" + Usage;

    /// <summary>Every enum reachable from the arguments or the result, each listed once.</summary>
    public EquatableArray<EnumModel> Enums
        => Args.Enums
            .Concat(Result.Enums)
            .GroupBy(it => it.Type)
            .Select(it => it.First())
            .ToEquatableArray();
}

/// <param name="Type">Fully-qualified type name, as written in generated code.</param>
/// <param name="SimpleName">Trailing segment of <paramref name="Type"/>, e.g. "Args".</param>
/// <param name="HasPositionalConstructor">
/// True when a public constructor takes exactly one parameter per public property, i.e. the type is
/// a positional record and has to be built with constructor syntax rather than an object initializer.
/// </param>
public sealed record TypeModel(
    string Type,
    string SimpleName,
    bool HasPositionalConstructor,
    EquatableArray<PropertyModel> Properties,
    EquatableArray<EnumModel> Enums) : IEquatable<TypeModel>;

public sealed record PropertyModel(
    string Name,
    string Type,
    bool IsRequired,
    bool IsNullable,
    bool IsPipeInput,
    bool IsPipeOutput,
    string Description,
    string DisplayName,
    string? DefaultValue) : IEquatable<PropertyModel>
{
    /// <summary>
    /// The type with every nullable annotation stripped, for the places that need a bare
    /// <c>typeof</c> - which rejects nullable reference types outright.
    /// </summary>
    public string NonNullableType => Type.Replace("?", string.Empty);
}

public sealed record EnumModel(
    string Type,
    string SimpleName,
    EquatableArray<EnumValueModel> Values) : IEquatable<EnumModel>;

public sealed record EnumValueModel(string Name, string Description) : IEquatable<EnumValueModel>;

/// <summary>
/// The outcome of reading one <c>[ToolDefinition]</c> class: either a model, or the reason it could
/// not be built. The reason used to be a silent <c>null</c> - the tool simply vanished from the CLI,
/// the API and MCP with nothing written anywhere - so it is carried through the pipeline and
/// reported as a diagnostic once a <c>SourceProductionContext</c> exists.
/// </summary>
public sealed record ToolExtraction(string TypeName, ToolModel? Model, string? SkipReason)
    : IEquatable<ToolExtraction>
{
    public static ToolExtraction Extracted(ToolModel model) => new(model.TypeName, model, null);

    public static ToolExtraction Skipped(string typeName, string reason) => new(typeName, null, reason);
}
