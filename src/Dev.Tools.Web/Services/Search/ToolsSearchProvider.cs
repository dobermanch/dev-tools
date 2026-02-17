namespace Dev.Tools.Web.Services.Search;

internal sealed class ToolsSearchProvider(WebContext webContext) : ISearchProvider
{
    private ToolInfo[]? _tools;

    public ValueTask<SearchResult> SearchAsync(SearchContext context)
    {
        var query = context.Query?.ToLower();
        if (string.IsNullOrEmpty(query))
        {
            return new ValueTask<SearchResult>(new SearchResult(context, []));
        }
        
        var tools = GetTools();

        var results = tools
            .Where(it =>
                it.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || it.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .Select(it =>
                new SearchResult.Item(
                    it.Name,
                    it.Title,
                    it.Path,
                    1000,
                    new Dictionary<string, string>()
                    {
                        { nameof(ToolInfo.Description), it.Description }
                    }))
            .ToArray();

        return new ValueTask<SearchResult>(new SearchResult(context, results));
    }

    private IList<ToolInfo> GetTools()
    {
        return _tools ??= webContext.ToolsProvider
            .GetToolDefinitions()
            .Select(it => new ToolInfo(
                it.Name,
                $"/tools/{it.Name}",
                webContext.Localization.GetToolTitle(it),
                webContext.Localization.GetToolDescription(it)
            ))
            .ToArray();
    }

    private record ToolInfo(
        string Name,
        string Path,
        string Title,
        string Description
    );
}