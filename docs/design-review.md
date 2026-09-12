# Design Review & Improvement Roadmap

> Reviewed 2026-09-11 against commit `41f65a1`. Analysis only — no code changes were made in this pass.
> Every claim below is grounded in the code at the time of review (`file:line` references throughout).

## Context

Dev Tools' goal is: **write a tool once, ship it everywhere** — NuGet, dotnet global tool/CLI, REST API, MCP server, Blazor web, and later VS Code / browser extensions. Adding a tool should be near-effortless. Two future capabilities must not be designed into a corner:

- **External connectors** — a tool that calls a remote service whose endpoint is configurable.
- **File/stream payloads** — tools processing data too large or too binary for `string`.

Two directions were settled before the review was written up:

- **Localization becomes Web-only.** Core, CLI, API and MCP go English-only (§3).
- **No implementation in this pass.** The sequencing in §8 is a menu, not a commitment.

---

## 1. What the design gets right (keep it)

- **Single-file tool authoring.** `[ToolDefinition]` + `ToolBase<TArgs, TResult>` with nested `Args`/`Result` records is a genuinely good shape: declarative metadata, no interfaces to implement per frontend, testable without DI (`tests/Dev.Tools.Tests/Tools/*`).
- **A real runtime metadata model already exists.** `src/Dev.Tools/ToolDefinition.cs` + generated `ToolsCatalog.g.cs` (653 lines) carry name, aliases, categories, keywords, error codes, arg/result property shapes with `IsRequired`/`IsNullable`/`IsPipeInput`/`IsPipeOutput` and enum member lists. This is the right backbone — it is simply under-used (§4, §6).
- **Culture-neutral core.** `src/Dev.Tools` has **no** reference to `Dev.Tools.Localization` and no user-facing strings — errors are `ErrorCode` enum values. The desired "web-only localization" end state is already half-true; only the generators inject the coupling (§3).
- **`IMd5Hash` proves the portability seam works.** `src/Dev.Tools/Cryptography/MD5.cs` + `src/Dev.Tools.Web/Core/Cryptography/WebAssemblyMd5.cs` show how a host can substitute a platform-specific implementation. That pattern generalizes to time, randomness, and HTTP (§5).
- **`[PipeInput]`/`[PipeOutput]`** is a clean, frontend-agnostic way to express "the primary value", consumed generically by `Dev.Tools.Console/Core/ToolResponseHandler.cs:10-14`.
- **Web already runs purely off the catalog** for search/navigation (`Services/Search/ToolsSearchProvider.cs:40-46`, `Components/Tools/ToolCard.razor.cs:16` compute `tools/{Name}`), i.e. a fourth frontend that needs no generated code at all.

---

## 2. The real cost of adding a tool today

CLAUDE.md's checklist is 5 steps, but measured against the repo it is 7 artifacts in 4 projects:

| Artifact | Cost | Automatable? |
|---|---|---|
| `src/Dev.Tools/Tools/XTool.cs` | the actual work | — |
| `tests/.../XToolTests.cs` | the actual work | — |
| `ToolResources.en.resx` + `ToolResources.uk-UA.resx` | ~19 keys × 2 files (413 keys today) | **eliminate for non-web** (§3) |
| `Pages/Tools/XToolPage.razor` + `.razor.cs` | **~133 lines/tool** (22 pages, 2 926 lines) | mostly (§6) |
| `Web/Locals/Resources.en-US.resx` + `.uk-UA.resx` | page labels | partly |
| `Services/ToolIconProvider.cs` switch entry | 1 line, hand-synced | yes (§6) |

Three parallel hand-maintained lists (tool class, web page + route, icon) must stay in sync, and nothing enforces it: `ToolIconProvider.cs:24` maps `"rsa-generator"`, a tool that does not exist, and a missing page silently 404s because routes are hand-written `@page` strings while navigation computes `tools/{Name}` generically.

Worse, page code-behinds **re-declare the tool's own Args with duplicated defaults** (`TokenGeneratorToolPage.razor.cs:83-93` mirrors `TokenGeneratorTool.Args` including `= 15`, `= true`…). Change a default in core and the web silently disagrees.

**Target: adding a tool = 1 source file + 1 test file.**

---

## 3. Localization — Web-only

Measured depth of localization today:

| Frontend | What is localized | Actual value |
|---|---|---|
| Web | everything: titles, descriptions, enum values, error codes, 141 UI keys × 2 locales, culture switcher with persisted preference | high |
| CLI | `--help` text only; **no culture selection exists at all** (`grep CultureInfo` over `Dev.Tools.Console` → nothing) | ~zero |
| API | OpenAPI/Scalar metadata only; payloads and `ProblemDetails` never localized | ~zero |
| MCP | `tools/list` descriptions; no culture selection; read once at startup | ~zero, and arguably harmful — an LLM client benefits from stable English tool descriptions |

The cost of that near-zero value is structural:

1. **A global mutable static.** `LocalizedDescriptionAttribute.Description` resolves through `LocalizationProvider.Current` (`src/Dev.Tools.Localization/LocalizedDescriptionAttribute.cs:6-9`), backed by `LocalizationProvider.SetProviderResolver(() => services.BuildServiceProvider()...)` (`ServiceCollectionExtensions.cs:19-20`) — which **builds a second DI container** purely to feed an attribute. Attributes can't be injected, so this is unavoidable *given* attribute-based localization; removing the localization removes the need.
2. **Process-wide culture mutation.** `LocalizationProvider.SetCulture` writes `CultureInfo.DefaultThreadCurrentCulture/UICulture` (`LocalizationProvider.cs:66-75`) — acceptable in WASM, wrong in a server process.
3. **A build-time write into a sibling project.** `src/Dev.Tools/Dev.Tools.csproj:29-40` runs `dotnet tools/ResxGenerator/GenerateLocalizationResx.cs` `AfterTargets="Build"` and writes `ToolResources.resx` into `src/Dev.Tools.Localization/Resources`. This depends on the repo layout, has no project-ordering guarantee, and will never work for a NuGet consumer.
4. **Placeholder-valued neutral resources.** The generated neutral `ToolResources.resx` holds `:Description for HashText:` while real English lives in `ToolResources.en.resx`. Any culture that doesn't resolve (e.g. `de-DE` before fallback, or a trimmed satellite assembly) surfaces `:placeholders:` to users. `CompositeStringLocalizer.cs:13-35` returns the same `:key:` shape on a miss instead of throwing, so this fails silently.
5. **CLI/API/MCP can't drop the dependency by editing themselves** — the coupling is emitted by the generators (`CliCommandGenerator.cs:160,167,213,228`; `ApiEndpointGenerator.cs:69,83-84,125`; `McpToolGenerator.cs:236,247,298,354`).

### Recommended end state

**English text becomes first-class tool metadata in core**, and translation becomes a layer that only *user-facing* frontends load.

The axis is user-facing vs machine-facing, not Web vs everything else. Web is the only user-facing frontend today, but VS Code and browser extensions are planned and are equally user-facing, so tool translations must stay reusable rather than being absorbed into the Blazor app:

- **Tool-domain text** — titles, descriptions, enum value labels, error messages, categories, keywords — lives in `Dev.Tools.Localization` as RESX, shared by every user-facing frontend.
- **Frontend-shell text** — `Page.*` / `Component.*` labels, buttons, navigation — stays with the frontend that owns those screens (`Dev.Tools.Web/Locals/Resources.*.resx` today). A VS Code extension will never reuse `Page.TokenGeneratorToolPage.*`.
- **Machine-facing frontends** (API, MCP) ship English only, straight from the catalog.
- **The CLI ships English for now but is user-facing too**, so the door stays open: the retrofit is a Spectre `IHelpProvider` subclass in `Dev.Tools.Console` that looks each command/parameter up through a localizer and falls back to the attribute text, plus a culture mechanism (`--culture` or env var) which the CLI lacks entirely today. It needs no generator change, so there is no saving in building it early — but it is why the arg-description translations are kept rather than pruned.

- Author English `Title` / `Description` / `Usage` in `[ToolDefinition]`, and per-property / per-enum-value text as BCL `[Description]`, **where the tool is written**; the generators harvest it into `ToolsCatalog`. Attribute arguments were chosen over XML doc comments because they live in assembly metadata: every generator reads them through a `ProjectReference` with no `GenerateDocumentationFile` dependency and no silent degradation to empty text when a doc sidecar is missing. `Usage` is MCP-only model guidance (when to prefer the tool, when not to, result shape) and is appended to the MCP description alone. This removes the manual RESX step from the add-a-tool checklist entirely.
- Generators emit plain `[Description("…")]` (BCL) instead of `[LocalizedDescription("Tools.X…")]`. Spectre, ASP.NET OpenAPI and the MCP SDK all read `DescriptionAttribute` — no other change needed in the three frontends beyond dropping `.AddDevToolsLocalization()` from their `Program.cs` and deleting the two `LocalizedEndpoint*Attribute` files.
- `Dev.Tools.Localization` **stays a project** and keeps `ToolResources.*.resx`; only user-facing frontends reference it (Web today, VS Code / browser extensions later). It is not folded into `Dev.Tools.Web`, precisely so the next UI frontend inherits the translations instead of copying them.
- **Fallback becomes the catalog's English**, not a `:placeholder:` — `CompositeStringLocalizer` miss → `ToolsCatalog` description. The neutral placeholder RESX and `tools/ResxGenerator`'s neutral-file output can then go away; keep the generator only for *key inventory / missing-translation diffing* against the Web resources, run on demand rather than `AfterTargets="Build"`.
- `LocalizationKeysGenerator` stays (user-facing frontends need the key inventory), but its output moves out of the core assembly's build-time obligations. `tools/ResxGenerator` is repurposed: instead of writing a neutral placeholder RESX, it scaffolds and diffs translation files against the catalog.
- **Open consequence for non-.NET frontends.** A VS Code extension is TypeScript and cannot read RESX or satellite assemblies. Two architectures resolve this, and the choice can wait: either the extension is a thin client over the CLI/MCP server (the .NET side renders text and the host passes a culture), or it is standalone TypeScript and the build must *export* translations as JSON bundles (`l10n` for VS Code, `messages.json` for browser extensions). The repurposed `tools/ResxGenerator` is the natural home for such an export mode, which is another reason to keep it rather than delete it.
- **One-way door to note.** Baking English into generated `[Description]` attributes means re-adding CLI localization later can no longer go through attributes; it would need a custom Spectre `IHelpProvider` reading a localizer over the catalog. That path stays open and is clean, but it is a different mechanism than today's.

**Labels vs descriptions.** Both are authored in source and translated in RESX, but they differ in whether they can be derived: of the 118 `Args/Result .Name` keys, only 75 match a mechanical split of the property name — the other 43 carry wording a reader needs (`Data` → "Decoded Text", `Type` → "Case Type", `CharInfos` → "Character Details"). So labels are promoted to `[DisplayName]` next to `[Description]`, `TypeProperty` carries both plus a derived `Label` fallback, and `Tools.X.Args.P.Name` becomes `DisplayName`'s translation key instead of dead scaffolding. Nothing in the existing RESX is deleted; every key now has an English source of truth in code.

Result: core keeps zero `Microsoft.Extensions.Localization` coupling, CLI/API/MCP become English-only with *better* help text than today (real sentences, no resource lookup), the second DI container and static ambient provider disappear, and the add-a-tool RESX chore disappears except for optional translations in the user-facing frontends.

---

## 4. Source generators — one model, four re-derivations

The generator layer is the project's core bet, and it works, but it has drifted into copy-paste. `ToolGeneratorBase.cs` (389 lines) shares the *symbol reading*; everything downstream is duplicated:

| Duplicated concern | Copies |
|---|---|
| Incremental pipeline / assembly scan | `ApiEndpointGenerator.cs:13-26` ≡ `McpToolGenerator.cs:13-26` (character-identical); `CliCommandGenerator.cs:13-50` is the same wrapped in a 6-field debug tuple whose values are destructured and never used |
| DTO emission | `ApiEndpointGenerator.GenerateDataType:115-136` ≡ `McpToolGenerator.GenerateDataType:178-199` |
| Args/Result mapping | `ApiEndpointGenerator:138-166`, `McpToolGenerator:201-227`, `CliCommandGenerator.GenerateMapping:243-273` |
| Localization key convention | **5 independent string concatenations** (`CliCommandGenerator:151,205`; `ApiEndpointGenerator:122`; `McpToolGenerator:130,185`) plus a 6th reconstruction in `LocalizationKeysGenerator:135-163`; nothing checks they agree |
| `TypeName.Replace("Tool", …)` naming | 4 copies, and it is a global `Replace`, not a suffix trim (so `ToolkitTool` → `kit`) |
| Catalog/keys generator bodies | `ToolsCatalogGenerator.Initialize:18-63` ≈ `LocalizationKeysGenerator.Initialize:22-49`, incl. a duplicated nested `TrackingNames` class |

### Recommendations

1. **Extract a shared emit layer, not just a shared read layer.** One `ToolNaming` helper (fix the `Replace`), one `DtoEmitter`, one `MappingEmitter`, one pipeline factory. Each frontend generator should shrink to "which attributes and which method signature".
2. **Consider making the frontend generators consume `ToolDefinition`/the catalog model** rather than re-deriving from `ISymbol`. Today the model captures `IsRequired`, `IsNullable`, `IsPipeInput`, `IsPipeOutput`, `DefaultValue` — and uses them *inconsistently*: `IsPipeInput` only by CLI, `DefaultValue` only by MCP (`McpToolGenerator.cs:134-135`), `IsRequired`/`IsNullable` by nobody (everything is emitted as `= default!`). That's why **CLI silently drops every constructor default** (`CliCommandGenerator.cs:215,230`) while MCP honours them — the same tool behaves differently per frontend.
3. **Diagnose unsupported types instead of failing downstream.** There is no type mapping and no allow-list: `Type = it.Type.ToDisplayString()` (`ToolGeneratorBase.cs:250`) is emitted verbatim, so an exotic property type fails as a C# error inside generated code, an ASP.NET binding failure, or a Spectre `TypeConverter` failure at runtime. Add an allow-list (primitives, `string`, enums, `DateTimeOffset`, `Guid`, arrays/`IReadOnlyList<T>` of those, the future payload type) + a `DTxxxx` diagnostic.
4. **Fail loudly instead of silently skipping.** `GetToolDetails` returns `null` — tool vanishes, no diagnostic — when `[ToolDefinition]` lacks `Name` (`:136-139`) or no 2-arg generic interface is found (`:38-41`). Also `FindToolInterface:222-262` matches **any** 2-arg generic interface, not `ITool<,>` specifically.
5. **Strengthen the analyzer.** Today only DT1001 (unique name), DT1002 (valid name), DT1003 (`public sealed`) exist. Missing and worth adding: exactly one `[PipeInput]` (documented as a rule but unenforced), at most one `[PipeOutput]`, `Result` must have a parameterless ctor (required by `ToolBase.Failed1`), tool must implement `ITool<,>`, `Args` must have exactly one public constructor (`GetConstructorDefaultValues:264-314` silently takes the first).
6. **Restore determinism and test coverage.**
   - `Core/CodeBlock.cs:88` embeds `DateTime.Now` in every generated file header → every build re-emits every file; kills incremental-generator caching and makes snapshots noisy.
   - `ToolDetails`/`TypeDetails` keep `ISymbol`/`SyntaxNode` in the incremental model — precisely what `GeneratorTestsBase.AssertObjectGraph:238-241` bans, and the caching test that would catch it is `[Skip("temporary")]`.
   - Snapshots: `CliCommandGenerator`, `McpToolGenerator`, `LocalizationKeysGenerator` have **no tests**; `ApiEndpointGeneratorTests` is `[Skip("Just for now")]`; `ToolsCatalogGeneratorTests` runs but has no verified snapshot committed. Test compilations reference only `typeof(object).Assembly` (`GeneratorTestsBase.cs:98-101`), so snapshots are taken from compilations full of unresolved symbols.
7. **Dead code to remove:** `ValuesListGenerator` (its `[Generator]` is commented out at `:7`, its DT2001 rule and its test are dead), `GetToolDefinitionDetails:195-204`, the inert `.Where` assembly guard (`ToolsCatalogGenerator.cs:55-57`), and the unused scan tuple in `CliCommandGenerator`.
8. **Name mismatch:** `McpToolGenerator.cs:53` names the file `XMcpTool.g.cs` but the template at `:76` emits `class {TypeName}McpTool` → actual type `HashTextToolMcpTool`. Cosmetic, but it signals the naming logic isn't centralized.

---

## 5. Core contract — the seams the future features need

### 5a. Error model

`ToolResult.ErrorCodes` is an `IList` but only ever holds one code: `ToolBase.Failed1` (`ToolBase.cs:28-31`) creates the result and returns. An error carries **no target field, no message, no payload** — `ErrorCode` is a flat 7-value global enum, which already forces semantic abuse: the *encryptor* returns `FailedToDecrypt` (`TextEncryptorTool.cs:54`) and so does the *hasher* (`HashTextTool.cs:38`). `JsonFormatterTool` discards the parser's line/offset (`:18,28`).

Recommend: `ToolError(ErrorCode Code, string? Target = null, object?[]? Args = null)` and a result that can carry several. Codes stay culture-neutral (that is exactly what makes Web-only localization work — Web maps code → resx, as it already does at `JwtParserToolPage.razor:15`, the only place in the solution where an `ErrorCode` is shown to a human). `Args` lets Web format `"Value must be between {0} and {1}"` without core holding strings.

Also fix the frontends' error handling, which is inconsistent and in two cases absent:

- **CLI ignores `HasErrors` entirely** — prints the (empty) pipe-output and **exits 0** (`ToolResponseHandler.cs:8-25`). This is the most user-visible bug in the repo: `dev-tools base64-decoder "!!!"` reports success in a shell pipeline.
- **MCP has no error path at all** (`McpToolGenerator.cs:252-262`) — a failed tool returns an empty response with no error signal to the model.
- **API** keeps only `ErrorCodes[0]` and puts an enum name in the ProblemDetails `type` field, which is specified to be a URI (`ApiEndpointGenerator.cs:99`).

### 5b. Cancellation

`CancellationToken` is threaded through the signature but honoured in exactly one place. `ToolBase.ExecuteAsync:22-23` drops it for all 21 sync tools, including CPU-bound `Parallel.For` loops (`UuidGeneratorTool.cs:64`, `PassphraseGeneratorTool.cs:20`, `TokenGeneratorTool.cs:61`). Worse, `ToolBase.RunAsync`'s `catch (Exception)` (`:16-19`) swallows `OperationCanceledException` into `ErrorCode.Unknown`. Recommend: rethrow `OperationCanceledException`, pass the token into `ParallelOptions`, and give `Execute` an overload that receives the token.

### 5c. Determinism / testability

`Guid.NewGuid()`, `DateTimeOffset.Now/UtcNow`, `Ulid.NewUlid()`, `new Random()`, `RandomNumberGenerator`, and live HTTP are all called directly. Only `IMd5Hash` and `UuidGeneratorTool.Args.Time` are seams, so tests can assert shape but never values — and `IpDetailsToolTests` **hits the real network in a unit test**. Recommend two more core abstractions alongside `IMd5Hash`: BCL `TimeProvider` (already in .NET) and a small `IRandomSource` (`Next(int)`, `Fill(Span<byte>)`) with a crypto-backed default. This is also a correctness fix — see §7.

### 5d. External connectors (the future feature, designed now)

`IpDetailsTool` is the shape of everything to come, and it is currently the anti-pattern: hardcoded `https://api.ipify.org` / `api6.ipify.org` (`:15-16`), `new HttpClient()` created per request and never disposed (`:27`), `catch { return null; }` swallowing failures (and cancellation) into a null IP rather than an `ErrorCode`.

Recommended seam, in order of importance:

1. **Declare the dependency in the constructor**, like `IMd5Hash`. Core defines a minimal `IHttpRequester` (or takes `HttpClient` and lets the host own lifetime/factory) so core does **not** take a `Microsoft.Extensions.Http` dependency — that matters for WASM/AOT (§6).
2. **Options, not constants.** A per-tool options record (`IpDetailsOptions { Uri IpV4Endpoint; Uri IpV6Endpoint; TimeSpan Timeout; }`) with embedded defaults, bindable from `IConfiguration` by hosts that have one, and settable directly by hosts that don't (WASM, VS Code). Follow the repo's own guidance (`code.instructions.md`: "Prefer `IOptions<T>` for configuration binding") but keep the *core* type a plain record so the WASM/AOT host isn't forced into `Microsoft.Extensions.Options`.
3. **Declare the capability in metadata.** `[ToolDefinition(..., Capabilities = Capability.Network)]` flowing into `ToolDefinition`. This is what lets a browser extension hide network tools, the API document rate-limits, MCP mark a tool as non-hermetic, and the test suite exclude network tools from the default run.
4. **Network failures become `ErrorCode`s** (`ConnectorUnavailable`, `ConnectorTimeout`), not nulls.

### 5e. File/stream payloads (the other future feature)

Everything is `string` today, end to end: `Args`/`Result` hold whole payloads, and `StdinInterceptor.cs:31` does `Console.In.ReadToEnd()` and only assigns when the property is `typeof(string)` (`:38`) — so a non-string pipe input silently ignores stdin.

**Do not convert every tool to streams.** Recommend a payload abstraction used only by tools that need it:

```csharp
// core, no System.IO dependency at the call site for simple tools
public abstract class ToolContent           // implicit conversion from string
{
    public static ToolContent FromText(string s);
    public static ToolContent FromBytes(ReadOnlyMemory<byte> b);
    public static ToolContent FromStream(Func<Stream> open);   // lazy, re-openable
    public string AsText(); public Stream OpenRead();
}
```

`[PipeInput] ToolContent Input` then lets CLI stream stdin without `ReadToEnd`, lets API accept `multipart/form-data`, lets MCP pass a file path, and lets Web pass a `Blob` — while `base64-encoder("hello")` still compiles unchanged thanks to the implicit conversion. The generators need one addition: map `ToolContent` to the right binding per frontend (a `FileStream`/stdin for CLI, `IFormFile` for API, a string-or-path for MCP). Tools that are inherently whole-document (JSON/XML formatters) can keep `string`; tools that are inherently streaming (hash, base64) adopt `ToolContent`.

### 5f. Smaller contract issues

- `ToolDefinitionAttribute` is `internal` (`:4`) → **third-party assemblies cannot define tools.** If a VS Code extension or a plugin model is ever wanted, this must be `public`, and the generators must run in the consuming assembly (they already can — they are attribute-triggered per assembly).
- `IToolsProvider.GetTool<T>()` is generic-only and calls `ActivatorUtilities.CreateInstance<T>` (`ToolsProvider.cs:51`), which **bypasses the `AddTransient` registrations** made in `ServiceCollectionExtensions.cs:15` and constructs a new instance per call. A host that resolved a `ToolDefinition` **by name** has no way to get the instance — it must reflect. Add `object GetTool(Type)` / `GetTool(ToolDefinition)`, and either use the registrations or drop them.
- `ToolsProvider` doesn't index **aliases**; `GetToolDefinition(string)` matches `Name` only, so `dev-tools 64e` works (Spectre registers the alias) but a programmatic lookup by alias doesn't.
- `Category` and `Keyword` are closed enums in core — adding a tool in a new domain means editing library enums and two RESX files. Acceptable for a curated set; revisit if third-party tools become a goal.
- `ToolBase.Failed1` — name is a typo-shaped artifact; `ToolResult.ErrorCodes` is a mutable `IList` on a `record`.

---

## 6. Portability — what the new shipping targets require

Planned targets are VS Code and browser extensions; both imply WASM and/or AOT and/or a Node host.

- **Core's dependency surface is accidental.** `Dev.Tools.csproj` references `Microsoft.Extensions.Logging.Abstractions` (never used for logging — nothing registers or injects `ILogger`) and gets `Microsoft.Extensions.DependencyInjection.Abstractions` **transitively** for `IServiceCollection`/`ActivatorUtilities`, with `NU1510` suppressed globally (`Directory.Build.props:24`) to hide it. Recommend: core takes **zero** `Microsoft.Extensions.*` dependencies; move `ServiceCollectionExtensions` into a thin `Dev.Tools.DependencyInjection` package (or keep it but make `ToolsProvider` work off a `Func<Type, object>` factory so a DI-less host can use it).
- **The build-time RESX generation (`Dev.Tools.csproj:29-40`) must not survive** into a package-consumable core — it shells out to a repo-relative script path.
- `WordProvider.cs` is 3 878 lines of hardcoded arrays compiled into the assembly — fine functionally, but for a browser extension consider an embedded resource that can be trimmed/lazily loaded.
- **`ToolsCatalog` is `internal`** and reflection-typed (`typeof(...)` per tool); `LocalizationKeysProvider` exists precisely because reflection over tool types was undesirable in the RESX generator. For AOT/trimming, prefer the reflection-free path everywhere: frontends should bind via generated code or the `Type`-free model, never `GetProperty(name)` — `ToolResponseHandler.cs:12` and `StdinInterceptor.cs:11-22` both reflect today.

### Web UI — the biggest remaining per-tool cost

22 pages / 2 926 lines / ~133 lines per tool, with a fixed skeleton in all of them: identical 4-line `OnInitializedAsync`, one input control per `Args` property, one output field per `Result` property, one handler per property, a hand-written `@page` route, a hand-synced icon, and a duplicated private `record Args`.

Recommend a **metadata-driven `ToolPage` component**: one route `@page "/tools/{name}"` resolving `ToolDefinition` from the catalog, a property-type → editor map over the existing `Components/Inputs/Dt*` components (string → `DtTextField`, bool → `MudSwitch`, enum → `DtSelectEnum` using `EnumDetails.Values`, int → `DtSlider`), and args bound dynamically with defaults read from the record constructor (the generator already extracts them — `McpToolGenerator.cs:300-301`).

What resists generation, and therefore needs UI-hint metadata or an opt-out: slider bounds (`MinValue/MaxValue` vary per tool), grid widths, `Lines="5|8|10"`, section grouping/ordering, conditional rendering of optional parser outputs, and genuine outliers (`CharacterViewerPage`, 163-line code-behind, and `JwtParserToolPage`'s claims table). Recommend the escape hatch be explicit: the generic page is the default; a tool may still ship a hand-written page that wins by convention. Add `Icon` to `[ToolDefinition]` (or derive from `Category`) to kill `ToolIconProvider`'s third list.

---

## 7. Concrete bugs (Phase 0 — completed 2026-09-11)

All of the following were fixed, verified by `dotnet build`, 307 tests (305 pass / 2 pre-existing skips, green on three consecutive runs), and live CLI + MCP probes. Two review items turned out to be false positives and are recorded as such; five further defects surfaced only once the code was built and run.

| # | Issue | Location | Status |
|---|---|---|---|
| 1 | **UUID v3 was stamped as version 5.** `NewHashBasedUuid` hardcoded `0x50` for both v3 and v5, and hashed the namespace in `Guid.ToByteArray()` little-endian order instead of RFC 4122 network order, so no value matched any other implementation. | `UuidGeneratorTool.cs:79-92` | **Fixed** — version is now a parameter, namespace and result use `bigEndian: true`. Locked down by RFC 9562 known-answer tests (`5df41881-3aed-3515-88a7-2f4a814cf09e` / `2ed6657d-e927-568b-95e1-2665a8aea6a2`). |
| 2 | **`System.Random` shared across `Parallel.For`** — a data race, and non-cryptographic, in two *password* generators. | `PassphraseGeneratorTool.cs:17-30`, `TokenGeneratorTool.cs:58-70` | **Fixed** — both use `RandomNumberGenerator.GetInt32`, which is thread-safe and cryptographic. `LoremIpsumGeneratorTool` was also listed in the review but is **sequential**, so it had no race; plain `Random` is appropriate there and was left alone. |
| 3 | **CLI exited 0 on tool failure** and printed an empty line instead of the error. | `Console/Core/ToolResponseHandler.cs`, `Console/Program.cs` | **Fixed** — two layers: the handler ignored `HasErrors`, *and* `Program.cs` discarded `app.RunAsync`'s return value, so the process could never exit non-zero. Errors now go to stderr, exit code 1. |
| 4 | **MCP returned an empty success on failure** — no `HasErrors` check in generated code. | `McpToolGenerator.cs` | **Fixed** — generated tools throw `McpException` with the error codes; verified over stdio: `isError: true`, `"Tool 'base64-decoder' failed: InputNotValid"`. |
| 5 | **`new HttpClient()` per call, never disposed**; `catch { return null; }` hid failures *and* cancellation. | `IpDetailsTool.cs` | **Fixed** — one shared client with a 10s timeout, optional injected client, cancellation rethrown. `ToolBase` also no longer converts `OperationCanceledException` into `ErrorCode.Unknown` (pulled forward from §5b). |
| 6 | **`IpDetailsToolTests` called the live network.** | `tests/.../IpDetailsToolTests.cs` | **Fixed** — four hermetic tests over a stub `HttpMessageHandler` (success, HTTP failure, non-IP body, cancellation). |
| 7 | `TokenGeneratorTool` indexing bound read from the pre-materialized set rather than the array. | `TokenGeneratorTool.cs:65` | **False positive** — exclusions run before `ToArray()`, so the two counts are always equal. It did expose a real bug next to it: the empty-alphabet guard ran *before* exclusions, so excluding every character threw `IndexOutOfRangeException` (surfacing as `ErrorCode.Unknown`). Guard moved after exclusions. |
| 8 | CLI short-alias allocation short-circuiting on `\|\|`. | `CliCommandGenerator.cs:222` | **False positive** — not reserving the uppercase variant when the lowercase one succeeds is the intended behaviour; `-L` stays free for the next property. The real nit there was culture-sensitive `ToLower()`/`ToUpper()` (Turkish-I), now invariant. |
| 9 | `FormatKey` indexed `key[0]` with no empty-key guard. | `JsonFormatterTool.cs:92-93` | **Fixed** — empty keys pass through unchanged. |

### Surfaced during implementation (not in the original review)

These were invisible to a read-only review; the first appeared on the first `dotnet build`, the rest on the first real CLI/test run.

| # | Issue | Location | Status |
|---|---|---|---|
| 14 | **The Console project did not compile at `41f65a1`** — 22 × CS0507. Spectre.Console.Cli 0.55 made `AsyncCommand<T>.ExecuteAsync(..., CancellationToken)` `protected`, while the generator emitted `public override`. `dotnet build` was red on `main` before any change in this pass. | `CliCommandGenerator.cs` | **Fixed** — template emits `protected override`. |
| 15 | **Every collection `[PipeOutput]` printed `System.String[]`** — affected `uuid-generator`, `ulid-generator`, `token-generator`, `passphrase-generator`, `nato`, `char-viewer`. | `Console/Core/ToolResponseHandler.cs` | **Fixed** — collections render one value per line, keeping the CLI line-oriented for pipes. |
| 16 | **The CLI dropped every `Args` default** (§4.2's "emitted as `= default!`" in practice): `uuid-generator --type V4` produced un-hyphenated UUIDs, `token-generator` produced strings of spaces (empty alphabet), `passphrase-generator` produced one word instead of five. | `CliCommandGenerator.cs` | **Fixed** (pulled forward from Phase 2) — settings carry the record's default as both an initializer *and* `[System.ComponentModel.DefaultValue]`, because Spectre binds absent bool flags to `false` and only honours the default through the attribute. |
| 17 | **`ToolDeclarationAnalyzer` had a data race** — it calls `EnableConcurrentExecution()` but accumulated tool names into a plain `List<>` from concurrent syntax-node callbacks, so DT1001 intermittently reported nothing. Visible as a flaky `Should_report_diagnostic_When_name_is_not_unique` that passed in isolation and failed under full-suite load. | `ToolDeclarationAnalyzer.cs:58` | **Fixed** — `ConcurrentBag`. Suite now green on three consecutive full runs. |
| 18 | Review items #10-#13 (generated-header `DateTime.Now`, stale `ToolIconProvider`/RESX entries, csproj metadata, `TextEncryptorTool` key derivation) were deliberately left for Phases 2-3 as planned. | — | Deferred |

### Also removed in this pass

Dead generator code, as planned: `ValuesListGenerator` (its `[Generator]` was commented out) with its `GenerateValuesAttribute` definition, skipped test and snapshots, and the DT2001 row in `AnalyzerReleases.Unshipped.md`; the unused `GetToolDefinitionDetails` helper; the commented-out inert `.Where` guard in `ToolsCatalogGenerator`; and `CliCommandGenerator`'s leftover debug-scaffolding pipeline (6-field tuple whose values were never used), now matching the clean `ApiEndpointGenerator` shape while keeping the attribute gate that suppresses DTG1002 for assemblies that don't opt in.

---

## 8. Recommended sequencing

Each phase is independently shippable.

**Phase 0 — correctness, no design change. ✅ Done 2026-09-11.** §7 items 1-9 + dead generator code, plus five defects found only by building and running (§7.14-§7.17), plus the CLI-defaults fix pulled forward from Phase 2 and the cancellation rethrow pulled forward from Phase 3.

**Phase 1 — English metadata in core; translation limited to user-facing frontends.** Attribute-authored Title/Description/Usage + `[Description]` harvested into `ToolsCatalog`; generators emit `[Description]`/`[EndpointSummary]`; drop `.AddDevToolsLocalization()` from CLI/API/MCP; `Dev.Tools.Localization` keeps the tool RESX for Web and future VS Code / browser extensions, falling back to catalog English; an analyzer makes in-place description text mandatory; retire the neutral placeholder RESX and the `AfterTargets="Build"` RESX write. *Removes 2 files × ~19 keys from the add-a-tool checklist and deletes the static-provider/second-container machinery.*

**Phase 2 — generator consolidation.** Shared naming/DTO/mapping/pipeline; type allow-list with diagnostics; loud failures instead of silent skips; stronger analyzer rules; remove `DateTime.Now` and symbols from the incremental model; un-skip and fill in snapshot tests.

**Phase 3 — contract hardening.** `ToolError` with target/args and multiple errors; cancellation honoured and `OperationCanceledException` rethrown; `TimeProvider` + `IRandomSource` seams; `IToolsProvider.GetTool(Type/name)`; alias index; `ToolDefinitionAttribute` public.

**Phase 4 — connectors.** Constructor-injected `IHttpRequester`, per-tool options records with embedded defaults, `Capability` metadata, network `ErrorCode`s; `IpDetailsTool` migrated as the reference implementation; its test becomes hermetic.

**Phase 5 — `ToolContent` payloads.** Core type + implicit string conversion; generator binding per frontend; CLI stdin streaming; migrate hash/base64 as reference cases.

**Phase 6 — generic Web page + dependency-light core.** Metadata-driven `ToolPage` with per-tool UI hints and hand-written escape hatch; `Icon` in `[ToolDefinition]`; strip `Microsoft.Extensions.*` from core and move DI wiring to its own package — the precondition for VS Code / browser-extension targets.

---

## 9. Verification (per phase)

- `dotnet build` — must be warning-clean; confirm generated files under `src/*/obj/**/generated/` no longer churn between two consecutive builds (Phase 2).
- `dotnet test` — all TUnit suites; generator snapshot tests un-skipped and committed; network-dependent tests moved behind a category filter (Phase 4).
- CLI: `echo "Hello" | dev-tools base64-encoder | dev-tools base64-decoder`; `dev-tools base64-decoder "!!!"; echo $?` must be non-zero (Phase 0); `dev-tools hash --help` shows real English descriptions (Phase 1).
- API: `docker run` or `dotnet run`, check `/ui` Scalar doc renders descriptions, and `POST /tools/base64-decoder` with bad input returns a well-formed `ProblemDetails` (Phase 3).
- MCP: call a tool over stdio, plus an intentionally-failing input, to confirm errors surface (Phase 0/3).
- Web: `dotnet run` the Blazor app, switch culture en ↔ uk-UA, confirm no `:placeholder:` strings appear and each tool page still renders (Phase 1, 6).
