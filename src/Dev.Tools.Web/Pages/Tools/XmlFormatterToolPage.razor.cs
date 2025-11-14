using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class XmlFormatterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private XmlFormatterTool _tool = null!;
    private XmlFormatterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _xml = string.Empty;
    private int _indentSize = 2;
    private XmlFormatterTool.SortDirection _sortKeys = XmlFormatterTool.SortDirection.None;
    private XmlFormatterTool.TextCase _keyFormat = XmlFormatterTool.TextCase.None;
    private bool _excludeEmpty = false;
    private bool _compact = false;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<XmlFormatterToolPage>();
        _tool = Context.ToolsProvider.GetTool<XmlFormatterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<XmlFormatterTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_xml))
        {
            _result = null;
            return;
        }

        var args = new XmlFormatterTool.Args
        {
            Xml = _xml,
            IndentSize = _indentSize,
            SortKeys = _sortKeys,
            KeyFormat = _keyFormat,
            ExcludeEmpty = _excludeEmpty,
            Compact = _compact
        };
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnIndentSizeChangedAsync(int value)
    {
        _indentSize = value;
        await OnValueChangedAsync();
    }

    private async Task OnSortKeysChangedAsync(XmlFormatterTool.SortDirection value)
    {
        _sortKeys = value;
        await OnValueChangedAsync();
    }

    private async Task OnKeyFormatChangedAsync(XmlFormatterTool.TextCase value)
    {
        _keyFormat = value;
        await OnValueChangedAsync();
    }

    private async Task OnExcludeEmptyChangedAsync(bool value)
    {
        _excludeEmpty = value;
        await OnValueChangedAsync();
    }

    private async Task OnCompactChangedAsync(bool value)
    {
        _compact = value;
        await OnValueChangedAsync();
    }
}
