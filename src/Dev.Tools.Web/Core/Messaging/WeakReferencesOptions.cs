namespace Dev.Tools.Web.Core.Messaging;

public record WeakReferencesOptions
{
    public static readonly string SectionName = "WeakReferences";
    
    public TimeSpan CleanupPeriod { get; init; } = TimeSpan.FromSeconds(30);
}