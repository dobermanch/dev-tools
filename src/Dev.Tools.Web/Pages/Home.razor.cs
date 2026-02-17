using Dev.Tools.Localization;
using Dev.Tools.Providers;
using Dev.Tools.Web.Components.Tools;
using Dev.Tools.Web.Core.Messaging;
using Dev.Tools.Web.Notifications;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages;

public partial class Home : ComponentBase, IDisposable
{
    [Inject] private WebContext Context { get; set; } = default!;
    [Inject] private IToolsProvider ToolsProvider { get; set; } = default!;

    private IStringLocalizer _localizer = null!;
    private ViewMode _viewMode;
    private IReadOnlyList<ToolDefinition> _favoriteTools = [];
    private GroupedList<Category, ToolDefinition> _toolsByCategory = GroupedList<Category, ToolDefinition>.Empty;
    private int _toolCount;
    private int _categoryCount;
    private IDisposable? _subscription;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Home>();
        _viewMode = Context.Preferences.Preferences.Layout.ViewMode;
        _subscription = Context.Messenger.Subscribe<FavoritesChangedNotification>(OnFavoritesChanged);
        LoadTools();
        base.OnInitialized();
    }

    private void LoadTools()
    {
        var allTools = ToolsProvider.GetToolDefinitions();
        _toolCount = allTools.Count;

        var favoriteNames = Context.Preferences.Preferences.Favorite.Tools;
        _favoriteTools = allTools
            .Where(t => favoriteNames.Contains(t.Name))
            .ToList();

        var grouped = allTools
            .SelectMany(t => t.Categories.Select(c => (category: c, tool: t)))
            .GroupBy(x => x.category)
            .Select(g => new GroupedItem<Category, ToolDefinition>
            {
                Key = g.Key,
                Data = g.Select(x => x.tool).ToArray()
            })
            .ToList();

        _toolsByCategory = new GroupedList<Category, ToolDefinition>(grouped);
        _categoryCount = grouped.Count;
    }

    private async Task OnViewModeChangedAsync(ViewMode mode)
    {
        _viewMode = mode;
        var settings = Context.Preferences.Preferences.Layout with { ViewMode = mode };
        await Context.Preferences.UpdateLayoutAsync(settings);
    }

    private void OnFavoritesChanged(FavoritesChangedNotification _)
    {
        LoadTools();
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
