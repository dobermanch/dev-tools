using System.Globalization;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class LanguageSelector : ComponentBase
{
    /// <summary>The supported cultures as a list, which is what the select takes.</summary>
    private CultureInfo[] _cultures = [];

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected override void OnInitialized() => _cultures = [.. Context.Localization.SupportedCultures];

    private async Task OnCultureChangedAsync(CultureInfo culture) =>
        await Context.Localization.SetCurrentCultureInfo(culture, true, CancellationToken.None);
}
