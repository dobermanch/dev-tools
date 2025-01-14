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

    public async Task UpdateLayoutAsync(UserPreferences.LayoutSettings settings, CancellationToken cancellationToken)
    {
        var preferences = Preferences with
        {
            Layout = settings
        };
        
        await SaveAsync(preferences, cancellationToken);
    }
    
    public async Task UpdateFavoriteAsync(UserPreferences.FavoriteDetails favorites, CancellationToken cancellationToken)
    {
        var preferences = Preferences with
        {
            Favorite = favorites
        };
        
        await SaveAsync(preferences, cancellationToken);
    }

    public Task ResetAsync(CancellationToken cancellationToken)
    {
        return SaveAsync(new UserPreferences(), cancellationToken);
    }

    private async Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _userPreferences, preferences);
        await storageProvider.SetItemAsync(PreferenceKey, _userPreferences, cancellationToken);
    }
}