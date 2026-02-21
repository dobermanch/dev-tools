# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Code Style & Guidelines

See [.github/instructions/code.instructions.md](.github/instructions/code.instructions.md) for coding principles, architecture rules, and testing strategy.

## Build & Test Commands

```bash
dotnet build
dotnet test
dotnet restore

# Run a specific test class
dotnet test --filter "ClassName=Base64EncoderToolTests"

# Pack NuGet library
make nuget

# Pack + install CLI global tool
make tool

# Clean build artifacts
make clean
```

Target framework: `net10.0`. Language version: `latest`. Nullable and ImplicitUsings are enabled globally.

## Architecture Overview

This is a multi-project .NET suite where all tool logic lives in **Dev.Tools** and is automatically surfaced across four frontends via Roslyn source generators:

| Project | Purpose |
|---|---|
| `Dev.Tools` | Core library — all tool implementations |
| `Dev.Tools.CodeAnalysis` | Roslyn source generators (netstandard2.0) |
| `Dev.Tools.Web` | Blazor WASM UI (MudBlazor) |
| `Dev.Tools.Localization` | RESX-based i18n (en, uk-UA) |
| `Dev.Tools.Api` | ASP.NET Core REST API |
| `Dev.Tools.Console` | Spectre.Console CLI (dotnet global tool) |
| `Dev.Tools.Mcp` | MCP server |

### The Tool Pattern

Every tool is a single file in `src/Dev.Tools/Tools/`. Decorate with `[ToolDefinition]` and inherit `ToolBase<TArgs, TResult>`:

```csharp
[ToolDefinition(
    Name = "tool-name",        // kebab-case; drives CLI commands, API routes, icon lookup
    Aliases = ["alias"],
    Categories = [Category.Converter],
    Keywords = [Keyword.Text, Keyword.Encode]
)]
public sealed class MyTool : ToolBase<MyTool.Args, MyTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Input))
            throw new ToolException(ErrorCode.TextEmpty);
        return new Result(/* computed value */);
    }

    public readonly record struct Args(
        [property: PipeInput] string? Input,   // [PipeInput] marks primary CLI-pipeable arg
        int Count = 1
    );

    // Generator tool: single positional [PipeOutput] property
    public sealed record Result([property: PipeOutput] string Output) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
```

For **parser-style** tools (many output fields), use a non-positional `Result` record with nullable properties and `[PipeOutput]` on the main one.

**Error handling**: throw `ToolException(ErrorCode.X)` for known validation errors — `ToolBase` catches it and returns a failed result. Use `ErrorCode.Unknown` for unexpected errors.

### Source Generator — What's Auto-Generated

When you add `[ToolDefinition]` to a class in `Dev.Tools`, the `Dev.Tools.CodeAnalysis` generators automatically produce:
- `ToolsCatalog` — metadata registry used by `ToolsProvider`
- CLI command class (used by `Dev.Tools.Console`)
- API endpoint (used by `Dev.Tools.Api`)
- MCP tool definition (used by `Dev.Tools.Mcp`)
- Localization key scaffolding

Generated files land in `obj/.../generated/` — never edit them.

### Adding a New Tool (Checklist)

1. Create `src/Dev.Tools/Tools/YourNameTool.cs` following the pattern above
2. Add test class `tests/Dev.Tools.Tests/Tools/YourNameToolTests.cs`
3. Build — the source generator runs and registers the tool automatically
4. **Localization** (manual):
   - Add entries to `Dev.Tools.Localization/Resources/ToolResources.en.resx` and `ToolResources.uk-UA.resx`
   - Key prefix: `Tools.<ClassName>.*` e.g. `Tools.MyTool.Name`, `Tools.MyTool.Description`, `Tools.MyTool.Args.Input.Name`, etc.
   - `ToolResources.resx` (no locale suffix) is **auto-generated on build** — do not edit it
5. **Web UI** (if adding a Blazor page):
   - Create `src/Dev.Tools.Web/Pages/Tools/YourNameToolPage.razor` + `.razor.cs`
   - Add icon to `src/Dev.Tools.Web/Services/ToolIconProvider.cs`
   - Add web page labels to `Dev.Tools.Web/Locals/Resources.en-US.resx` and `Resources.uk-UA.resx`
   - Key prefix for page labels: `Page.YourNameToolPage.*`

### Available Enums

**Categories**: `Converter`, `Crypto`, `Security`, `Text`, `Network`, `Web`

**Keywords**: `Base64`, `Convert`, `Case`, `Decode`, `Encode`, `Generate`, `Guid`, `Hash`, `String`, `Json`, `Xml`, `Format`, `Password`, `Text`, `Url`, `Uuid`, `Network`, `Ip`, `Internet`, `Token`, `Web`, `Jwt`, `Lorem`, `Encrypt`

**ErrorCodes**: `Unknown`, `NamespaceEmpty`, `TextEmpty`, `FailedToDecrypt`, `InputNotValid`, `WrongBase`, `WrongFormat`

If you need a new enum value, add it to the enum and add localization entries in both `.en.resx` and `.uk-UA.resx` files.

### Testing

Tests use **TUnit** (not xUnit/NUnit) with **Verify**. Test runner: `UseMicrosoftTestingPlatformRunner`.

```csharp
public class MyToolTests
{
    [Test]
    [Arguments("input", "expected")]
    public async Task ShouldProduceExpectedOutput(string input, string expected)
    {
        var result = await new MyTool().RunAsync(new MyTool.Args(input), CancellationToken.None);
        await Assert.That(result.Output).IsEqualTo(expected);
    }

    [Test]
    public async Task ShouldReturnError_WhenInputEmpty()
    {
        var result = await new MyTool().RunAsync(new MyTool.Args(string.Empty), CancellationToken.None);
        await Assert.That(result.HasErrors).IsTrue();
        await Assert.That(result.ErrorCodes[0]).IsEqualTo(ErrorCode.TextEmpty);
    }
}
```

Tools can be instantiated directly (no DI required for unit tests).

### Blazor Web UI Patterns

Two page styles exist — reference these when creating pages:

- **Generator style** (config → auto-updating output): `TokenGeneratorToolPage`, `LoremIpsumGeneratorToolPage`
- **Parser style** (input field → conditional output sections): `JwtParserToolPage`, `UrlParserToolPage`

Code-behind injects via `WebContext`:
```csharp
[Inject] private WebContext Context { get; set; } = null!;

protected override async Task OnInitializedAsync()
{
    _localizer = Context.Localization.PageLocalizer<MyToolPage>();
    _tool = Context.ToolsProvider.GetTool<MyTool>();
    _toolDefinition = Context.ToolsProvider.GetToolDefinition<MyTool>();
}
```
