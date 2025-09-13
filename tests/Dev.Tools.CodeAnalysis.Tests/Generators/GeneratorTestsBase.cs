using System.Runtime.CompilerServices;
using Dev.Tools.CodeAnalysis.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public abstract class GeneratorTestsBase
{
    [ModuleInitializer]
    public static void Init()
    {
        CodeBlock.SystemTime = new DateTime(2025, 1, 1);
        VerifySourceGenerators.Initialize();
    }

    protected Task Verify<T>(SyntaxTree source, MetadataReference? reference = null)
        where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation("Tests", [source], null, reference);

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new T());

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver).UseDirectory($"../Snapshots/{GetType().Name}");
    }

    protected CSharpCompilation CreateCompilation(string assemblyName, IEnumerable<SyntaxTree> sources,
        CSharpCompilationOptions? options = null,
        MetadataReference? reference = null)
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        if (reference is not null)
        {
            references.Add(reference);
        }

        // Create a Roslyn compilation for the syntax tree.
        return CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: sources,
            references: references,
            options: options);
    }

    protected byte[] GenerateAssembly(string assemblyName, IEnumerable<SyntaxTree> sources)
    {
        CSharpCompilation compilation =
            CreateCompilation(
                assemblyName,
                sources,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (result.Success)
        {
            return ms.ToArray();
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine(diagnostic.ToString());
        }

        throw new InvalidOperationException("Compilation failed");
    }

    protected MetadataReference GetMetadataReference(string assemblyName, IEnumerable<SyntaxTree> sources)
    {
        var bytes = GenerateAssembly(assemblyName, sources);
        return MetadataReference.CreateFromImage(bytes, filePath: assemblyName);
    }
}