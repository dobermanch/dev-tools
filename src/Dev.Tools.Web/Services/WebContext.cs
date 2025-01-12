using Dev.Tools.Providers;
using Dev.Tools.Web.Services.Localization;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dev.Tools.Web.Services;

public class WebContext(
    NavigationManager navigation,
    IToolsProvider toolsProvider,
    IMessenger messenger,
    IJsServices jsService,
    ILocalizationProvider localizationProvider,
    IPreferencesService preferences
)
{
    public NavigationManager Navigation { get; } = navigation;
    public IToolsProvider ToolsProvider { get; } = toolsProvider;
    public IMessenger Messenger { get; } = messenger;
    public IJsServices JsService { get; } = jsService;
    public ILocalizationProvider LocalizationProvider { get; } = localizationProvider;
    public IPreferencesService Preferences { get; } = preferences;
}