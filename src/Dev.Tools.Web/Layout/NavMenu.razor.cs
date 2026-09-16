using Dev.Tools.Providers;
using Dev.Tools.Web.Core.Messaging;
using Dev.Tools.Web.Notifications;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Layout;

public partial class NavMenu : ComponentBase, IDisposable
{
    private IStringLocalizer _localizer = null!;
    private IReadOnlyList<Category> _categories = [];
    private Dictionary<Category, int> _categoryCounts = [];
    private int _favoriteCount;
    private int _recentCount;
    private Category? _activeCategory;
    private bool _isFavoritesView;
    private bool _isRecentView;
    private bool _isIndex;
    private IDisposable? _subscription;
    private IDisposable? _recentSubscription;

    [Inject] private WebContext Context { get; set; } = null!;
    [Inject] private IToolsProvider ToolsProvider { get; set; } = null!;

    /// <summary>The rail: icons only, with the labels as tooltips.</summary>
    [Parameter]
    public bool Collapsed { get; set; }

    private bool IsAllToolsActive => _isIndex && _activeCategory is null && !_isFavoritesView;

    private bool IsFavoritesActive => _isIndex && _isFavoritesView;

    private bool IsRecentActive => _isIndex && _isRecentView;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.CreateScopedLocalizer("Layout.NavMenu");

        var tools = ToolsProvider.GetToolDefinitions();
        _categoryCounts = tools
            .SelectMany(tool => tool.Categories)
            .GroupBy(category => category)
            .ToDictionary(group => group.Key, group => group.Count());
        _categories = [.. _categoryCounts.Keys.OrderBy(category => category)];

        LoadCounts();
        _subscription = Context.Messenger.Subscribe<FavoritesChangedNotification>(OnFavoritesChanged);
        _recentSubscription = Context.Messenger.Subscribe<RecentChangedNotification>(OnRecentChanged);

        // The active row is read off the URL, so it follows the chips on the index as well as these links.
        Context.Navigation.LocationChanged += OnLocationChanged;
        ReadLocation();

        base.OnInitialized();
    }

    private void LoadCounts()
    {
        _favoriteCount = Context.Preferences.Preferences.Favorite.Tools.Count;
        _recentCount = Context.Preferences.Preferences.Recent.Tools.Count;
    }

    private string CountFor(Category category)
        => _categoryCounts.TryGetValue(category, out var count) ? count.ToString() : "0";

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        ReadLocation();
        InvokeAsync(StateHasChanged);
    }

    private void ReadLocation()
    {
        var uri = Context.Navigation.ToAbsoluteUri(Context.Navigation.Uri);
        var path = uri.AbsolutePath.TrimEnd('/');
        var basePath = new Uri(Context.Navigation.BaseUri).AbsolutePath.TrimEnd('/');

        _isIndex = string.Equals(path, basePath, StringComparison.OrdinalIgnoreCase);

        var query = QueryValues(uri.Query);
        _activeCategory = Enum.TryParse<Category>(query.GetValueOrDefault("category"), ignoreCase: true, out var category)
            ? category
            : null;
        var view = query.GetValueOrDefault("view");
        _isFavoritesView = string.Equals(view, "favorites", StringComparison.OrdinalIgnoreCase);
        _isRecentView = string.Equals(view, "recent", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Two parameters, both plain words: worth a few lines here rather than a dependency on a query
    /// parser the rest of the app does not use.
    /// </summary>
    private static Dictionary<string, string> QueryValues(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                values[Uri.UnescapeDataString(pair[..separator])] = Uri.UnescapeDataString(pair[(separator + 1)..]);
            }
        }

        return values;
    }

    private void OnFavoritesChanged(FavoritesChangedNotification _)
    {
        LoadCounts();
        InvokeAsync(StateHasChanged);
    }

    private void OnRecentChanged(RecentChangedNotification _)
    {
        LoadCounts();
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Context.Navigation.LocationChanged -= OnLocationChanged;
        _subscription?.Dispose();
        _recentSubscription?.Dispose();
    }
}
