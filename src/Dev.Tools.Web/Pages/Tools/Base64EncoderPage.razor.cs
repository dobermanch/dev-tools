using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class Base64EncoderPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private Base64EncoderTool _tool = null!;
    private readonly Args _args = new();
    private Base64EncoderTool.Result _result = new();
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<Base64EncoderPage>();
        _tool = Context.ToolsProvider.GetTool<Base64EncoderTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<Base64EncoderTool>();

        base.OnInitialized();
    }

    private async Task OnStringToEncodeValueChangedAsync(string value)
    {
        _args.Text = value;
        _result = await _tool.RunAsync(
            new Base64EncoderTool.Args(_args.Text, _args.InsertLineBreaks, _args.UrlSafe),
            CancellationToken.None);
    }

    private async Task OnUrlSafeValueChangedAsync(bool value)
    {
        _args.UrlSafe = value;
        await OnStringToEncodeValueChangedAsync(_args.Text!);
    }

    private async Task OnInsertLineBreaksValueChangedAsync(bool value)
    {
        _args.InsertLineBreaks = value;
        await OnStringToEncodeValueChangedAsync(_args.Text!);
    }

    record Args
    {
        public string? Text { get; set; }
        public bool InsertLineBreaks { get; set; }
        public bool UrlSafe { get; set; }
    }
}