using ResxGenerator;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ResxGenerator <assembly-dll-path> <output-directory>");
    Console.WriteLine("  <assembly-dll-path>: Path to Dev.Tools.dll (used to find generated source files)");
    Console.WriteLine("  <output-directory>: Directory where RESX files will be generated");
    return 1;
}

string assemblyPath = args[0];
string outputDirectory = args[1];
if (!File.Exists(assemblyPath))
{
    Console.Error.WriteLine($"Error: Assembly not found: {assemblyPath}");
    return 1;
}

try
{
    Console.WriteLine($"Finding generated source files from: {assemblyPath}");
    var generator = new LocalizationResxGenerator(assemblyPath, outputDirectory);
    generator.Generate();

    Console.WriteLine("✓ RESX files generated successfully!");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}