using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Tools;

public partial class KeywordChips : ComponentBase
{
    private const string DivMarker = "keyword-chips-singleline";
    private List<Keyword> _visibleKeywords = new();
    private List<Keyword> _hiddenKeywords = new();
    private bool _initialized;

    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public ToolDefinition ToolDefinition { get; set; } = null!;

    [Parameter]
    public bool SingleLine { get; set; }

    protected override void OnParametersSet()
    {
        if (SingleLine)
        {
            _visibleKeywords = ToolDefinition.Keywords.OrderBy(it => it).ToList();
            _hiddenKeywords = new List<Keyword>();
            _initialized = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (SingleLine && !_initialized && _visibleKeywords.Any())
        {
            _initialized = true;
            await Task.Delay(50);
            await AdjustVisibleChipsAsync();
        }
    }

    private async Task AdjustVisibleChipsAsync()
    {
        try
        {
            var keywords = ToolDefinition.Keywords.OrderBy(it => it).ToList();
            var totalCount = keywords.Count;

            var containerWidth = await Context.JsService.InvokeAsync<double>(
                "eval",
                CancellationToken.None,
                $"document.querySelector('.{DivMarker}')?.clientWidth || 0");

            if (containerWidth <= 0)
            {
                return;
            }

            // Calculate how many chips can fit
            const int chipMaxWidth = 70;
            const int plusChipWidth = 50;
            const int gap = 4;
            double chipWidth = chipMaxWidth + gap;

            int visibleCount = 0;
            double usedWidth = 0;

            for (int i = 0; i < totalCount; i++)
            {
                double widthWithPlusChip = usedWidth + chipWidth + plusChipWidth + gap;
                double widthWithoutPlus = usedWidth + chipWidth;

                if (i == totalCount - 1)
                {
                    // Last chip, no need for +N
                    if (widthWithoutPlus <= containerWidth)
                    {
                        visibleCount = i + 1;
                    }
                    break;
                }

                // Check if we can fit this chip + the +N indicator
                if (widthWithPlusChip <= containerWidth)
                {
                    visibleCount = i + 1;
                    usedWidth += chipWidth;
                }
                else
                {
                    // Can't fit more
                    break;
                }
            }

            _visibleKeywords = keywords.Take(visibleCount).ToList();
            _hiddenKeywords = keywords.Skip(visibleCount).ToList();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"KeywordChips overflow detection failed: {ex.Message}");
        }
    }
}