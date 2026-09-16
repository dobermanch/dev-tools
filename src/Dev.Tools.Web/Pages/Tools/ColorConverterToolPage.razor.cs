using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor.Utilities;

namespace Dev.Tools.Web.Pages.Tools;

public partial class ColorConverterToolPage : ComponentBase
{
    private const string InitialColor = "#1e90ff";

    private ToolDefinition _toolDefinition = null!;
    private ColorConverterTool _tool = null!;
    private ColorConverterTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private string _color = InitialColor;
    private MudColor _picked = new(InitialColor);

    [Inject] private WebContext Context { get; set; } = null!;

    private string? ColorError => _result?.Errors
        .Select(it => Context.Localization.GetErrorCode(it.Code))
        .FirstOrDefault();

    // What copy and download act on: the colour in every notation at once.
    private string AllFormats => _result is null or { HasErrors: true }
        ? string.Empty
        : string.Join(Environment.NewLine, new[]
        {
            $"{_localizer["TextField.Hex.Label"]}: {_result.Hex}",
            $"{_localizer["TextField.Rgb.Label"]}: {_result.Rgb}",
            $"{_localizer["TextField.Hsl.Label"]}: {_result.Hsl}",
            $"{_localizer["TextField.Hwb.Label"]}: {_result.Hwb}",
            $"{_localizer["TextField.Cmyk.Label"]}: {_result.Cmyk}",
            _result.Name is null ? null : $"{_localizer["TextField.Name.Label"]}: {_result.Name}"
        }.Where(line => line is not null));

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<ColorConverterToolPage>();
        _tool = Context.ToolsProvider.GetTool<ColorConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<ColorConverterTool>();

        await ConvertAsync();
        await base.OnInitializedAsync();
    }

    // Typed text moves the picker to the color, once it is one.
    private async Task OnColorChangedAsync()
    {
        await ConvertAsync();
        if (_result is { HasErrors: false })
        {
            _picked = new MudColor(_result.Hex);
        }
    }

    // A picked color replaces the text with its hex.
    private async Task OnPickedAsync(MudColor color)
    {
        _picked = color;
        _color = color.ToString(MudColorOutputFormats.HexA);
        await ConvertAsync();
        if (_result is { HasErrors: false })
        {
            _color = _result.Hex;
        }
    }

    private async Task ConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(_color))
        {
            _result = null;
            return;
        }

        _result = await _tool.RunAsync(new ColorConverterTool.Args(_color), CancellationToken.None);
    }
}
