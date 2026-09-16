using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Tools;

public partial class FavoriteButton : ComponentBase, IDisposable
{
    private bool _isFavorite;
    private IDisposable? _subscription;
    private IStringLocalizer _localizer = null!;

    [Inject]
    private WebContext Context { get; set; } = null!;
    
    [Parameter]
    public string Class { get; set; } = null!;

    [Parameter]
    public ToolDefinition ToolDefinition { get; set; } = null!;

    /// <summary>In a bordered square, as on a tool page header.</summary>
    [Parameter]
    public bool Bordered { get; set; }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Component.FavoriteButton");
        _subscription?.Dispose();
        _subscription = Context.Messenger.Subscribe<FavoritesChangedNotification>(OnFavoriteChanged);
        _isFavorite = Context.Preferences.Preferences.Favorite.Tools.Contains(ToolDefinition.Name);
        base.OnInitialized();
    }

    private void OnFavoriteChanged(FavoritesChangedNotification message)
    {
        if (message.Name.Equals(ToolDefinition.Name))
        {
            _isFavorite = Context.Preferences.Preferences.Favorite.Tools.Contains(ToolDefinition.Name);
        }
    }

    private async Task OnToggleFavoriteAsync()
    {
        if (!Context.Preferences.Preferences.Favorite.Tools.Add(ToolDefinition.Name))
        {
            Context.Preferences.Preferences.Favorite.Tools.Remove(ToolDefinition.Name);
        }

        _isFavorite = Context.Preferences.Preferences.Favorite.Tools.Contains(ToolDefinition.Name);

        await Context.Preferences.UpdateFavoriteAsync(Context.Preferences.Preferences.Favorite, CancellationToken.None);
        
        await Context.Messenger.Publish(new FavoritesChangedNotification(ToolDefinition.Name));
    }
}