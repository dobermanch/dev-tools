using Dev.Tools.Providers;

namespace Dev.Tools.Web.Services.Search;

internal sealed class ToolsSearchProvider : ISearchProvider
{
    private readonly IToolsProvider _provider;

    public ToolsSearchProvider(IToolsProvider provider)
    {
        _provider = provider;
    }
    
    public ValueTask<SearchResult> SearchAsync(SearchContext context)
    {
        throw new NotImplementedException();
    }
}