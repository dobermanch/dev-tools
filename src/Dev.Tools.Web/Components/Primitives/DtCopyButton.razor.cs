using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Primitives;

public partial class DtCopyButton : ComponentBase
{
    private IStringLocalizer _localizer = null!;
    private bool _copied;

    [Inject]
    private WebContext Context { get; set; } = null!;

    /// <summary>What lands on the clipboard. Nothing to copy disables the button.</summary>
    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public DtControlSize Size { get; set; } = DtControlSize.Small;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected override void OnInitialized()
    {
        // One pair of words for copying, wherever the button appears.
        _localizer = Context.Localization.CreateScopedLocalizer("Component.DtPane");
        base.OnInitialized();
    }

    private async Task CopyAsync()
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        await Context.JsService.CopyToClipboardAsync(Text);

        _copied = true;
        StateHasChanged();
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        _copied = false;
    }
}
