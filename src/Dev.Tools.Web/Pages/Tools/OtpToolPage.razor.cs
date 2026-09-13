using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class OtpToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private OtpTool _tool = null!;
    private OtpTool.Result? _result;
    private IStringLocalizer _localizer = null!;
    private System.Timers.Timer? _timer;

    private string _secret = string.Empty;
    private OtpTool.OtpType _type = OtpTool.OtpType.Totp;
    private OtpTool.OtpAlgorithm _algorithm = OtpTool.OtpAlgorithm.Sha1;
    private int _digits = 6;
    private int _period = 30;
    private long _counter;
    private string _issuer = string.Empty;
    private string _accountName = string.Empty;
    private string _code = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    private string? SecretError => _result?.Errors
        .Where(it => it.Target == nameof(OtpTool.Args.Secret))
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<OtpToolPage>();
        _tool = Context.ToolsProvider.GetTool<OtpTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<OtpTool>();

        await GenerateSecretAsync();
        await base.OnInitializedAsync();

        // A TOTP code changes with the clock, so the page follows it.
        _timer = new System.Timers.Timer(1000) { AutoReset = true };
        _timer.Elapsed += async (_, _) =>
        {
            if (_type == OtpTool.OtpType.Totp && _result is not null)
            {
                await InvokeAsync(async () =>
                {
                    await RunAsync(_secret);
                    StateHasChanged();
                });
            }
        };
        _timer.Enabled = true;
    }

    // The tool generates a secret when given none; the page keeps it, so the codes stay put until asked for a new one.
    private async Task GenerateSecretAsync()
    {
        await RunAsync(null);
        if (_result is { HasErrors: false })
        {
            _secret = _result.Secret;
        }
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrWhiteSpace(_secret))
        {
            _result = null;
            return;
        }

        await RunAsync(_secret);
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await OnValueChangedAsync();
    }

    private async Task RunAsync(string? secret)
    {
        var args = new OtpTool.Args(
            secret,
            _type,
            _algorithm,
            _digits,
            _period,
            _counter,
            string.IsNullOrWhiteSpace(_code) ? null : _code,
            Issuer: string.IsNullOrWhiteSpace(_issuer) ? null : _issuer,
            AccountName: string.IsNullOrWhiteSpace(_accountName) ? null : _accountName);

        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    public void Dispose() => _timer?.Dispose();
}
