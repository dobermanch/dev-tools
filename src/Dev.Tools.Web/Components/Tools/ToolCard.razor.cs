using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class ToolCard : ComponentBase
{
    private bool _isFavorite;

    [Inject]
    private WebContext Context { get; set; } = null!;
    
    [Parameter]
    public ToolDefinition Tool { get; set; }

    protected override void OnInitialized()
    {
        _isFavorite = Context.Preferences.Preferences.Favorite.Tools.Contains(Tool.Name);
        base.OnInitialized();
    }

    private void NavigateToToolPage()
    {
        Context.Navigation.NavigateTo($"/tools/{Tool.Name}");
    }

    private async Task OnToggleFavoriteAsync()
    {
        if (!Context.Preferences.Preferences.Favorite.Tools.Add(Tool.Name))
        {
            Context.Preferences.Preferences.Favorite.Tools.Remove(Tool.Name);
        }
        
        await Context.Preferences.UpdateFavoriteAsync(Context.Preferences.Preferences.Favorite, CancellationToken.None);
   
        _isFavorite = Context.Preferences.Preferences.Favorite.Tools.Contains(Tool.Name);
    }
}