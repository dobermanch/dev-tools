using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class DataConverterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private DataConverterTool _tool = null!;
    private DataConverterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;
    private DataConverterTool.SourceFormat _from = DataConverterTool.SourceFormat.Json;
    private DataConverterTool.TargetFormat _to = DataConverterTool.TargetFormat.Yaml;

    [Inject] private WebContext Context { get; set; } = null!;

    // Input that does not parse is the input field's error; data the target format cannot hold is the result's.
    private string? InputError => ErrorFor(nameof(DataConverterTool.Args.Text));
    private string? OutputError => ErrorFor(nameof(DataConverterTool.Args.To));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<DataConverterToolPage>();
        _tool = Context.ToolsProvider.GetTool<DataConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<DataConverterTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            _result = null;
            return;
        }

        var args = new DataConverterTool.Args(_text, _from, _to);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnFromChangedAsync(DataConverterTool.SourceFormat value)
    {
        _from = value;
        await OnValueChangedAsync();
    }

    private async Task OnToChangedAsync(DataConverterTool.TargetFormat value)
    {
        _to = value;
        await OnValueChangedAsync();
    }

    private string? ErrorFor(string target)
        => _result?.Errors
            .Where(it => it.Target == target)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
