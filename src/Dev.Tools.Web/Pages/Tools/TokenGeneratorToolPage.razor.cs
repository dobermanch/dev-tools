using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TokenGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition;
    private TokenGeneratorTool _tool = null!;
    private readonly TokenGeneratorTool.Args _args = new();
    private TokenGeneratorTool.Result? _result;

    [Inject] private WebContext Context { get; set; } = null!;
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<TokenGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TokenGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(_args, CancellationToken.None);
    }

    private string GetTokens()
    {
        return _result != null ? string.Join(Environment.NewLine, _result.Tokens) : string.Empty;
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await Context.JsService.CopyToClipboardAsync(textToCopy);
        }
    }

    private Task OnTokenCountValueChangedAsync(int count)
    {
        _args.TokenCount = count;
        return OnValueChangedAsync();
    }
    
    private Task OnTokenLengthValueChangedAsync(int count)
    {
        _args.TokenLength = count;
        return OnValueChangedAsync();
    }
    
    private Task OnLowercaseValueChangedAsync(bool value)
    {
        _args.Lowercase = value;
        return OnValueChangedAsync();
    }
    
    private Task OnUppercaseValueChangedAsync(bool value)
    {
        _args.Uppercase = value;
        return OnValueChangedAsync();
    }
    
    
    private Task OnNumbersValueChangedAsync(bool value)
    {
        _args.Numbers = value;
        return OnValueChangedAsync();
    }
    
    
    private Task OnSymbolsValueChangedAsync(bool value)
    {
        _args.Symbols = value;
        return OnValueChangedAsync();
    }
}