using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor.Utilities;

namespace Dev.Tools.Web.Pages.Tools;

public partial class QrCodeToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private QrCodeTool _tool = null!;
    private QrCodeTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _text = "https://dobermanch.github.io/dev-tools/";
    private QrCodeTool.QrContent _content = QrCodeTool.QrContent.Text;
    private QrCodeTool.QrErrorCorrection _errorCorrection = QrCodeTool.QrErrorCorrection.Medium;
    private int _border = 4;
    private MudColor _dark = new("#000000");
    private MudColor _light = new("#ffffff");
    private string _password = string.Empty;
    private QrCodeTool.WifiSecurity _security = QrCodeTool.WifiSecurity.Wpa;
    private bool _hidden;

    [Inject] private WebContext Context { get; set; } = null!;

    // The document without its XML declaration and doctype, which have no place inside HTML, and sized to its box.
    // Safe to render as markup: the library writes it, and the tool lets only hex colors into it.
    private string InlineSvg
    {
        get
        {
            var svg = _result!.Svg;
            return svg[svg.IndexOf("<svg", StringComparison.Ordinal)..].Replace("<svg ", "<svg width=\"100%\" ");
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<QrCodeToolPage>();
        _tool = Context.ToolsProvider.GetTool<QrCodeTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<QrCodeTool>();

        await OnValueChangedAsync();
        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        if (string.IsNullOrEmpty(_text))
        {
            _result = null;
            return;
        }

        var args = new QrCodeTool.Args(
            _text,
            _content,
            _errorCorrection,
            _border,
            _dark.ToString(MudColorOutputFormats.Hex),
            _light.ToString(MudColorOutputFormats.Hex),
            _password,
            _security,
            _hidden);

        _result = await _tool.RunAsync(args, CancellationToken.None);
    }

    private async Task SetOptionAsync(Action set)
    {
        set();
        await OnValueChangedAsync();
    }

    private string? ErrorFor(string target)
        => _result?.Errors
            .Where(it => it.Target == target)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
