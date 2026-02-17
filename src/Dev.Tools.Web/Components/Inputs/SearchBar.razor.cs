using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Search;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Inputs;

public partial class SearchBar : ComponentBase
{
    private IStringLocalizer _localizer = null!;

    [Parameter] public string? Placeholder { get; set; }
    
    [Inject]
    ISearchProvider SearchProvider { get; set; } = null!;
    
    [Inject]
    WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.ComponentLocalizer<SearchBar>();
        base.OnInitialized();
    }

    private Task OnSearchItemSelectedAsync(SearchResult.Item? item)
    {
        if (item is not null)
        {
            Context.Navigation.NavigateTo(item.Link);
        }
        
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<SearchResult.Item>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        combinedTokenSource.CancelAfter(TimeSpan.FromMilliseconds(300)); 
        
        var result = await SearchProvider.SearchAsync(new SearchContext(query, combinedTokenSource.Token));
        
        return result.OrderBy(it => it.Rank).Take(10);
    }
}