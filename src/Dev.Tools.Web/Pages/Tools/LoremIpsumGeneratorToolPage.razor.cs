using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class LoremIpsumGeneratorToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private LoremIpsumGeneratorTool _tool = null!;
    private readonly Args _args = new();
    private LoremIpsumGeneratorTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    [Inject]
    private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<LoremIpsumGeneratorToolPage>();
        _tool = Context.ToolsProvider.GetTool<LoremIpsumGeneratorTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<LoremIpsumGeneratorTool>();
        await OnValueChangedAsync();

        await base.OnInitializedAsync();
    }

    private async Task OnValueChangedAsync()
    {
        _result = await _tool.RunAsync(new LoremIpsumGeneratorTool.Args(
                _args.Paragraphs,
                _args.SentencesPerParagraph,
                _args.WordsPerSentence,
                _args.StartWithLoremIpsum),
            CancellationToken.None);
    }

    private string GetText() => _result?.Text ?? string.Empty;

    private Task OnParagraphsValueChangedAsync(int value)
    {
        _args.Paragraphs = value;
        return OnValueChangedAsync();
    }

    private Task OnSentencesValueChangedAsync(int value)
    {
        _args.SentencesPerParagraph = value;
        return OnValueChangedAsync();
    }

    private Task OnWordsValueChangedAsync(int value)
    {
        _args.WordsPerSentence = value;
        return OnValueChangedAsync();
    }

    private Task OnStartWithLoremIpsumValueChangedAsync(bool value)
    {
        _args.StartWithLoremIpsum = value;
        return OnValueChangedAsync();
    }

    record Args
    {
        public int Paragraphs            { get; set; } = 3;
        public int SentencesPerParagraph { get; set; } = 5;
        public int WordsPerSentence      { get; set; } = 8;
        public bool StartWithLoremIpsum  { get; set; } = true;
    }
}
