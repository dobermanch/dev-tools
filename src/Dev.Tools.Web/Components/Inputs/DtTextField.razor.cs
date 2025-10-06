using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dev.Tools.Web.Components.Inputs;

public partial class DtTextField<T> : ComponentBase
{
    private const int LabelWidthMinWidth = 100;
    private const int LabelWidthMaxWidth = 300;
    
    private int _labelWidth = LabelWidthMinWidth;
    
    [Inject]
    private WebContext Context { get; set; } = null!;
    
    [Parameter]
    public string Class { get; set; } = null!;

    [Parameter]
    public string Label { get; set; } = null!;

    [Parameter]
    public int LabelWidth
    {
        get => _labelWidth;
        set => _labelWidth = value <= 0 ? LabelWidthMinWidth : value > LabelWidthMaxWidth ? LabelWidthMaxWidth : value;
    }
    
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
    public int MaxLength { get; set; }

    [Parameter] 
    public bool ShowCopyButton { get; set; } = true;
    
    [Parameter]
    public string? Format { get; set; }
    
    [Parameter]
    public InputType InputType { get; set; } = InputType.Text;

    [Parameter] 
    public int Lines { get; set; } = 1;

    private async Task OnCopyToClipboardAsync()
    {
        if (Value is not null && !string.IsNullOrEmpty(Value.ToString()))
        {
            await Context.JsService.CopyToClipboardAsync(Value.ToString()!);
        }
    }
}