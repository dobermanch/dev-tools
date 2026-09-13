using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class HtmlEntitiesToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private HtmlEntitiesTool _tool = null!;
    private HtmlEntitiesTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;
    private HtmlEntitiesTool.TranscodingType _transcoding = HtmlEntitiesTool.TranscodingType.Encode;
    private bool _encodeNonAscii;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<HtmlEntitiesToolPage>();
        _tool = Context.ToolsProvider.GetTool<HtmlEntitiesTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<HtmlEntitiesTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrEmpty(_text))
        {
            _result = null;
            return;
        }

        var args = new HtmlEntitiesTool.Args(_text, _transcoding, _encodeNonAscii);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnTranscodingChangedAsync(HtmlEntitiesTool.TranscodingType value)
    {
        _transcoding = value;
        await OnValueChangedAsync();
    }

    private async Task OnEncodeNonAsciiChangedAsync(bool value)
    {
        _encodeNonAscii = value;
        await OnValueChangedAsync();
    }
}
