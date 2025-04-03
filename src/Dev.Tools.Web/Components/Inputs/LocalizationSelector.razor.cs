using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class LocalizationSelector : ComponentBase
{
    [Inject]
    WebContext Context { get; set; } = null!;

    private async Task OnLanguageChangedAsync(CultureInfo culture)
    {
        await Context.Localization.SetCurrentCultureInfo(culture, CancellationToken.None);
    }
}