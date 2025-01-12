using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class DateTimeConverterToolPage : ComponentBase
{
    private System.Timers.Timer? _timer;
    private ToolDefinition _toolDefinition;
    private DateTimeConverterTool _tool = null!;
    private readonly DateTimeConverterTool.Args _args = new();
    private Dictionary<DateTimeConverterTool.DateFormatType, DateTimeConverterTool.Result?> _results = new();

    [Inject] private WebContext Context { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<DateTimeConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<DateTimeConverterTool>();
        _results = DateFormats()
            .ToDictionary(it => it, _ => (DateTimeConverterTool.Result?)null);

        await base.OnInitializedAsync();
        
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += UpdateTime;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private DateTimeConverterTool.DateFormatType[] DateFormats()
    {
        return Enum.GetValues<DateTimeConverterTool.DateFormatType>();
    } 
    
    private async Task OnValueChangedAsync(string value)
    {
        _args.Date = value;
        _timer!.Enabled = string.IsNullOrEmpty(value);
        
        await ConvertAsync(value);
    }
    
    private async Task ConvertAsync(string? date)
    {
        var tasks = new List<Task>();
        foreach (var format in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new DateTimeConverterTool.Args
                    {
                        Date = date,
                        From = _args.From,
                        To = format
                    },
                    CancellationToken.None)
                .ContinueWith(it =>
                {
                    _results[format] = it.Result;
                }));
        }
        
        await Task.WhenAll(tasks);
    }

    private void NavigateToPreviousPage()
    {
        Context.Navigation.NavigateTo("/");
    }

    private async Task OnCopyToClipboardAsync(string? textToCopy)
    {
        if (!string.IsNullOrEmpty(textToCopy))
        {
            await Context.JsService.CopyToClipboardAsync(textToCopy);
        }
    }

    private string ErrorMessage()
    {
        return string.Join(
            "; ",
            _results
                .Where(it => it.Value?.HasErrors ?? false)
                .Select(it => $"{it.Key.ToString()}: {it.Value!.ErrorCodes[0]}")
        );
    }

    private Task OnFromFormatSelectedAsync(DateTimeConverterTool.DateFormatType format)
    {
        _args.From = format;
        return ConvertAsync(_args.Date);
    }
    
    private async void UpdateTime(object? source, System.Timers.ElapsedEventArgs e)
    {
        await ConvertAsync(DateTime.Now.ToString("O"));
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}