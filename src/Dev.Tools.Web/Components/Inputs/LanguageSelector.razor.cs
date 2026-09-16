using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class LanguageSelector : ComponentBase
{
    private readonly string _id = $"dt-language-{Guid.NewGuid():N}";

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private async Task OnLanguageChangedAsync(ChangeEventArgs args)
    {
        if (args.Value is not string name)
        {
            return;
        }

        var culture = Context.Localization.SupportedCultures
            .FirstOrDefault(it => string.Equals(it.Name, name, StringComparison.OrdinalIgnoreCase));

        if (culture is not null)
        {
            await Context.Localization.SetCurrentCultureInfo(culture, true, CancellationToken.None);
        }
    }
}
