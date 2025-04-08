using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace VerifyXunit;

public static class VerifyExtensions
{
    private static readonly string ProjectName = Assembly.GetAssembly(typeof(VerifyExtensions))!.GetName().Name!;
    private static readonly ConcurrentDictionary<string, string> PathMap = new();
    
    public static SettingsTask UseSnapshotFolder(this SettingsTask settingsTask, [CallerFilePath] string callerFilePath = "")
    {
        var path = PathMap.GetOrAdd(callerFilePath, filePath =>
        {
            var directory = Path.GetDirectoryName(filePath)!;
            while (!string.IsNullOrEmpty(directory) && !directory.EndsWith(ProjectName))
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            return Path.Combine(directory!, "Snapshots", Path.GetFileNameWithoutExtension(filePath));    
        });

        return settingsTask.UseDirectory(path);
    }
}