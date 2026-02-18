using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtHtmlField : ComponentBase
{
    [Inject]
    private WebContext Context { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public MarkupString? FormattedValue { get; set; }
    
    [Parameter]
    public string? Value { get; set; }
    
    [Parameter]
    public EventCallback<MarkupString> ValueChanged { get; set; }
    
    [Parameter] 
    public bool ShowCopyButton { get; set; } = true;
    
    [Parameter] 
    public bool CopyFormattedValue { get; set; }
    
    [Parameter] 
    public int Lines { get; set; } = 1;

    private int Height => Lines * 19 - 11;

    private async Task OnCopyToClipboardAsync()
    {
        if (!CopyFormattedValue && Value is not null && !string.IsNullOrEmpty(Value))
        {
            await Context.JsService.CopyToClipboardAsync(Value!);
        }
        else if (FormattedValue is not null && !string.IsNullOrEmpty(FormattedValue.ToString()))
        {
            await Context.JsService.CopyToClipboardAsync(FormattedValue.ToString()!);
        } 
    }
}