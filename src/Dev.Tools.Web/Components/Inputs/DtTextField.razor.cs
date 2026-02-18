using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtTextField<T> : ComponentBase
{
    [Inject]
    private WebContext Context { get; set; } = null!;
    
    [Parameter]
    public string Class { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public LabelPositionType LabelPosition { get; set; }
    
    [Parameter]
    public T? Value { get; set; }
    
    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }
    
    [Parameter]
    public bool ReadOnly { get; set; }
    
    [Parameter]
    public string? ErrorText { get; set; }

    [Parameter]
    public bool Error { get; set; }

    [Parameter] 
    public double DebounceInterval { get; set; } = 500;

    [Parameter]
    public EventCallback<string> OnDebounceIntervalElapsed { get; set; }

    [Parameter] 
    public int MaxLength { get; set; } = 524288;

    [Parameter] 
    public bool ShowCopyButton { get; set; } = true;
    
    [Parameter]
    public string? Format { get; set; }
    
    [Parameter]
    public InputType InputType { get; set; } = InputType.Text;

    [Parameter] 
    public int Lines { get; set; } = 1;
    
    [Parameter] 
    public Func<T, bool>? Validation { get; set; }

    private async Task OnCopyToClipboardAsync()
    {
        if (Value is not null && !string.IsNullOrEmpty(Value.ToString()))
        {
            await Context.JsService.CopyToClipboardAsync(Value.ToString()!);
        }
    }
}