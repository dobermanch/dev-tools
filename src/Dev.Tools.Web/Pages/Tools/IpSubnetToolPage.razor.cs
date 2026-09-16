using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IpSubnetToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IpSubnetTool _tool = null!;
    private IpSubnetTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _input = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? InputError => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    // What copy and download act on: the whole subnet at once, labelled.
    private string AllValues => _result is null or { HasErrors: true }
        ? string.Empty
        : string.Join(Environment.NewLine, new[]
        {
            $"{_localizer["TextField.Network.Label"]}: {_result.Network}",
            $"{_localizer["TextField.NetworkAddress.Label"]}: {_result.NetworkAddress}",
            $"{_localizer["TextField.SubnetMask.Label"]}: {_result.SubnetMask}",
            $"{_localizer["TextField.PrefixLength.Label"]}: /{_result.PrefixLength}",
            $"{_localizer["TextField.WildcardMask.Label"]}: {_result.WildcardMask}",
            $"{_localizer["TextField.AddressCount.Label"]}: {_result.AddressCount}",
            $"{_localizer["TextField.FirstHost.Label"]}: {_result.FirstHost}",
            $"{_localizer["TextField.LastHost.Label"]}: {_result.LastHost}",
            $"{_localizer["TextField.HostCount.Label"]}: {_result.HostCount}",
            _result.BroadcastAddress is null ? null : $"{_localizer["TextField.BroadcastAddress.Label"]}: {_result.BroadcastAddress}",
            _result.IpClass is null ? null : $"{_localizer["TextField.IpClass.Label"]}: {_result.IpClass}",
            _result.Blocks.Count > 1 ? $"{_localizer["TextField.Blocks.Label"]}: {string.Join(", ", _result.Blocks)}" : null
        }.Where(line => line is not null));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<IpSubnetToolPage>();
        _tool = Context.ToolsProvider.GetTool<IpSubnetTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IpSubnetTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_input))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(new IpSubnetTool.Args(_input), CancellationToken.None);
    }
}
