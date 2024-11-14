using System.Collections;

namespace Dev.Tools.Web.Services.Search;

public sealed record SearchResult : IEnumerable<SearchResult.Item>
{
    private readonly IEnumerable<Item> _result;

    public SearchResult(SearchContext context, IEnumerable<Item> result)
    {
        _result = result;
        Context = context;
    }

    public SearchContext Context { get; }

    public IEnumerator<Item> GetEnumerator() => _result.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    
    public record Item(string Title, Uri Link, int Rank, IDictionary<string, string> Details);
}