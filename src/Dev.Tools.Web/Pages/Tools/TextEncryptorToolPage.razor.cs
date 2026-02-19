using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TextEncryptorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TextEncryptorTool _tool = null!;
    private readonly Args _args = new();
    private TextEncryptorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject]
    private WebContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<TextEncryptorToolPage>();
        _tool = Context.ToolsProvider.GetTool<TextEncryptorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TextEncryptorTool>();

        base.OnInitialized();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_args.Text))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(
            new TextEncryptorTool.Args(_args.Text, _args.Key ?? string.Empty, _args.Algorithm, _args.OutputFormat),
            CancellationToken.None);
    }

    private string GetEncryptedText() => _result?.EncryptedText ?? string.Empty;

    private Task OnTextChangedAsync(string value)
    {
        _args.Text = value;
        return OnValueChangedAsync();
    }

    private Task OnKeyChangedAsync(string value)
    {
        _args.Key = value;
        return OnValueChangedAsync();
    }

    private Task OnAlgorithmChangedAsync(TextEncryptorTool.EncryptionAlgorithm value)
    {
        _args.Algorithm = value;
        return OnValueChangedAsync();
    }

    private Task OnOutputFormatChangedAsync(TextEncryptorTool.OutputFormat value)
    {
        _args.OutputFormat = value;
        return OnValueChangedAsync();
    }

    record Args
    {
        public string? Text      { get; set; }
        public string? Key       { get; set; }
        public TextEncryptorTool.EncryptionAlgorithm Algorithm     { get; set; } = TextEncryptorTool.EncryptionAlgorithm.Aes256Cbc;
        public TextEncryptorTool.OutputFormat         OutputFormat { get; set; } = TextEncryptorTool.OutputFormat.Base64;
    }
}
