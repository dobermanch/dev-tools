using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dev.Tools.CodeAnalysis.Tests.Fixtures;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dev.Tools.CodeAnalysis.Tests.Generators;

public abstract class GeneratorTestsBase
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }

    /// <summary>
    /// The stand-in Dev.Tools library, compiled once and handed to the frontend generators the way
    /// the real one reaches them: as a metadata reference, not as source.
    /// </summary>
    private static readonly Lazy<MetadataReference> s_toolsAssembly = new(() =>
    {
        var source = CSharpSyntaxTree.ParseText(SampleTools.Source);
        var compilation = CSharpCompilation.Create(
            assemblyName: Dev.Tools.CodeAnalysis.CodeDefinitions.ToolsAssemblyName,
            syntaxTrees: [source],
            references: PlatformReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                "The sample tools assembly does not compile:\n"
                + string.Join("\n", result.Diagnostics.Where(it => it.Severity == DiagnosticSeverity.Error)));
        }

        return MetadataReference.CreateFromImage(
            stream.ToArray(), filePath: Dev.Tools.CodeAnalysis.CodeDefinitions.ToolsAssemblyName);
    });

    protected static MetadataReference ToolsAssembly => s_toolsAssembly.Value;

    /// <summary>
    /// The assemblies a tool's source actually needs. The test compilations used to reference only
    /// System.Private.CoreLib, so <c>DescriptionAttribute</c> and friends were unresolved and the
    /// generators read a model full of error types. Loaded once: every compilation in the run shares
    /// the same references rather than re-reading the files.
    /// </summary>
    private static readonly Lazy<MetadataReference[]> s_platformReferences = new(() =>
    {
        var directory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        return new[]
            {
                "System.Private.CoreLib.dll",
                "System.Runtime.dll",
                "netstandard.dll",
                "System.Collections.dll",
                "System.ComponentModel.Primitives.dll",
                "System.Linq.dll",
                "System.Threading.Tasks.dll"
            }
            .Select(it => (MetadataReference)MetadataReference.CreateFromFile(Path.Combine(directory, it)))
            .ToArray();
    });

    private static IEnumerable<MetadataReference> PlatformReferences => s_platformReferences.Value;

    /// <summary>
    /// The consuming assembly of a frontend generator: it declares the attribute that switches the
    /// generator on, and applies it.
    /// </summary>
    protected static SyntaxTree ConsumerAssembly(string attributeNamespace, string attributeName)
        => CSharpSyntaxTree.ParseText(
            // Assembly attributes have to precede every namespace and type declaration; placed after
            // them the attribute does not bind, and the generator reads as switched off.
            $$"""
              using System;

              [assembly: {{attributeNamespace}}.{{attributeName}}]

              namespace {{attributeNamespace}}
              {
                  [AttributeUsage(AttributeTargets.Assembly)]
                  internal sealed class {{attributeName}} : Attribute;
              }
              """);

    /// <summary>An assembly that references the tools but never asked for a frontend.</summary>
    protected static SyntaxTree EmptyAssembly => CSharpSyntaxTree.ParseText("// nothing to generate from");

    /// <summary>
    /// A frontend generator is switched on by an assembly attribute; without it, it has to stay
    /// silent rather than emit code the consumer never asked for.
    /// </summary>
    protected async Task VerifyNoOutput<T>(SyntaxTree source, MetadataReference? reference = null)
        where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation("Tests", [source], null, reference);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new T().AsSourceGenerator());
        var result = driver.RunGenerators(compilation).GetRunResult();

        await Assert.That(result.GeneratedTrees).IsEmpty();
        await Assert.That(result.Diagnostics).IsEmpty();
    }

    protected async Task Verify<T>(SyntaxTree source, MetadataReference? reference = null)
        where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation("Tests", [source], null, reference);
        
        ISourceGenerator generator = new T().AsSourceGenerator();
        
        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // A generator that emits nothing leaves Verify nothing to compare, and it passes. That hid a
        // fixture the frontends could not read at all, so an empty run is a failure here.
        await Assert.That(driver.GetRunResult().GeneratedTrees).IsNotEmpty()
            .Because($"{typeof(T).Name} generated no sources to snapshot");
        
        // Use verify to snapshot test the source generator output!
        await Verifier.Verify(driver).UseDirectory($"../Snapshots/{GetType().Name}");
    }

    // You call this method passing in C# sources, and the list of stages you expect
    // It runs the generator, asserts the outputs are ok, 
    protected async Task<(ImmutableArray<Diagnostic> Diagnostics, string[])> VerifyCaching<T, TTrackingType>(SyntaxTree source, MetadataReference? reference = null)
        where T : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation("Tests", [source], 
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary), 
            reference);

        var opts = new GeneratorDriverOptions(
            disabledOutputs: IncrementalGeneratorOutputKind.None,
            trackIncrementalGeneratorSteps: true);

        ISourceGenerator generator = new T().AsSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);

        var compilationClone = compilation.Clone();

        // Run the source generator!
        driver = driver.RunGenerators(compilation);
        GeneratorDriverRunResult runResult = driver.GetRunResult();

        // Run again, using the same driver, with a clone of the compilation
        GeneratorDriverRunResult runResult2 = driver
            .RunGenerators(compilationClone)
            .GetRunResult();
        
        var trackingNames = typeof(TTrackingType)
            .GetFields()
            .Where(fi => fi is { IsLiteral: true, IsInitOnly: false } && fi.FieldType == typeof(string))
            .Select(x => (string)x.GetRawConstantValue()!)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        // Compare all the tracked outputs, throw if there's a failure
        await AssertRunsEqual(runResult, runResult2, trackingNames);

        // verify the second run only generated cached source outputs
        var steps = runResult2.Results[0]
            .TrackedOutputSteps
            .SelectMany(x => x.Value) // step executions
            .SelectMany(x => x.Outputs); // execution results
        
        await Assert.That(steps).ContainsOnly(it => it.Reason == IncrementalStepRunReason.Cached);
        
        
        // Return the generator diagnostics and generated sources
        return (runResult.Diagnostics, runResult.GeneratedTrees.Select(x => x.ToString()).ToArray());
    }

    protected CSharpCompilation CreateCompilation(string assemblyName, IEnumerable<SyntaxTree> sources,
        CSharpCompilationOptions? options = null,
        MetadataReference? reference = null)
    {
        var references = PlatformReferences.ToList();

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
            System.Console.WriteLine(diagnostic.ToString());
        }

        throw new InvalidOperationException("Compilation failed");
    }

    protected MetadataReference GetMetadataReference(string assemblyName, IEnumerable<SyntaxTree> sources)
    {
        var bytes = GenerateAssembly(assemblyName, sources);
        return MetadataReference.CreateFromImage(bytes, filePath: assemblyName);
    }
    
    private static async Task AssertRunsEqual(
        GeneratorDriverRunResult runResult1,
        GeneratorDriverRunResult runResult2,
        string[] trackingNames)
    {
        // We're given all the tracking names, but not all the
        // stages will necessarily execute, so extract all the 
        // output steps, and filter to ones we know about
        var trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
        var trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

        // Both runs should have the same tracked steps
        await Assert.That(trackedSteps1.Keys)
            .IsNotEmpty()
            .And.Count().IsEqualTo(trackedSteps2.Count)
            .And.Contains(it => trackedSteps2.Keys.Contains(it));

        // Get the IncrementalGeneratorRunStep collection for each run
        foreach (var (trackingName, runSteps1) in trackedSteps1)
        {
            // Assert that both runs produced the same outputs
            var runSteps2 = trackedSteps2[trackingName];
            await AssertEqual(runSteps1, runSteps2, trackingName);
        }
    
        return;

        // Local function that extracts the tracked steps
        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
            GeneratorDriverRunResult runResult, string[] trackingNames)
            => runResult
                .Results[0] // We're only running a single generator, so this is safe
                .TrackedSteps // Get the pipeline outputs
                .Where(step => trackingNames.Contains(step.Key)) // filter to known steps
                .ToDictionary(x => x.Key, x => x.Value); // Convert to a dictionary
    }

    private static async Task AssertEqual(
        ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
        ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
        string stepName)
    {
        await Assert.That(runSteps1.Length).IsEqualTo(runSteps2.Length);

        for (var i = 0; i < runSteps1.Length; i++)
        {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            IEnumerable<object> outputs1 = runStep1.Outputs.Select(x => x.Value);
            IEnumerable<object> outputs2 = runStep2.Outputs.Select(x => x.Value);

            await Assert
                .That(outputs1)
                .IsEquivalentTo(outputs2)
                .Because($"because {stepName} should produce cacheable outputs");
            
            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the _input_ has changed, but the output hasn't
            // - Cached is when the the input has not changed, so the cached output is used 
            // This asserted on the first run, whose outputs are always New, so the test could only
            // ever fail - the likely reason it sat skipped.
            await Assert
                .That(runStep2.Outputs.ToArray())
                .ContainsOnly(it => it.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged)
                .Because($"{stepName} expected to have reason {IncrementalStepRunReason.Cached} or {IncrementalStepRunReason.Unchanged}");

            // Make sure we're not using anything we shouldn't
            await AssertObjectGraph(runStep1, stepName);
        }
    }
    
    private static async Task AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName)
    {
        // Including the stepName in error messages to make it easy to isolate issues
        var because = $"{stepName} shouldn't contain banned symbols";
        var visited = new HashSet<object>();

        // Check all of the outputs - probably overkill, but why not
        foreach (var (obj, _) in runStep.Outputs)
        {
            await Visit(obj);
        }

        async Task Visit(object? node)
        {
            // If we've already seen this object, or it's null, stop.
            if (node is null || !visited.Add(node))
            {
                return;
            }

            // Make sure it's not a banned type
            await Assert.That(node)
                .IsNotTypeOf<Compilation>().Because(because)
                .And.IsNotTypeOf<ISymbol>().Because(because)
                .And.IsNotTypeOf<SyntaxNode>().Because(because);
            
            // Examine the object
            Type type = node.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
            {
                return;
            }

            // If the object is a collection, check each of the values
            if (node is IEnumerable collection and not string)
            {
                foreach (object element in collection)
                {
                    // recursively check each element in the collection
                    await Visit(element);
                }

                return;
            }

            // Recursively check each field in the object
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                await Visit(field.GetValue(node));
            }
        }
    }
}