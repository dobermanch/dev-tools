# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Code Style & Guidelines

See [.github/instructions/code.instructions.md](.github/instructions/code.instructions.md) for coding principles, architecture rules, and testing strategy.

## In-Flight Design Work

[docs/design-review.md](docs/design-review.md) — **temporary** working document: a design review of the solution (localization scope, generator duplication, tool-contract seams for connectors and stream payloads, known bugs) plus a phased roadmap. Read it before reshaping the tool abstraction, the source generators, or the localization layout, and keep it current as phases land. **Delete it once the roadmap is done** — it is not permanent documentation and is deliberately not linked from the README.

Phases 0 (correctness fixes), 1 (English in the tools, localization confined to the Web UI), 2 (generator consolidation) and 3 (contract hardening) are done; phase 4 (connectors) is postponed until a second tool needs an external service - see [docs/proposals/connectors.md](docs/proposals/connectors.md), which is a standing feature proposal and stays when the review is deleted; phase 5 (file and binary payloads) is in progress; phase 6 is not started.

[docs/proposals/tool-flow.md](docs/proposals/tool-flow.md) records a future feature — running tools as a pipeline with parallel/loop/branch steps. It is why `[PipeInput]`/`[PipeOutput]` are a contract rather than a CLI convenience: a tool does not care where its input came from, and no frontend may reshape a pipe value.

[docs/proposals/it-tools-port.md](docs/proposals/it-tools-port.md) is the agreed list of tools to port from it-tools, in order, with the library decision for each. Check it before adding a tool, and update it as tools land.

## Build & Test Commands

```bash
dotnet build
dotnet test
dotnet restore

# Run a specific test class
dotnet test --filter "ClassName=BaseEncodingToolTests"

# Run the Web UI (add -watch for hot reload)
make web

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
| `Dev.Tools.Localization` | Translations of the tools' English (uk-UA); referenced by the Web UI only |
| `Dev.Tools.Api` | ASP.NET Core REST API |
| `Dev.Tools.Console` | Spectre.Console CLI (dotnet global tool) |
| `Dev.Tools.Mcp` | MCP server |

### The Tool Pattern

Every tool is a single file in `src/Dev.Tools/Tools/`. Decorate with `[ToolDefinition]` and inherit `ToolBase<TArgs, TResult>`:

```csharp
[ToolDefinition(
    Name = "tool-name",        // kebab-case; drives CLI commands, API routes, icon lookup
    Title = "My Tool",         // English display name
    Description = "Does the thing, in one line or several paragraphs",
    Aliases = ["alias"],
    Categories = [Category.Converter],
    Keywords = [Keyword.Text, Keyword.Encode]
)]
public sealed class MyTool : ToolBase<MyTool.Args, MyTool.Result>
{
    protected override Result Execute(Args args)
    {
        if (string.IsNullOrEmpty(args.Input))
            throw new ToolException(ErrorCode.TextEmpty, nameof(Args.Input));
        return new Result(/* computed value */);
    }

    public sealed record Args(
        [property: PipeInput,                  // [PipeInput] marks primary CLI-pipeable arg
                   Description("Text to work on")]
        string? Input,
        [property: Description("How many times; values below 1 are treated as 1")]
        int Count = 1
    );

    // Generator tool: single positional [PipeOutput] property
    public sealed record Result(
        [property: PipeOutput,
                   Description("The computed value")]
        string Output
    ) : ToolResult
    {
        public Result() : this(string.Empty) { }
    }
}
```

For **parser-style** tools (many output fields), use a non-positional `Result` record with nullable properties and `[PipeOutput]` on the main one.

**Error handling**: throw `ToolException(ErrorCode.X, nameof(Args.Y))` for known validation errors, naming the argument at fault — `ToolBase` catches it and returns a result whose `Errors` holds it. When several arguments can be wrong independently, collect `ToolError`s and throw `new ToolException(errors)` once, so the caller hears about all of them. Anything else a tool throws becomes `ErrorCode.Unknown` with no detail. Every frontend shows the code's English `[Description]` - never an exception message - so keep those messages short, plain and free of internals.

**Randomness and time**: never call `Random`, `RandomNumberGenerator`, `Guid.NewGuid()`, `Ulid.NewUlid()` or `DateTimeOffset.Now` directly. Take an optional `IRandomSource? random = null` and/or `TimeProvider? time = null` constructor parameter and default it to `CryptographicRandomSource.Instance` / `TimeProvider.System`. The container injects whatever a host registered, and tests pass `ScriptedRandomSource` / `FixedTimeProvider` (`tests/Dev.Tools.Tests/Helpers`) to assert the exact value a tool produces.

**Cancellation**: a tool with work worth stopping part way overrides `Execute(Args, CancellationToken)` instead of `Execute(Args)` and passes the token on - to `ParallelOptions` for a `Parallel.For`.

**Files and binary input**: a tool does not care where its input came from. Every frontend offers a file for the `[PipeInput]` argument in its own way - the CLI through stdin (`dev-tools hash < file.iso`), the API at `POST /tools/{name}/file` (multipart/form-data), the Web UI through a load-file button on the input pane (`AllowFileInput="true"` on its `DtInputPane`), on the pages where a file makes sense - so most tools need nothing for it. A tool whose input is a text document takes `string` and receives the file's text. A tool that needs the raw bytes, or input too big to hold in memory, takes `ToolContent` instead: override `ExecuteAsync`, read it with `ReadAllBytesAsync`/`ReadAllTextAsync` or stream it with `OpenRead()`, and report content that turns out to be empty as `TextEmpty`. A string converts to `ToolContent` implicitly, so callers and tests pass text unchanged. `ToolContent` is only allowed on the `[PipeInput]` argument (`DT1013`), and there are no content results yet. MCP takes text only - never file paths.

### Tool Text

A tool's English lives in the tool file and nowhere else. The generators lift it into CLI help, the OpenAPI document, the MCP tool schema and the Web UI, so it is worth writing as a contract — the format, the constraints, what omitting a value does — rather than restating the property name.

- **`Description` is required** on the tool, on every `Args`/`Result` property, and on every enum value those properties use. Four analyzer rules enforce it at build time: `DT1004` (tool description), `DT1005` (single-line `Title`), `DT1006` (property), `DT1007` (enum value).
- **The tool's shape is checked too.** `DT1008` the class must implement `ITool<TArgs, TResult>` (directly or through `ToolBase`); `DT1009` at most one `[PipeInput]` argument; `DT1010` at most one `[PipeOutput]` result; `DT1011` `Args` has exactly one public constructor, since argument defaults are read from it; `DT1012` (warning) every argument type is one the CLI, API and MCP can all bind — strings, booleans, numbers, `char`, enums, `Guid`, date/time types, their nullable forms, and arrays or lists of those. `Result` types are not restricted: they are only serialized. `DT1013` `ToolContent` only on the `[PipeInput]` argument.
- **`Description` may run to several paragraphs.** Use a raw string literal and wrap it for readability: a single newline inside a paragraph is dropped when it is read, a blank line starts a new paragraph. The first paragraph is what shows where only one line fits (the CLI command list); the full text shows where there is room.
- **`[DisplayName]` only when the derived label reads wrong.** `TypeProperty.Label` splits the property name (`InsertLineBreaks` → "Insert Line Breaks"), which is right most of the time, so most properties carry no label at all. Author one where it is not:

  ```csharp
  [property: DisplayName("URL Safe"),          // "Url Safe" otherwise
             Description("Use URL-safe encoding (replaces + with -, / with _, and removes padding)")]
  bool UrlSafe = false
  ```

- **`Usage`** is optional guidance for a model calling the tool through MCP — when to prefer it, when not to, what it will not do. It is appended to the MCP description only, so only the tools that need steering cost tokens.

### Source Generator — What's Auto-Generated

When you add `[ToolDefinition]` to a class in `Dev.Tools`, the `Dev.Tools.CodeAnalysis` generators automatically produce:
- `ToolsCatalog` — metadata registry used by `ToolsProvider`
- CLI command class (used by `Dev.Tools.Console`)
- API endpoint (used by `Dev.Tools.Api`)
- MCP tool definition (used by `Dev.Tools.Mcp`)
- `ToolDescriptions` — the detailed text the CLI help provider shows per command

Generated files land in `obj/.../generated/` — never edit them.

### Adding a New Tool (Checklist)

0. Decide whether it is a new tool at all, or a mode or option of an existing one - see [docs/tool-design.md](docs/tool-design.md). In short: inverses and views of one subject that share one contract are one tool with a mode argument (`html-entities`, `otp`); results of different shape are separate tools; an algorithm, format or version is always an option
1. Create `src/Dev.Tools/Tools/YourNameTool.cs` following the pattern above, English text included
2. Add test class `tests/Dev.Tools.Tests/Tools/YourNameToolTests.cs`
3. Build — the source generator runs and registers the tool automatically

That is the whole tool as far as the CLI, the API and the MCP server are concerned; they pick it up with no further work. Two follow-ups, neither of which the tool needs to function:

**Web UI** — a tool with no page still appears in search and navigation, but its route 404s, so add one:

- Create `src/Dev.Tools.Web/Pages/Tools/YourNameToolPage.razor` + `.razor.cs`
- If the `[PipeInput]` argument is something a user would have in a file - a document, encoded or encrypted data - put `AllowFileInput="true"` on the `DtInputPane` bound to it; add `OnFileLoaded` when the tool takes `ToolContent`, and the page receives the file's bytes instead of its text (`HashTextToolPage`, `BaseEncodingToolPage`). Short values (a number, a URL, a date, a token) get no file button
- Add icon to `src/Dev.Tools.Web/Services/ToolIconProvider.cs`
- Add web page labels to `Dev.Tools.Web/Locals/Resources.resx` (the neutral English fallback), `Resources.en-US.resx` and `Resources.uk-UA.resx` - every culture carries the neutral file's keys
- Key prefix for page labels: `Page.YourNameToolPage.*`

**Translations** — the Web UI shows the tool's English until someone translates it:

- `dotnet run --project tools/Translations -- uk-UA` lists what a culture is missing
- `--write` adds those entries to `ToolResources.uk-UA.resx` pre-filled with English, ready to translate
- `LocalizationTests` fails while a culture is incomplete, so this does not get forgotten

### Available Enums

**Categories**: `Converter`, `Crypto`, `Security`, `Text`, `Network`, `Web`, `Development`

**Keywords**: `Base64`, `Convert`, `Case`, `Decode`, `Encode`, `Generate`, `Guid`, `Hash`, `String`, `Json`, `Xml`, `Format`, `Password`, `Text`, `Url`, `Uuid`, `Network`, `Ip`, `Internet`, `Token`, `Web`, `Jwt`, `Lorem`, `Encrypt`, `Html`, `Hmac`, `Yaml`, `Toml`, `Csv`, `Regex`, `Cron`, `Otp`, `Diff`, `Color`, `Qr`

**ErrorCodes**: `Unknown`, `NamespaceEmpty`, `TextEmpty`, `FailedToDecrypt`, `InputNotValid`, `WrongBase`, `WrongFormat`, `FailedToEncrypt`, `MatchTimeout`, `DataTooLong`

If you need a new enum value, add it to the enum. `Category` and `Keyword` members carry a `[Description]` only where the English label differs from the member name (`Crypto` → "Cryptography"); every `ErrorCode` carries one, because those are the messages the CLI, API and MCP print.

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
        await Assert.That(result.Errors[0].Code).IsEqualTo(ErrorCode.TextEmpty);
    }
}
```

Tools can be instantiated directly (no DI required for unit tests).

### Blazor Web UI Patterns

Every tool page is assembled from the same few components, so writing one is mostly a layout decision. Do not reach for MudBlazor's own layout components (`MudCard`, `MudGrid`, `MudSelect`) — no page uses them any more:

- **`DtToolPageHeader`** — title, description and categories, all read from the `ToolDefinition`. Every page opens with it
- **`DtOptions`** — the grid the short controls live in: `DtSelectEnum`, `DtSwitch`, `DtSlider`, `DtToggleGroup`, and `DtTextField` for a value that fits on one line
- **`DtPanes`** — the input/output pair: `DtInputPane` (mono textarea, load-file and clear, char and line count, debounce) beside `DtOutputPane` (copy, download, an `ok`-or-error status line). Anything the user pastes belongs in a pane; anything they type a word into belongs in the options grid
- **`DtFieldList`/`DtFieldValue`** inside an output pane when a parser tool returns many named values, and **`DtTable`** when it returns rows

Two page styles exist — reference these when creating pages:

- **Generator style** (config → auto-updating output): `TokenGeneratorToolPage`, `LoremIpsumGeneratorToolPage`
- **Parser style** (input → conditional output fields): `JwtParserToolPage`, `UrlParserToolPage`

**Styling.** Design tokens are CSS custom properties (`--dt-*`) defined in `wwwroot/css/tokens.css`; never hard-code a colour, radius or font. Scoped CSS (`.razor.css`) cannot be shared between components, so anything two components both need — the focus ring (`.dt-focus-ring`), the mono microlabel (`.dt-label-mono`), the bordered control box (`.dt-box`) — is a class in `wwwroot/css/app.css` and is authoritative there. A control that looks like a bordered box *is* `.dt-box`, which is what keeps copy buttons, inputs and selects aligned with each other.

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
