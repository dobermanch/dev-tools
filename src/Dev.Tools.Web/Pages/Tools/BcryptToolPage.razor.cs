using Dev.Tools.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Dev.Tools.Web.Pages.Tools;

public partial class BcryptToolPage : ComponentBase
{
    private ToolDefinition _toolDefinition = null!;
    private BcryptTool _tool = null!;
    private BcryptTool.Result? _result;
    private IStringLocalizer _localizer = null!;

    private BcryptTool.BcryptOperation _operation = BcryptTool.BcryptOperation.Hash;
    private string _password = string.Empty;
    private string _hash = string.Empty;
    private int _cost = 10;
    private bool _running;

    [Inject] private WebContext Context { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _localizer = Context.Localization.PageLocalizer<BcryptToolPage>();
        _tool = Context.ToolsProvider.GetTool<BcryptTool>();
        _toolDefinition = Context.ToolsProvider.GetToolDefinition<BcryptTool>();

        await base.OnInitializedAsync();
    }

    private void SetOperation(BcryptTool.BcryptOperation operation)
    {
        _operation = operation;
        _result = null;
    }

    // Run on request rather than as the user types: bcrypt is slow on purpose, and slower still in the browser,
    // where it holds the page until it finishes.
    private async Task RunAsync()
    {
        _running = true;
        StateHasChanged();

        // Let the button show that it is working before the hash takes the thread.
        await Task.Delay(1);

        try
        {
            _result = await _tool.RunAsync(
                new BcryptTool.Args(_password, _operation, _hash, _cost),
                CancellationToken.None);
        }
        finally
        {
            _running = false;
        }
    }

    private string? ErrorFor(string target)
        => _result?.Errors
            .Where(it => it.Target == target)
            .Select(it => Context.Localization.GetErrorCode(it.Code))
            .FirstOrDefault();
}
