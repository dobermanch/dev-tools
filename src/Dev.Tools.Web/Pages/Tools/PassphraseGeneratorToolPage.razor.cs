using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class PassphraseGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private PassphraseGeneratorTool _tool = null!;
    private readonly PassphraseGeneratorTool.Args _args = new();
    private PassphraseGeneratorTool.Result? _result;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<PassphraseGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<PassphraseGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }

    private string GetPassphrase()
    {
        return _result != null ? string.Join(Environment.NewLine, _result.Phrases) : string.Empty;
    }

    private Task OnWordCountValueChangedAsync(int count)
    {
        _args.WordCount = count;
        return OnValueChangedAsync();
    }
    
    private Task OnPhraseCountValueChangedAsync(int count)
    {
        _args.PhraseCount = count;
        return OnValueChangedAsync();
    }
    
    private Task OnCapitalValueChangedAsync(bool value)
    {
        _args.Capitalize = value;
        return OnValueChangedAsync();
    }
}