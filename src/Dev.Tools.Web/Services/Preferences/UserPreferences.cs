using Dev.Tools.Web.Services.Layout;

namespace Dev.Tools.Web.Services.Preferences;

public sealed record UserPreferences
{
    public LayoutSettings Layout { get; init; } = new();

    public sealed record LayoutSettings
    {
        public ThemeMode? ThemeMode { get; init; }
        public bool? IsDrawerOpen { get; init; }
    }
}
