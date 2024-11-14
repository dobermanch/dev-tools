namespace Dev.Tools.Web.Services.Search;

public interface ISearchProvider
{
    ValueTask<SearchResult> SearchAsync(SearchContext context);
}