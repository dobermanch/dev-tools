using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IntegerToRomanConverterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IntegerToRomanConverterTool _tool = null!;
    private IntegerToRomanConverterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _number = string.Empty;
    private IntegerToRomanConverterTool.TranscodingType _transcoding = IntegerToRomanConverterTool.TranscodingType.Encode;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<IntegerToRomanConverterToolPage>();
        _tool = Context.ToolsProvider.GetTool<IntegerToRomanConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IntegerToRomanConverterTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_number))
        {
            _result = null;
            return;
        }

        var args = new IntegerToRomanConverterTool.Args(_number, _transcoding);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnTranscodingValueChangedAsync(IntegerToRomanConverterTool.TranscodingType value)
    {
        _transcoding = value;
        await OnValueChangedAsync();
    }
}
