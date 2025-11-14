using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class SafeUrlTranscoderToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private SafeUrlTranscoderTool _tool = null!;
    private SafeUrlTranscoderTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _url = string.Empty;
    private SafeUrlTranscoderTool.TranscodingType _transcoding = SafeUrlTranscoderTool.TranscodingType.Encode;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<SafeUrlTranscoderToolPage>();
        _tool = Context.ToolsProvider.GetTool<SafeUrlTranscoderTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<SafeUrlTranscoderTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_url))
        {
            _result = null;
            return;
        }

        var args = new SafeUrlTranscoderTool.Args(_url, _transcoding);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnTranscodingValueChangedAsync(SafeUrlTranscoderTool.TranscodingType value)
    {
        _transcoding = value;
        await OnValueChangedAsync();
    }
}
