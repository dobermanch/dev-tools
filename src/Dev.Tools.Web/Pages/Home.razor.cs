using Dev.Tools.Localization;
using Dev.Tools.Providers;
using Dev.Tools.Web.Components.Primitives;
using Dev.Tools.Web.Components.Tools;
using Dev.Tools.Web.Core.Messaging;
using Dev.Tools.Web.Notifications;
using Dev.Tools.Web.Services;
using Dev.Tools.Web.Services.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Dev.Tools.Web.Pages;

public partial class Home : ComponentBase, IDisposable
{
    private static readonly IReadOnlyList<DtToggleGroup<ViewMode>.Option> ViewModeOptions =
    [
        new(ViewMode.Cards, Icons.Material.Outlined.Apps, "Card view"),
        new(ViewMode.List, Icons.Material.Outlined.ViewList, "List view")
    ];

    private static readonly IReadOnlyList<DtToggleGroup<bool>.Option> GroupingOptions =
    [
        new(true, Icons.Material.Outlined.Category, "Group by category"),
        new(false, Icons.Material.Outlined.List, "Show all")
    ];

    [Inject] private WebContext Context { get; set; } = default!;
    [Inject] private IToolsProvider ToolsProvider { get; set; } = default!;

    /// <summary>Set by the sidebar and the category chips, so a filtered index is a link.</summary>
    [SupplyParameterFromQuery(Name = "category")]
    private string? CategoryQuery { get; set; }

    /// <summary>"favorites" or "recent" narrows the index to those tools.</summary>
    [SupplyParameterFromQuery(Name = "view")]
    private string? ViewQuery { get; set; }

    private IStringLocalizer _localizer = null!;
    private ViewMode _viewMode;
    private bool _groupByCategory;
    private string _query = string.Empty;
    private Category? _category;
    private bool _favoritesOnly;
    private bool _recentOnly;

    private IReadOnlyList<ToolDefinition> _allTools = [];
    private IReadOnlyList<ToolDefinition> _visibleTools = [];
    private IReadOnlyList<Category> _categories = [];
    private Dictionary<Category, int> _categoryCounts = [];
    private GroupedList<Category, ToolDefinition> _visibleGroups = GroupedList<Category, ToolDefinition>.Empty;
    private int _toolCount;
    private int _categoryCount;

    private IDisposable? _subscription;
    private CancellationTokenSource? _debounce;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Home>();
        _viewMode = Context.Preferences.Preferences.Layout.ViewMode;
        _groupByCategory = Context.Preferences.Preferences.Layout.GroupByCategory;
        _subscription = Context.Messenger.Subscribe<FavoritesChangedNotification>(OnFavoritesChanged);
        LoadTools();
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        _category = Enum.TryParse<Category>(CategoryQuery, ignoreCase: true, out var category) ? category : null;
        _favoritesOnly = string.Equals(ViewQuery, "favorites", StringComparison.OrdinalIgnoreCase);
        _recentOnly = string.Equals(ViewQuery, "recent", StringComparison.OrdinalIgnoreCase);
        ApplyFilters();
        base.OnParametersSet();
    }

    private void LoadTools()
    {
        var allTools = ToolsProvider.GetToolDefinitions();
        _allTools = allTools as IReadOnlyList<ToolDefinition> ?? [.. allTools];
        _toolCount = _allTools.Count;

        _categoryCounts = _allTools
            .SelectMany(tool => tool.Categories)
            .GroupBy(category => category)
            .ToDictionary(group => group.Key, group => group.Count());

        _categories = [.. _categoryCounts.Keys.OrderBy(category => category)];
        _categoryCount = _categories.Count;
    }

    private void ApplyFilters()
    {
        IEnumerable<ToolDefinition> tools = _allTools;

        if (_favoritesOnly)
        {
            var favorites = Context.Preferences.Preferences.Favorite.Tools;
            tools = tools.Where(tool => favorites.Contains(tool.Name));
        }

        if (_recentOnly)
        {
            // The saved order is the answer here, not the catalogue's: most recently opened first.
            var recent = Context.Preferences.Preferences.Recent.Tools;
            tools = recent
                .Select(name => _allTools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.Ordinal)))
                .Where(tool => tool is not null)
                .Select(tool => tool!);
        }

        if (_category is { } category)
        {
            tools = tools.Where(tool => tool.Categories.Contains(category));
        }

        if (!string.IsNullOrWhiteSpace(_query))
        {
            var query = _query.Trim();
            tools = tools.Where(tool => Matches(tool, query));
        }

        _visibleTools = [.. tools];

        // A tool in two categories shows under both, and a category with nothing left does not show.
        var grouped = _visibleTools
            .SelectMany(tool => tool.Categories.Select(category => (category, tool)))
            .GroupBy(pair => pair.category)
            .OrderBy(group => group.Key)
            .Select(group => new GroupedItem<Category, ToolDefinition>
            {
                Key = group.Key,
                Data = [.. group.Select(pair => pair.tool)]
            })
            .ToList();

        _visibleGroups = new GroupedList<Category, ToolDefinition>(grouped);
    }

    /// <summary>Matches what someone would type: the name they see, the slug, or a keyword.</summary>
    private bool Matches(ToolDefinition tool, string query)
        => Context.Localization.GetToolTitle(tool).Contains(query, StringComparison.OrdinalIgnoreCase)
           || tool.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
           || tool.Keywords.Any(keyword =>
               keyword.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
               || Context.Localization.GetKeyword(keyword).Contains(query, StringComparison.OrdinalIgnoreCase));

    private string CountFor(Category category)
        => _categoryCounts.TryGetValue(category, out var count) ? count.ToString() : "0";

    private async Task OnQueryChangedAsync(ChangeEventArgs args)
    {
        _query = args.Value as string ?? string.Empty;

        var previous = _debounce;
        if (previous is not null)
        {
            await previous.CancelAsync();
            previous.Dispose();
        }

        var cancellation = new CancellationTokenSource();
        _debounce = cancellation;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        ApplyFilters();
        StateHasChanged();
    }

    private Task SelectCategoryAsync(Category? category)
    {
        // Through the URL, so the filter can be linked to and the back button undoes it.
        var uri = Context.Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["category"] = category?.ToString(),
            ["view"] = null
        });

        Context.Navigation.NavigateTo(uri);
        return Task.CompletedTask;
    }

    private async Task OnViewModeChangedAsync(ViewMode mode)
    {
        _viewMode = mode;
        var settings = Context.Preferences.Preferences.Layout with { ViewMode = mode };
        await Context.Preferences.UpdateLayoutAsync(settings);
    }

    private async Task OnGroupByCategoryChangedAsync(bool grouped)
    {
        _groupByCategory = grouped;
        var settings = Context.Preferences.Preferences.Layout with { GroupByCategory = grouped };
        await Context.Preferences.UpdateLayoutAsync(settings);
    }

    private void OnFavoritesChanged(FavoritesChangedNotification _)
    {
        ApplyFilters();
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _debounce?.Dispose();
    }
}
