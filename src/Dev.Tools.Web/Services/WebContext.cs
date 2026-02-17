using System.Globalization;
using System.Reflection;
using Dev.Tools.Providers;
using Dev.Tools.Web.Services.JavaScript;
using Dev.Tools.Web.Services.Layout;
using Dev.Tools.Web.Services.Preferences;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Services;

public class WebContext(
    NavigationManager navigation,
    IToolsProvider toolsProvider,
    IMessenger messenger,
    IJsServices jsService,
    ILocalizationProvider localization,
    IPreferencesService preferences,
    ILayoutService layout
)
{
    public AppInfo AppDetails { get; } = new();

    public NavigationManager Navigation { get; } = navigation;

    public IToolsProvider ToolsProvider { get; } = toolsProvider;

    public IMessenger Messenger { get; } = messenger;

    public IJsServices JsService { get; } = jsService;

    public ILocalizationProvider Localization { get; } = localization;

    public IPreferencesService Preferences { get; } = preferences;

    public ILayoutService Layout { get; } = layout;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Preferences.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Set culture before RunAsync() so the WASM runtime loads the correct satellite
        // assemblies for resource localization. Fall back to the provider's default culture
        // (en-US) when no preference is saved, to ensure a supported culture is always set.
        var cultureName = Preferences.Preferences.Localization.Culture ?? Localization.CurrentCulture.Name;
        await Localization
            .SetCurrentCultureInfo(new CultureInfo(cultureName), false, cancellationToken)
            .ConfigureAwait(false);
    }

    public record AppInfo
    {
        public string Name => "Dev Tools";

        public string? Version
        {
            get
            {
                if (field != null)
                {
                    return field;
                }

                var asm = typeof(AppInfo).Assembly;

                var fileVersion = asm
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                    .Version;

                field = fileVersion ?? asm.GetName().Version?.ToString() ?? string.Empty;

                return field;
            }
        }
    }
}