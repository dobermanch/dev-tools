using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class StringToNatoAlphabetToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private StringToNatoAlphabetTool _tool = null!;
    private StringToNatoAlphabetTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<StringToNatoAlphabetToolPage>();
        _tool = Context.ToolsProvider.GetTool<StringToNatoAlphabetTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<StringToNatoAlphabetTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            _result = null;
            return;
        }

        var args = new StringToNatoAlphabetTool.Args(_text);
        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private string GetNatoWords()
    {
        if (_result?.Words == null || _result.Words.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(" ", _result.Words);
    }
}
