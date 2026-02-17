using Dev.Tools.Web.Services.Layout;

namespace Dev.Tools.Web.Services.Preferences;

public sealed record UserPreferences
{
    public LayoutSettings Layout { get; init; } = new();
    public FavoriteDetails Favorite { get; init; } = new();
    
    public LocalizationSettings Localization { get; init; } = new();

    public sealed record LayoutSettings
    {
        public ThemeMode? ThemeMode { get; init; }
        public bool? IsDrawerOpen { get; init; }
        public ViewMode ViewMode { get; init; } = ViewMode.Cards;
    }

    public sealed record FavoriteDetails
    {
        public HashSet<string> Tools { get; init; } = [];
    }

    public sealed record LocalizationSettings
    {
        public string? Culture { get; init; }
    }
}
