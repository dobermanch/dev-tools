using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IntegerBaseConverterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IntegerBaseConverterTool _tool = null!;
    private IntegerBaseConverterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _inputValue = string.Empty;
    private IntegerBaseConverterTool.BaseType _inputBase = IntegerBaseConverterTool.BaseType.Decimal;
    private IntegerBaseConverterTool.BaseType _targetBase = IntegerBaseConverterTool.BaseType.Binary;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<IntegerBaseConverterToolPage>();
        _tool = Context.ToolsProvider.GetTool<IntegerBaseConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<IntegerBaseConverterTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputValue))
        {
            _result = null;
            return;
        }

        var args = new IntegerBaseConverterTool.Args(_inputValue, _inputBase, _targetBase);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task OnInputBaseValueChangedAsync(IntegerBaseConverterTool.BaseType value)
    {
        _inputBase = value;
        await OnValueChangedAsync();
    }

    private async Task OnTargetBaseValueChangedAsync(IntegerBaseConverterTool.BaseType value)
    {
        _targetBase = value;
        await OnValueChangedAsync();
    }
}
