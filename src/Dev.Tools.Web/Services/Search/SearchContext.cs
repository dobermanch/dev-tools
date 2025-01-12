namespace Dev.Tools.Web.Services.Search;

public record SearchContext(string? Query, CancellationToken AbandonedToken);