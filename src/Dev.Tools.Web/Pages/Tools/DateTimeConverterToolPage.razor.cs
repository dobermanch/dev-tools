using Dev.Tools.Tools;
using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Dev.Tools.Web.Pages.Tools;

public partial class DateTimeConverterToolPage : ComponentBase
{
    private System.Timers.Timer? _timer;
    private ToolDefinition _toolDefinition = null!;
    private DateTimeConverterTool _tool = null!;
    private readonly Args _args = new();
    private Dictionary<DateTimeConverterTool.DateFormatType, DateTimeConverterTool.Result?> _results = new();

    [Inject] private WebContext Context { get; set; } = null!;
    
    protected override async Task OnInitializedAsync()
    {
        _tool = Context.ToolsProvider.GetTool<DateTimeConverterTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<DateTimeConverterTool>();
        
        _results = Enum
            .GetValues<DateTimeConverterTool.DateFormatType>()
            .ToDictionary(it => it, _ => (DateTimeConverterTool.Result?)null);

        await base.OnInitializedAsync();
        
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += UpdateTime;
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }
    
    private async Task OnValueChangedAsync(string value)
    {
        _args.Date = value;
        _timer!.Enabled = string.IsNullOrEmpty(value);
        
        await ConvertAsync();
    }
    
    private async Task ConvertAsync()
    {
        var tasks = new List<Task>();
        foreach (var format in _results.Keys)
        {
            tasks.Add(_tool.RunAsync(new DateTimeConverterTool.Args
                    {
                        Date = _args.Date,
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
        return ConvertAsync();
    }
    
    private async void UpdateTime(object? source, System.Timers.ElapsedEventArgs e)
    {
        _args.Date = DateTime.Now.ToString("O");
        await ConvertAsync();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
    
    public record Args
    {
        public string Date { get; set; } = DateTime.Now.ToString("O");
        public DateTimeConverterTool.DateFormatType From { get; set; } = DateTimeConverterTool.DateFormatType.Iso8601;
        public DateTimeConverterTool.DateFormatType To { get; set; }
    }
}