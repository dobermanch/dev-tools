using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtFieldValue : ComponentBase
{
    private IStringLocalizer _localizer = null!;
    private bool _copied;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected override void OnInitialized()
    {
        // The same two words the panes use, rather than a second pair saying the same thing.
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtPane");
        base.OnInitialized();
    }

    private async Task CopyAsync()
    {
        if (string.IsNullOrEmpty(Value))
        {
            return;
        }

        await Context.JsService.CopyToClipboardAsync(Value);

        _copied = true;
        StateHasChanged();
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        _copied = false;
    }
}
