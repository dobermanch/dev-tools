using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class TextStatisticsToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private TextStatisticsTool _tool = null!;
    private TextStatisticsTool.Result _result = new();
    private IStringLocalizer _localizer = null!;

    private string _text = string.Empty;

    [Inject] private WebContext Context { get; set; } = null!;

    // What copy and download act on: every count at once, labelled.
    private string AllStatistics => string.Join(Environment.NewLine,
        $"{_localizer["TextField.Characters.Label"]}: {_result.Characters}",
        $"{_localizer["TextField.NonWhitespaceCharacters.Label"]}: {_result.NonWhitespaceCharacters}",
        $"{_localizer["TextField.Words.Label"]}: {_result.Words}",
        $"{_localizer["TextField.Lines.Label"]}: {_result.Lines}",
        $"{_localizer["TextField.Paragraphs.Label"]}: {_result.Paragraphs}",
        $"{_localizer["TextField.Utf8Bytes.Label"]}: {_result.Utf8Bytes}",
        $"{_localizer["TextField.Utf16Length.Label"]}: {_result.Utf16Length}");

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<TextStatisticsToolPage>();
        _tool = Context.ToolsProvider.GetTool<TextStatisticsTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<TextStatisticsTool>();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new TextStatisticsTool.Args(_text), CancellationToken.None);
    }
}
