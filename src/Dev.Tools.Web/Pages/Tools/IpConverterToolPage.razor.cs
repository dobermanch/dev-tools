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

    // What copy and download act on: every representation at once, labelled.
    private string AllRepresentations => _result is null or { HasErrors: true }
        ? string.Empty
        : string.Join(Environment.NewLine, new[]
        {
            _result.Ipv4 is null ? null : $"{_localizer["TextField.Ipv4.Label"]}: {_result.Ipv4}",
            $"{_localizer["TextField.Decimal.Label"]}: {_result.Decimal}",
            $"{_localizer["TextField.Hexadecimal.Label"]}: {_result.Hexadecimal}",
            $"{_localizer["TextField.Binary.Label"]}: {_result.Binary}",
            $"{_localizer["TextField.Ipv6.Label"]}: {_result.Ipv6}",
            $"{_localizer["TextField.Ipv6Expanded.Label"]}: {_result.Ipv6Expanded}"
        }.Where(line => line is not null));

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
