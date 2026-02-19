using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class JsonFormatterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private JsonFormatterTool _tool = null!;
    private JsonFormatterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _json = string.Empty;
    private int _indentSize = 2;
    private JsonFormatterTool.SortDirection _sortKeys = JsonFormatterTool.SortDirection.None;
    private JsonFormatterTool.TextCase _keyFormat = JsonFormatterTool.TextCase.None;
    private bool _excludeEmpty;
    private bool _compact;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<JsonFormatterToolPage>();
        _tool = Context.ToolsProvider.GetTool<JsonFormatterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<JsonFormatterTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_json))
        {
            _result = null;
            return;
        }

        var args = new JsonFormatterTool.Args(
            Json: _json,
            IndentSize: _indentSize,
            SortKeys: _sortKeys,
            KeyFormat: _keyFormat,
            ExcludeEmpty: _excludeEmpty,
            Compact: _compact
        );
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnIndentSizeChangedAsync(int value)
    {
        _indentSize = value;
        await OnValueChangedAsync();
    }

    private async Task OnSortKeysChangedAsync(JsonFormatterTool.SortDirection value)
    {
        _sortKeys = value;
        await OnValueChangedAsync();
    }

    private async Task OnKeyFormatChangedAsync(JsonFormatterTool.TextCase value)
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
