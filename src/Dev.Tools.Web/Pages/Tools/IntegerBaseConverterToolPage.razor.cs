using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class IntegerBaseConverterToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private IntegerBaseConverterTool _tool = null!;
    private IStringLocalizer _localizer = null!;

    private string _inputValue = string.Empty;
    private IntegerBaseConverterTool.BaseType _inputBase = IntegerBaseConverterTool.BaseType.Decimal;

    private readonly Dictionary<IntegerBaseConverterTool.BaseType, IntegerBaseConverterTool.Result?> _results =
        Enum
            .GetValues<IntegerBaseConverterTool.BaseType>()
            .ToDictionary(it => it, _ => (IntegerBaseConverterTool.Result?)null);


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
            foreach (var baseType in _results.Keys)
            {
                _results[baseType] = null;
            }
            return;
        }
        
        var tasks = new  List<Task>();
        foreach (var targetBaseType in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new IntegerBaseConverterTool.Args(
                        _inputValue, 
                        _inputBase, 
                        targetBaseType),
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[targetBaseType] = it.Result;
                }));
        }

        await Task.WhenAll(tasks);
    }

    private async Task OnInputBaseValueChangedAsync(IntegerBaseConverterTool.BaseType value)
    {
        _inputBase = value;
        await OnValueChangedAsync();
    }
}
