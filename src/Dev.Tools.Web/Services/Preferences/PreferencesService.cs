using Dev.Tools.Web.Core.Storage;

namespace Dev.Tools.Web.Services.Preferences;

internal sealed class PreferencesService(IStorageProvider storageProvider) : IPreferencesService
{
    private const string PreferenceKey = "userPreferences";
    private UserPreferences _userPreferences = new();

    public UserPreferences Preferences => _userPreferences;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var data = await storageProvider.GetItemAsync<UserPreferences>(PreferenceKey, cancellationToken);
        if (data is not null)
        {
            Interlocked.Exchange(ref _userPreferences, data);
        }
    }

    public async Task UpdatePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        
        Interlocked.Exchange(ref _userPreferences, preferences);
        await storageProvider.SetItemAsync(PreferenceKey, preferences, cancellationToken);
    }

    public async Task UpdateLayoutAsync(UserPreferences.LayoutSettings settings, CancellationToken cancellationToken)
    {
        var preferences = Preferences with
        {
            Layout = settings
        };
        
        await UpdatePreferencesAsync(preferences, cancellationToken);
    }

    public async Task UpdateFavoriteAsync(UserPreferences.FavoriteDetails favorites, CancellationToken cancellationToken)
    {
        var preferences = Preferences with
        {
            Favorite = favorites
        };
        
        await UpdatePreferencesAsync(preferences, cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken)
    {
        return UpdatePreferencesAsync(new UserPreferences(), cancellationToken);
    }
}