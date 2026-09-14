using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class EncryptionToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private EncryptionTool _tool = null!;
    private EncryptionTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;
    private string _key = string.Empty;
    private EncryptionTool.EncryptionOperation _operation = EncryptionTool.EncryptionOperation.Encrypt;
    private EncryptionTool.EncryptionAlgorithm _algorithm = EncryptionTool.EncryptionAlgorithm.Aes256Cbc;
    private EncryptionTool.CiphertextFormat _format = EncryptionTool.CiphertextFormat.Base64;

    [Inject] private WebContext Context { get; set; } = null!;

    // A missing key is the key field's error; everything else - an empty text, a wrong format, a failure - the input's.
    private string? KeyError => ErrorWhere(it => it.Target == nameof(EncryptionTool.Args.Key));
    private string? InputError => ErrorWhere(it => it.Target != nameof(EncryptionTool.Args.Key));

    protected override void OnInitialized()
    {
        _localizer = Context.Localization.PageLocalizer<EncryptionToolPage>();
        _tool = Context.ToolsProvider.GetTool<EncryptionTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<EncryptionTool>();

        base.OnInitialized();
    }

    private Task OnTextChangedAsync(string value)
    {
        _text = value;
        return RunAsync();
    }

    private Task OnKeyChangedAsync(string value)
    {
        _key = value;
        return RunAsync();
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await RunAsync();
    }

    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(
            new EncryptionTool.Args(_text, _key, _operation, _algorithm, _format),
            CancellationToken.None);
    }

    private string? ErrorWhere(Func<ToolError, bool> predicate)
        => _result?.Errors
            .Where(predicate)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
