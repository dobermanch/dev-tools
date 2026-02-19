using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TextDecryptorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TextDecryptorTool _tool = null!;
    private readonly Args _args = new();
    private TextDecryptorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject]
    private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<TextDecryptorToolPage>();
        _tool = Context.ToolsProvider.GetTool<TextDecryptorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TextDecryptorTool>();

        base.OnInitialized();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_args.EncryptedText))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(
            new TextDecryptorTool.Args(_args.EncryptedText, _args.Key ?? string.Empty, _args.Algorithm, _args.InputFormat),
            CancellationToken.None);
    }

    private string GetDecryptedText() => _result?.DecryptedText ?? string.Empty;

    private Task OnEncryptedTextChangedAsync(string value)
    {
        _args.EncryptedText = value;
        return OnValueChangedAsync();
    }

    private Task OnKeyChangedAsync(string value)
    {
        _args.Key = value;
        return OnValueChangedAsync();
    }

    private Task OnAlgorithmChangedAsync(TextDecryptorTool.EncryptionAlgorithm value)
    {
        _args.Algorithm = value;
        return OnValueChangedAsync();
    }

    private Task OnInputFormatChangedAsync(TextDecryptorTool.InputFormat value)
    {
        _args.InputFormat = value;
        return OnValueChangedAsync();
    }

    record Args
    {
        public string? EncryptedText { get; set; }
        public string? Key           { get; set; }
        public TextDecryptorTool.EncryptionAlgorithm Algorithm   { get; set; } = TextDecryptorTool.EncryptionAlgorithm.Aes256Cbc;
        public TextDecryptorTool.InputFormat         InputFormat { get; set; } = TextDecryptorTool.InputFormat.Base64;
    }
}
