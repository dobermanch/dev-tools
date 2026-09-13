using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IpConverterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IpConverterTool _tool = null!;
    private IpConverterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _input = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? InputError => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<IpConverterToolPage>();
        _tool = Context.ToolsProvider.GetTool<IpConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IpConverterTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_input))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(new IpConverterTool.Args(_input), CancellationToken.None);
    }
}
