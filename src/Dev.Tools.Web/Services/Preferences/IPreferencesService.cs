namespace Dev.Tools.Web.Services.Preferences;

public interface IPreferencesService
{
    UserPreferences Preferences { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    Task ResetAsync(CancellationToken cancellationToken = default);

    Task UpdatePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken);
    
    Task UpdateLayoutAsync(UserPreferences.LayoutSettings settings, CancellationToken cancellationToken = default);
    
    Task UpdateFavoriteAsync(UserPreferences.FavoriteDetails favorites, CancellationToken cancellationToken);
}