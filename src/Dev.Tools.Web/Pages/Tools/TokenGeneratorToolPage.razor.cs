using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TokenGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TokenGeneratorTool _tool = null!;
    private readonly Args _args = new();
    private TokenGeneratorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject] private WebContext Context { get; set; } = null!;
    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<TokenGeneratorToolPage>();
        _tool = Context.ToolsProvider.GetTool<TokenGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TokenGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new TokenGeneratorTool.Args(
                _args.TokenLength,
                _args.Lowercase,
                _args.Numbers,
                _args.Uppercase,
                _args.Symbols,
                _args.TokenCount,
                _args.ExcludeSymbols,
                _args.Alphabet),
            CancellationToken.None);
    }

    private string GetTokens()
    {
        return _result != null ? string.Join(Environment.NewLine, _result.Tokens) : string.Empty;
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
    
    record Args
    {
        public int TokenLength { get; set; } = 15;
        public bool Lowercase { get; set; } = true;
        public bool Numbers { get; set; } = true;
        public bool Uppercase { get; set; } = true;
        public bool Symbols { get; set; } = true;
        public int TokenCount { get; set; } = 1;
        public string? ExcludeSymbols { get; set; }
        public string? Alphabet { get; set; }
    }
}