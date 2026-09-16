using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtOutputPane : ComponentBase
{
    private IStringLocalizer _localizer = null!;
    private bool _copied;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = null!;

    /// <summary>The result as text. Also what copy and download act on, whatever the body shows.</summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>A body of the page's own - labelled fields, an image, a diff - instead of the text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Given by the tools that generate: re-runs them with the same options.</summary>
    [Parameter]
    public EventCallback OnRefresh { get; set; }

    /// <summary>What a download is called. The extension says what the result is.</summary>
    [Parameter]
    public string DownloadFileName { get; set; } = "output.txt";

    /// <summary>The footer line. Defaults to a plain "ok"; an error from the tool replaces it.</summary>
    [Parameter]
    public string? Status { get; set; }

    [Parameter]
    public bool StatusIsError { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected override void OnInitialized()
    {
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

        // The icon becomes a tick for a moment: enough to show it worked, without a toast to dismiss.
        _copied = true;
        StateHasChanged();
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        _copied = false;
    }

    private async Task DownloadAsync()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            await Context.JsService.DownloadAsync(DownloadFileName, Text);
        }
    }
}
