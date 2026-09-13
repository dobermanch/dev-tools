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

## 3. Localization — user-facing frontends only

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
- **Fallback becomes the catalog's English**, not a `:placeholder:` — `CompositeStringLocalizer` miss → `ToolsCatalog` description. The neutral placeholder RESX and the resx generator's neutral-file output can then go away; keep the tool only for *key inventory / missing-translation diffing* against the Web resources, run on demand rather than `AfterTargets="Build"`.
- `LocalizationKeysGenerator` stays (user-facing frontends need the key inventory), but its output moves out of the core assembly's build-time obligations. The resx generator is repurposed: instead of writing a neutral placeholder RESX, it scaffolds and diffs translation files against the catalog.
- **Open consequence for non-.NET frontends.** A VS Code extension is TypeScript and cannot read RESX or satellite assemblies. Two architectures resolve this, and the choice can wait: either the extension is a thin client over the CLI/MCP server (the .NET side renders text and the host passes a culture), or it is standalone TypeScript and the build must *export* translations as JSON bundles (`l10n` for VS Code, `messages.json` for browser extensions). The repurposed translation tool is the natural home for such an export mode, which is another reason to keep it rather than delete it.
- **One-way door to note.** Baking English into generated `[Description]` attributes means re-adding CLI localization later can no longer go through attributes; it would need a custom Spectre `IHelpProvider` reading a localizer over the catalog. That path stays open and is clean, but it is a different mechanism than today's.

**Labels vs descriptions.** Both are authored in source and translated in RESX, but they differ in whether they can be derived: of the 118 `Args/Result .Name` keys, only 75 match a mechanical split of the property name — the other 43 carry wording a reader needs (`Data` → "Decoded Text", `Type` → "Case Type", `CharInfos` → "Character Details"). So labels are promoted to `[DisplayName]` next to `[Description]`, `TypeProperty` carries both plus a derived `Label` fallback, and `Tools.X.Args.P.Name` becomes `DisplayName`'s translation key instead of dead scaffolding. Nothing in the existing RESX is deleted; every key now has an English source of truth in code.

Result: core keeps zero `Microsoft.Extensions.Localization` coupling, CLI/API/MCP become English-only with *better* help text than today (real sentences, no resource lookup), the second DI container and static ambient provider disappear, and the add-a-tool RESX chore disappears except for optional translations in the user-facing frontends.

---

## 3a. Phase 1 progress and decisions

**Step 1 (core metadata plumbing) — done.** `ToolDefinitionAttribute` gains `Title` / `Description` / `Usage`; `ToolDefinition` gains `Title`, `Description`, `Usage` and a derived `Summary` (first line, for one-line lists); `TypeProperty` gains `Description`, `DisplayName` and a derived `Label`; `EnumDetails.Values` becomes `EnumValue[]` (name + description). `ToolGeneratorBase` reads all of it from assembly metadata and `ToolsCatalogGenerator` emits it. `global using System.ComponentModel;` added to core. `Base64EncoderTool` is migrated as the reference example. Tests: `tests/Dev.Tools.Tests/ToolsCatalogTests.cs`, `tests/Dev.Tools.CodeAnalysis.Tests/Generators/ToolTextEmissionTests.cs`.

**Decisions taken during Phase 1 design:**

| Decision | Choice |
|---|---|
| Where English is authored | Attribute arguments, not XML doc comments — they live in assembly metadata, so generators read them through a `ProjectReference` with no doc-sidecar dependency |
| Multi-line descriptions | Supported via raw string literals; text is normalized to LF and escaped on emission; `Summary` gives the first line for CLI/MCP lists |
| Model-facing MCP text | Optional `Usage` on `[ToolDefinition]`, appended to the MCP description only, so only tools that need steering cost tokens |
| Display text scope in core | Everything: tool title/description, property descriptions and labels, enum values, `Category`, `Keyword`, `ErrorCode` |
| `ErrorCode` English messages | Yes — so CLI/API/MCP print sentences instead of enum names |
| Property labels | `[DisplayName]` authored **only where the derived label reads badly** (49 of 118 cases); `TypeProperty.Label` derives the rest by splitting the property name, to keep tool files readable |
| RESX | Nothing deleted. Every key keeps an English source of truth in code and a translation slot in `Dev.Tools.Localization`, including the 74 arg descriptions a localized CLI would need |
| `Dev.Tools.Localization` | Stays a project, referenced by user-facing frontends only (Web now; VS Code / browser extensions later) |
| CLI localization | Deferred, not foreclosed: a Spectre `IHelpProvider` subclass plus a culture mechanism, self-contained in `Dev.Tools.Console`, no generator involvement |

**Remaining steps**, each a separate reviewable commit:

~~2. Migration of the English text into source attributes.~~ **Done.** All 22 tools carry `Title` and `Description`; all 118 argument and result properties and all 96 enum members carry `[Description]`; 49 properties carry a `[DisplayName]` where the derived label reads badly. `Category`, `Keyword` and `ErrorCode` carry English on the members that need it — for `Category`/`Keyword` only where the label differs from the member name, for every `ErrorCode` because they are messages. Descriptions were rewritten as contracts (format, constraints, what omission does) rather than copied verbatim, and `Usage` was written for the eleven tools that need model steering: `hash`, `text-encryptor`, `text-decryptor`, `uuid-generator`, `date-convert`, `json-formatter`, `xml-formatter`, `jwt-parser`, `char-viewer`, `int-base-converter`, `ip-details`.

   Three defects found by reading the live MCP schema were folded in: `uuid-generator` defaulted to `Nil` and now defaults to `V4` (the Web page already used `V4`, so this removes a frontend disagreement); `date-convert` declared `Date` as required while its own text said "leave empty for current time" and its implementation already handled it, so the parameter became `string? Date = null`; and `int-base-converter` advertised "custom bases (2-64)" while exposing only five named bases, so the claim is gone and `Usage` says so explicitly. `docs/tools.md` was updated for all three. Writing the text against the implementation rather than against the RESX turned up five claims that were simply untrue and are now corrected: `base64-decoder` does not accept the URL-safe alphabet, `json-formatter` rejects comments rather than dropping them, `text-decryptor` only authenticates under AES-GCM, `token-generator` does not clamp a zero length, and `url-transcoder` writes a space as `+`, not `%20`.

   Five tests in `ToolsCatalogTests` now assert that no tool, property or enum member is left without text, that titles stay single-line, and that no `[DisplayName]` merely repeats what the property name already derives — a stopgap until step 3 turns the first three into analyzer rules.
~~3. Analyzer rules making description text mandatory in place.~~ **Done.** Four rules at error severity, alongside DT1001-1003: **DT1004** a tool must set a non-empty `Description`; **DT1005** `Title` must be a single line; **DT1006** every argument and result property needs `[Description]`; **DT1007** every enum value they use needs one. DT1006/DT1007 find the types the way the generators do — the two type arguments of the tool interface — so nested helper types the catalog never reads (`CharacterViewerTool.CharInfo`) are left alone, and the core `Category`/`Keyword`/`ErrorCode` enums are skipped because they are not per-tool text. The rules deliberately carry no `CompilationEnd` tag: unlike DT1001 they are decidable per declaration, so they report while typing.

   The `ToolsCatalogTests` assertions from step 2 stay: the analyzer stops undescribed text being *written*, while those tests check the other half — that what was written survives the generator into the catalog every frontend reads.
~~4. Generators emit the English text instead of localization keys.~~ **Done.** The CLI emits `[Description]`, the API emits the built-in `[EndpointSummary]` / `[EndpointDescription]`, and MCP emits `[Description]` plus `Title` on `[McpServerTool]`. All three hosts dropped `.AddDevToolsLocalization()` and their `Dev.Tools.Localization` project reference, and the two `LocalizedEndpoint*Attribute` files are gone. Verified live: the CLI shows real sentences with no resource lookup, the OpenAPI document carries a summary, a full description and per-property descriptions on all 22 endpoints, and `tools/list` returns a title and a description for all 22 MCP tools.

   **MCP now emits an output schema.** `UseStructuredContent = true` makes the SDK publish the response type's schema, so all 22 tools now carry one and the result descriptions reach the model for the first time. `Usage` is appended to the MCP description only. Tool annotations (`ReadOnly`, `OpenWorld`) are deliberately *not* emitted: they would have to be hardcoded true for every tool today, which is a claim the generator cannot check — they belong with the `Capability` metadata in Phase 4 (postponed; see [docs/proposals/connectors.md](proposals/connectors.md)). **Emitted in the interim since 2026-09-13**: leaving them out was worse, because the spec's defaults tell a model every tool may be destructive and reach the outside world. Every tool is `ReadOnly = true, Destructive = false`, and `OpenWorld` is true only for a tool whose constructor takes an `HttpClient` (`ip-details`) - read from the code, not from the Network category, which also fits pure calculators such as a CIDR calculator; verified over stdio in `tools/list`.

   Two gaps showed up while verifying, both fixed here:

   - **JSON Schema has no per-enum-value documentation**, so an enum parameter reached the model as a bare list of names and the 96 authored value descriptions went nowhere. The MCP generator now folds them into the parameter description (`"Hashing algorithm to use. Options: Md5 = MD5 - 128-bit, fast, not collision resistant; ..."`). The OpenAPI document has the same limitation and still shows bare enum names — worth revisiting when the API gets attention.
   - **Spectre reads one description for both the command list and a single command's help**, and the list only fits a line, so detailed descriptions would have been truncated everywhere. The generator now also emits `ToolDescriptions`, a map of the tools whose text runs past one paragraph, and a `ToolHelpProvider` (a Spectre `HelpProvider` subclass) reads it for `dev-tools <tool> --help`. That is also the seam a future localized CLI needs.

   One convention settled while doing it: a **single newline inside a paragraph is dropped** when the generator reads authored text, and a blank line starts a new paragraph. Raw string literals in tool files are wrapped for readability, and every frontend wraps to its own width, so without this the text wrapped twice and came out ragged.
~~5. `Dev.Tools.Localization` cleanup.~~ **Done.** `LocalizedDescriptionAttribute` is gone, and with it the static `LocalizationProvider.Current`, the `SetProviderResolver` hook and the `services.BuildServiceProvider()` call that built a second DI container just to feed an attribute. A new `ToolCatalogStringLocalizer` serves the tools' own English under the RESX key shapes and is registered **last**, so a translation wins where one exists and English shows through where one does not — which is every key of every tool added from now on.

   English is therefore **out of the resource files entirely**: `ToolResources.en.resx` is deleted and the neutral `ToolResources.resx` is emptied of data. The neutral file has to stay because `RegisterResourcesForAssembly` enumerates embedded `.resources` to decide what to register, and without it `IStringLocalizer<ToolResources>` would never be registered and the uk-UA satellite would never be consulted; verified that the emptied file still embeds `Dev.Tools.Localization.Resources.ToolResources.resources`. `ToolResources.uk-UA.resx` keeps all 413 translations.

   `tools/ResxGenerator` is renamed `tools/Translations`, since it no longer generates anything. It is now a translation checker: `dotnet run --project tools/Translations -- uk-UA` reports missing and stale keys against the catalog and exits non-zero if either exists, and `--write` adds the missing entries pre-filled with English and drops the stale ones. It edits the file rather than rewriting it (since 2026-09-13; it used to re-sort every entry, which stranded the section comments at the top and added a BOM): a missing entry goes next to its own tool's entries, or under a new section comment, and the order, comments and indentation a translator kept stay as they were. It takes the resources directory as `--resources <dir>` and otherwise finds this repository's by walking up for the `.slnx` — not by counting `../` out of the build output, which changes with the configuration and the target framework. It stays under `tools/` rather than moving into `Dev.Tools.Localization`: the library ships to the browser in the WASM payload and has no business carrying `System.Xml.Linq` and file IO for something that only runs on a developer's machine. The part that does belong in the library — the English key inventory — is already there, and the tool and the tests both read it from `ToolCatalogStringLocalizer`. `LocalizationKeysGenerator` and the `LocalizationKeysProvider` it emitted into the core assembly are deleted — the checker reads the catalog localizer instead, which carries the English values as well as the keys, and nothing else used them.

   The `GenerateLocalizationResx` target is gone from `Dev.Tools.csproj`, so the core project no longer shells out to a repo-relative script and writes into a sibling project on every build.

   Six tests in `LocalizationTests` cover it: the catalog serves every key shape, a key with no translation resolves to English rather than a `:placeholder:`, a uk-UA translation wins over the English, and the uk-UA file is neither missing a key nor carrying a stale one.
~~6. Docs.~~ **Done.** The `CLAUDE.md` add-a-tool checklist is three steps and no longer mentions RESX at all; a new **Tool Text** section states the rules the analyzer enforces, and the Web UI and translation follow-ups are given as optional rather than as steps 4 and 5. The project table now describes `Dev.Tools.Localization` as translations referenced by the Web UI only, and the generated-artifact list swaps the deleted localization key scaffolding for `ToolDescriptions`. `README.md` no longer claims the tools are localized — the web UI is; the CLI, API and MCP server speak English.

**Phase 1 is complete.** What is left of this document is the Phase 4-6 roadmap and the §1-§7 analysis behind it; delete it once those land.

---

## 4. Source generators — one model, four re-derivations

> **Re-measured 2026-09-12, after Phase 1.** Two of the six rows below are gone: the localization key convention no longer exists, and `LocalizationKeysGenerator` is deleted. The rest still stands.

> **Phase 2 landed 2026-09-12** — the table and recommendations below are kept as the record of what it addressed; §4a says what changed and where the plan was revised.

The generator layer is the project's core bet, and it works, but it has drifted into copy-paste. `ToolGeneratorBase.cs` (491 lines) shares the *symbol reading*; everything downstream is duplicated:

| Duplicated concern | Copies |
|---|---|
| Incremental pipeline / assembly scan | `ApiEndpointGenerator.cs:13-26` ≡ `McpToolGenerator.cs:13-26` ≡ `CliCommandGenerator.cs:13-26`, character-identical apart from the attribute they combine with |
| DTO emission | `ApiEndpointGenerator.GenerateDataType` ≡ `McpToolGenerator.GenerateDataType` |
| Args/Result mapping | `ApiEndpointGenerator.GenerateDataTypeMapping`, `McpToolGenerator.GenerateDataTypeMapping`, `CliCommandGenerator.GenerateMapping` |
| `TypeName.Replace("Tool", …)` naming | **10 call sites** across the three frontend generators, and it is a global `Replace`, not a suffix trim (so `ToolkitTool` → `kit`) |
| ~~Localization key convention~~ | ~~5 independent string concatenations~~ — **gone in Phase 1**; the generators emit the text itself now |
| ~~Catalog/keys generator bodies~~ | ~~`ToolsCatalogGenerator` ≈ `LocalizationKeysGenerator`~~ — **gone in Phase 1**; `LocalizationKeysGenerator` is deleted |

### Recommendations

1. **Extract a shared emit layer, not just a shared read layer.** One `ToolNaming` helper (fix the `Replace`), one `DtoEmitter`, one `MappingEmitter`, one pipeline factory. Each frontend generator should shrink to "which attributes and which method signature".
2. **Consider making the frontend generators consume `ToolDefinition`/the catalog model** rather than re-deriving from `ISymbol`. The model captures `IsRequired`, `IsNullable`, `IsPipeInput`, `IsPipeOutput`, `DefaultValue` and still uses them *inconsistently*: `IsPipeInput` only by CLI, `IsRequired`/`IsNullable` by nobody (everything is emitted as `= default!`). *The worst symptom — the CLI dropping every constructor default while MCP honoured them — was fixed in Phase 0, but the underlying re-derivation is unchanged.*
3. **Diagnose unsupported types instead of failing downstream.** There is no type mapping and no allow-list: `Type = it.Type.ToDisplayString()` (`ToolGeneratorBase.cs:250`) is emitted verbatim, so an exotic property type fails as a C# error inside generated code, an ASP.NET binding failure, or a Spectre `TypeConverter` failure at runtime. Add an allow-list (primitives, `string`, enums, `DateTimeOffset`, `Guid`, arrays/`IReadOnlyList<T>` of those, the future payload type) + a `DTxxxx` diagnostic.
4. **Fail loudly instead of silently skipping.** `GetToolDetails` returns `null` — tool vanishes, no diagnostic — when `[ToolDefinition]` lacks `Name` (`:136-139`) or no 2-arg generic interface is found (`:38-41`). Also `FindToolInterface:222-262` matches **any** 2-arg generic interface, not `ITool<,>` specifically.
5. **Strengthen the analyzer.** DT1001 (unique name), DT1002 (valid name), DT1003 (`public sealed`) and DT1004-DT1007 (description text, added in Phase 1) exist. Missing and worth adding: exactly one `[PipeInput]` (documented as a rule but unenforced), at most one `[PipeOutput]`, `Result` must have a parameterless ctor (required by `ToolBase.Failed1`), tool must implement `ITool<,>`, `Args` must have exactly one public constructor (`GetConstructorDefaultValues:264-314` silently takes the first).
6. **Restore determinism and test coverage.**
   - `Core/CodeBlock.cs:88` embeds `DateTime.Now` in every generated file header → every build re-emits every file; kills incremental-generator caching and makes snapshots noisy.
   - `ToolDetails`/`TypeDetails` keep `ISymbol`/`SyntaxNode` in the incremental model — precisely what `GeneratorTestsBase.AssertObjectGraph:238-241` bans, and the caching test that would catch it is `[Skip("temporary")]`.
   - Snapshots: `CliCommandGenerator` and `McpToolGenerator` have **no tests**; `ApiEndpointGeneratorTests` is `[Skip("Just for now")]` and `ToolsCatalogGeneratorTests`'s caching test is `[Skip("temporary")]` — those are the 2 skips in the suite. Test compilations reference only `typeof(object).Assembly` (`GeneratorTestsBase.cs:98-101`), so snapshots are taken from compilations full of unresolved symbols.
7. ~~**Dead code to remove.**~~ **Done** across Phases 0-1: `ValuesListGenerator`, `GetToolDefinitionDetails`, the unused scan tuple in `CliCommandGenerator`, and `LocalizationKeysGenerator` are all gone. What survives is `ToolsCatalogGenerator`'s `.Combine(assemblyProvider).Select(it => it.Left)`, which pairs the tools with an assembly flag and then throws the flag away.
8. **Name mismatch:** `McpToolGenerator.cs:53` names the file `XMcpTool.g.cs` but the template at `:76` emits `class {TypeName}McpTool` → actual type `HashTextToolMcpTool`. Cosmetic, but it signals the naming logic isn't centralized.


## 4a. Phase 2 progress and decisions

**What landed.** `ToolGeneratorBase` (491 lines) is gone. In its place, each piece has one home:

| Concern | Now | Was |
|---|---|---|
| Symbol reading | `ToolModelFactory` (298 lines) — the only generator code that touches Roslyn symbols | `ToolGeneratorBase`, with the symbols carried onward in the model |
| Model | `ToolModel` / `TypeModel` / `PropertyModel` / `EnumModel` (97 lines), positional records of strings, bools and `EquatableArray<T>` | `ToolDetails : TypeDeclaration`, holding the `ITypeSymbol` and `TypeDeclarationSyntax` |
| Pipeline | `ToolPipeline.RegisterFrontend<TGuard>` / `RegisterSourceTools` (161 lines) | three character-identical copies plus the catalog's own |
| DTO + mapping emission | `ToolEmitter.DataType` / `ToolEmitter.Mapping` (78 lines) | two copies of the DTO, three of the mapping |
| Naming | `ToolNaming` — a suffix trim | 10 global `Replace("Tool", …)` in the generators, 2 more in `Dev.Tools.Localization` (now one shared trim there too) |
| Text helpers | `ToolText` | static members of the generator base class |

The frontend generators are now 99 (API), 161 (MCP) and 214 (CLI) lines, each opening with a one-line `Initialize` naming its guard attribute. The catalog generator is 97. The guard attribute is reduced to a `bool` before it joins the pipeline instead of travelling as `AttributeData`.

**Loud failures.** A `[ToolDefinition]` class the generators cannot read now yields `DTG1001` naming the class and the reason (no `Name`, no `ITool<,>`), and a frontend switched on but finding no tools yields `DTG1002` — previously CLI-only, now all three. The interface lookup matches `Dev.Tools.ITool<,>` by identity, not "any generic interface with two type arguments".

**Analyzer rules added** (reported where the author is typing, which the generator diagnostics cannot do — those run in the consuming assembly):

- **DT1008** the class must implement `ITool<TArgs, TResult>`, directly or through a base class.
- **DT1009** at most one `[PipeInput]`. *Revised from "exactly one":* six tools legitimately have none — `ip-details`, `lorem-ipsum`, `passphrase-generator`, `token-generator`, `ulid-generator` and `uuid-generator` take no primary input.
- **DT1010** at most one `[PipeOutput]` (`ip-details`, `jwt-parser` and `url-parser` have none, correctly).
- **DT1011** `Args` has exactly one public constructor. Applied to `Args` only: the documented `Result` shape pairs the positional constructor with a parameterless one, and result defaults are never read.
- **DT1012** (warning) argument types must be bindable: string, bool, char, numeric primitives, decimal, enums, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Uri`, nullable forms of those, and arrays or `IList`/`List`/`ICollection`/`IEnumerable`/`IReadOnlyList`/`IReadOnlyCollection` of them. `Result` types are exempt — they are serialized, never bound, and `char-viewer` returns `IList<CharInfo>`. A warning rather than an error so an unlisted-but-working type is not blocked; it names the cause where the generated code would otherwise fail obscurely.
- *Dropped:* "`Result` must have a parameterless constructor". `ToolBase<TArgs, TResult>` already constrains `TResult : new()`, so the compiler enforces it for every tool that uses the base class, and a tool implementing `ITool<,>` directly never calls `Failed1`. The rule would have duplicated a compiler error.

**Determinism.** The generation timestamp is gone from `CodeBlock`'s header. Verified: all 70 generated files are byte-identical across a `--no-incremental` rebuild, and `ToolsCatalogGeneratorTests.ShouldReuseItsCachedOutputWhenNothingChanged` — the caching test that was `[Skip("temporary")]` — passes. It could never have passed as written: its helper asserted the *first* run's outputs were cached, which a first run never is.

**Tests.** Generator tests now compile a stand-in Dev.Tools library (`Fixtures/SampleTools.cs`: a positional `Args` with defaults, a described enum, piped input and output, a nullable result and an object-initializer result) and hand it to the frontends as a metadata reference, the way the real library reaches them, against real platform references rather than CoreLib alone. `CliCommandGenerator` and `McpToolGenerator` have snapshots for the first time; `ApiEndpointGenerator`'s is un-skipped; each frontend also asserts it emits nothing without its assembly attribute. `Verify` now fails when a generator emits nothing — without that, all three frontend snapshots passed on empty output while the fixture was unreadable (an `[assembly:]` attribute placed after a namespace does not bind). The orphaned snapshot of the long-deleted `GenerateApiEndpointsAttribute` is removed. Suite: **55 passed, 0 skipped** (was 38 passed, 2 skipped). Analyzer tests grew from 19 to 29.

**Regression check.** `HEAD` was built from a clean `git archive` and its generated code diffed against this tree for all 22 tools across API, MCP, CLI and the catalog. The only differences are the header line in every file and the 22 MCP holder classes, now named as their files are (`HashTextMcpTool`, was `HashTextToolMcpTool`). The MCP host discovers them with `WithToolsFromAssembly()`, so the name is not referenced anywhere.

**Not done, deliberately.** Recommendation 2 — frontends consuming the catalog model rather than re-deriving it — is unchanged: `IsRequired`/`IsNullable` are still unused and DTO properties are still emitted as `= default!`. Doing it changes emitted code for every tool, which this phase set out to avoid so the refactor could be verified as behaviour-neutral. It fits naturally with Phase 5, when `ToolContent` forces per-frontend type mapping anyway.

---

## 5. Core contract — the seams the future features need

### 5a. Error model

> **Done in Phase 3 step 5.** A result carries `IReadOnlyList<ToolError> Errors`, each a `Code`, an optional `Target` (the `Args` property at fault) and the English `Message` authored on the code; `ErrorCodes` and `Failed1` are gone. `ToolException` takes one error or several, so the encryptor and decryptor report a missing text *and* a missing key together; every throw site that is about one argument names it - all but the three that are not (a failed decryption, a failed encryption, and the hasher's `Unknown`). The misused codes are fixed: the encryptor reports the new `FailedToEncrypt`, and the hasher's `CryptographicException` - which no input can cause - is `Unknown`. The catalog's error-code scan now also finds codes collected into a list and thrown from switch arms. Frontends: the CLI prints `text-encryptor: --key: Input is not valid`, one line per error, exit 1; the API answers 400 with `ToolProblemDetails` - the standard RFC 9110 problem type plus an `errors` list of code, target (spelled as in the JSON body) and message, typed in the OpenAPI document - and 500 for `Unknown`; MCP raises `text-encryptor failed: text: Text cannot be empty; key: Input is not valid`; the Web pages read `Errors`. Format arguments were deliberately left out: none of today's codes needs one.
>
> **No exception detail reaches a client.** Only authored messages, codes and argument names are exposed. The generated API endpoint used to `catch (Exception e)` and return `e.Message`; it no longer catches, and `AddProblemDetails` + `UseExceptionHandler` log the exception and answer with a detail-free 500. The CLI no longer prints a stack trace for a failure outside a tool - it prints `<tool>: the tool failed unexpectedly`, or `cancelled` with exit 130. The MCP SDK only forwards `McpException` messages, and ours carry authored English only.

`ToolResult.ErrorCodes` is an `IList` but only ever holds one code: `ToolBase.Failed1` (`ToolBase.cs:28-31`) creates the result and returns. An error carries **no target field, no message, no payload** — `ErrorCode` is a flat 7-value global enum, which already forces semantic abuse: the *encryptor* returns `FailedToDecrypt` (`TextEncryptorTool.cs:54`) and so does the *hasher* (`HashTextTool.cs:38`). `JsonFormatterTool` discards the parser's line/offset (`:18,28`).

Recommend: `ToolError(ErrorCode Code, string? Target = null, object?[]? Args = null)` and a result that can carry several. Codes stay culture-neutral (that is exactly what makes Web-only localization work — Web maps code → resx, as it already does at `JwtParserToolPage.razor:15`, the only place in the solution where an `ErrorCode` is shown to a human). `Args` lets Web format `"Value must be between {0} and {1}"` without core holding strings.

Also fix the frontends' error handling, which is inconsistent and in two cases absent:

- **CLI ignores `HasErrors` entirely** — prints the (empty) pipe-output and **exits 0** (`ToolResponseHandler.cs:8-25`). This is the most user-visible bug in the repo: `dev-tools base64-decoder "!!!"` reports success in a shell pipeline.
- **MCP has no error path at all** (`McpToolGenerator.cs:252-262`) — a failed tool returns an empty response with no error signal to the model.
- **API** keeps only `ErrorCodes[0]` and puts an enum name in the ProblemDetails `type` field, which is specified to be a URI (`ApiEndpointGenerator.cs:99`).

### 5b. Cancellation

~~`CancellationToken` is threaded through the signature but honoured in exactly one place.~~ **Done** — the `OperationCanceledException` rethrow in Phase 0, the rest in Phase 3 step 3. `ToolBase.ExecuteAsync` checks the token before running, so an already-cancelled call does no work in any tool, and passes it to a new `Execute(TArgs, CancellationToken)` overload that defaults to `Execute(TArgs)` — the 17 tools with nothing worth interrupting are untouched. The four `Parallel.For` generators (`passphrase-generator`, `token-generator`, `ulid-generator`, `uuid-generator`) override it and hand the token to `ParallelOptions`. `CancellationTests` covers both halves: a pre-cancelled token against all four plus a tool that does not override, and a 20 000 × 4 096-character token run cancelled after 25 ms, which only the token inside `Parallel.For` can stop.

### 5c. Determinism / testability

~~Randomness and time were read directly, so tests could assert shape but never values.~~ **Done in Phase 3 step 4.** `IRandomSource` (`Next(int)`, `Fill(Span<byte>)`) sits next to `IMd5Hash` in `Dev.Tools.Cryptography`, with `CryptographicRandomSource` as its default, and BCL `TimeProvider` covers the clock. Every direct call is gone: `Guid.NewGuid()` (UUID v4, now built from 16 drawn bytes), `DateTimeOffset.UtcNow` and `RandomNumberGenerator.Fill` (UUID v7), `Ulid.NewUlid()` (now `Ulid.NewUlid(clock, randomness)`), `new Random()` (Lorem Ipsum), `RandomNumberGenerator.GetInt32` (passphrase and token generators), `DateTimeOffset.Now` (date-convert with no input), and the text encryptor's AES-CBC and Triple-DES IVs and AES-GCM nonce. Tools take them as optional constructor parameters — the container injects a host's registrations, `AddDevTools` registers the defaults with `TryAdd`, and the 100 existing `new XTool()` test call sites compile unchanged. Tests now assert exact values through `ScriptedRandomSource` and `FixedTimeProvider`. That surfaced a real bug: **UUID v7 never set the RFC 9562 variant bits**, so about three values in four carried the wrong variant; fixed and pinned by a zero-bytes test that failed before the fix. The live-network half of this item was already dealt with in Phase 0 (`IpDetailsToolTests` uses a stub handler).

### 5d. External connectors (the future feature)

> **Postponed 2026-09-12 and moved to its own document: [docs/proposals/connectors.md](proposals/connectors.md).** It is a feature proposal rather than part of this review, and it should outlive this document. It holds the purpose (a generic HTTP transport plus a per-service contract), what is still wrong with `ip-details`, the draft proposal, and the questions to answer when a second tool needs an external service.

### 5e. File/stream payloads (the other future feature)

Everything is `string` today, end to end: `Args`/`Result` hold whole payloads, and `StdinInterceptor.cs:31` does `Console.In.ReadToEnd()` and only assigns when the property is `typeof(string)` (`:38`) — so a non-string pipe input silently ignores stdin.

> ~~**Also fix here (found in Phase 3 step 4, deliberately left for this phase):** `StdinInterceptor` reads stdin whenever input is redirected, even when the pipe-input argument was passed on the command line.~~ **Done in Phase 5 step 1** — see §5e-progress below.

#### §5e-progress. Phase 5 scope and decisions (agreed 2026-09-12)

**The principle: a tool does not care where its data came from.** Typed on the command line, piped, uploaded, loaded from a file, or — later — produced by the previous step of a [tool flow](proposals/tool-flow.md). Loading files is each frontend's business, and every frontend has its own way to do it, so every frontend offers file input for the `[PipeInput]`, whatever its type - the CLI and the API for every tool, the Web UI only where a file makes sense for the tool (a document or encoded data, not a number, a date or a URL):

- a `string` pipe input (`json-formatter`, and most tools) receives the file's text — the tool is unchanged;
- a `ToolContent` pipe input receives the bytes or a stream — for tools whose data is not text or not whole (certificates, hashing a large file, Base64 of an image).

Decisions:

- **Stdin trailing newline:** trimmed for `string` pipe inputs, as today (`echo hello | hash` stays convenient); **not** trimmed for `ToolContent`, matching `sha256sum` and `base64`.
- **API file input:** `multipart/form-data` on the same route, the file as a part and the other arguments as form fields, with an explicit size limit. The JSON body stays.
- **MCP:** text only; no file paths, which on the HTTP transport would let a client read any file on the server.
- **`ToolContent` only on the `[PipeInput]`** (an analyzer rule), so each frontend has exactly one file slot.
- **Binary output is a later step.** Returning decoded bytes as Base64 inside JSON makes no sense for a Base64 decoder; decide raw `application/octet-stream`, text-when-UTF-8, MCP blob resources and a Web download once the input side is in. **The output format is the client's choice, not the tool's** (agreed 2026-09-13): the tool returns the bytes; the CLI gets an output-format option; the API and MCP follow each protocol's standard, recommended way of returning binary content.
- `[PipeInput]`/`[PipeOutput]` are the contract the future tool flow connects tools through, so no frontend may reshape a pipe value — see [docs/proposals/tool-flow.md](proposals/tool-flow.md).

Planned as: (1) CLI output and stdin fixes, no new type — **done**; (2) `ToolContent` input, file input across the frontends, `hash` and `base64-encoder` as the reference tools — **done**; (3) binary output, `base64-decoder` as the reference — later.

**Step 1 — done.** Two CLI bugs, both found by running it:

- **Piped output was wrapped at 80 columns.** `ToolResponseHandler` printed results with Spectre's `AnsiConsole.WriteLine`, which wraps to the console width — 80 columns when stdout is redirected. Anything longer than a line gained newlines on the way through: 3 000 bytes Base64-encoded came out as 50 lines, and `base64-encoder < file | base64-decoder` returned the file with a newline every 80 bytes (the system `base64 -d` decoded the same encoder output correctly, so the damage was in the printing). Results now go straight to `Console.Out`; Spectre still renders errors and help. The round trip now returns the file plus the one trailing newline the CLI prints.
- **Stdin was read even when the argument was given**, so a given argument was replaced by piped data (`echo x | dev-tools base64-encoder hello` encoded `x`) and every command hung wherever stdin is redirected but never written to. The argument now wins, and stdin is read only when it is missing. Verified with `(sleep 25) | timeout 20 dev-tools base64-encoder hello`, which printed `aGVsbG8=` and exited 0 before the timeout; it used to hang.

Neither fix had an automated test when it landed; step 2 added the Console test project, and both are covered there.

**Step 2 — done.** File and binary input, end to end:

- **`ToolContent`** (core): text, bytes, or a stream opened when the tool reads it. `ReadAllBytesAsync`, `ReadAllTextAsync` (UTF-8, a byte order mark honoured, invalid bytes become U+FFFD rather than failing) and `OpenRead()`. A string converts implicitly, so every existing caller and test compiles unchanged.
- **`hash` and `base64-encoder`** take it. The SHA family hashes as it reads (`IncrementalHash`), so a file of any size is never held in memory; MD5 reads the content whole, because `IMd5Hash` hashes a buffer and the WebAssembly host substitutes a managed implementation that works that way. Empty content from any source is `TextEmpty`, as empty text was. Text hashes and encodes exactly as before.
- **CLI:** `PipeInputReader` replaces `StdinInterceptor` and its reflection over settings. A string pipe input is read whole and trimmed as before; content is streamed from stdin untrimmed, so `dev-tools hash < file` matches `sha256sum file`. Only commands with a pipe input take the reader.
- **API:** the JSON route is unchanged. Every tool whose pipe input is a string or content also gets `POST /tools/{name}/file`, multipart/form-data, the file as the pipe input's part and the other arguments as form fields; a string tool gets the file's text. A request over 10 MB (`EndpointBase.MaxUploadBytes`) is refused while the form is read, before the tool runs. The upload was first put on the JSON route with a second content type: routing told the two apart, but the OpenAPI document keeps one operation per path and method and the JSON body vanished from it, hence the separate route. The refusal is a 400 validation problem whose message is the framework's ("Request body too large. The max request body size is 10485760 bytes.") rather than a 413 — it names nothing internal, so it was left.
- **MCP:** the parameter stays a string; text only.
- **Generators:** `PropertyModel.IsContent` and `BindingType` (the type a frontend binds - `string` for content); `ToolEmitter.ArgumentValue` wraps text into content.
- **Analyzer:** `DT1013` — `ToolContent` only on the `[PipeInput]` argument; `DT1012` accepts it.
- **Web:** `DtTextField` gains `AllowFileInput` - a load-file button whose file arrives as if typed, capped at the field's `MaxLength` - and `OnFileLoaded`, which hands a page the file's bytes as `ToolContent` instead, up to 10 MB. Unlike the CLI and the API, the Web UI offers it only where a file makes sense - `base64-encoder`, `base64-decoder`, `hash`, `json-formatter`, `xml-formatter`, `text-encryptor`, `text-decryptor` - and not for short values such as a number, a date, a URL or a JWT; `hash` and `base64-encoder` take the bytes. Built, **not exercised in a browser**; that `IncrementalHash` runs under WebAssembly is also unverified.
- **uk-UA:** the four tool strings whose English changed are retranslated.

Found on the way: **API request DTOs dropped every argument default.** They were emitted with `= default!`, so a JSON body that left an option out ran the tool with `default(T)` - the same bug the CLI had before Phase 0. The DTOs now carry the tool's own defaults; `POST /tools/token-generator {}` returns a 15-character token.

Verified: 7 Console tests (argument wins without touching stdin, trimming, untrimmed content, unwrapped output), `ToolContentTests`, binary and stream cases for both tools, two `DT1013` analyzer tests, re-verified generator snapshots. Live: CLI hash and Base64 of a 200 KB random file against `sha256sum`, `md5sum` and `base64 -d`; API JSON and multipart uploads, OpenAPI showing both operations, the size limit and a missing file; MCP over stdio.

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

- `ToolDefinitionAttribute` is `internal`, so third-party assemblies cannot define tools. **Deferred in Phase 3 — tools from outside Dev.Tools are not a goal of this project.** The extensions and hosts on the roadmap reuse Dev.Tools' own tools. Should it ever become one, a public attribute alone would not be enough, and the earlier note here that the generators "already can" run in a consuming assembly holds only for the catalog generator: the NuGet package does not ship the generators or the analyzer (the `Dev.Tools.CodeAnalysis` reference is `PrivateAssets="all"` and not packed), the frontend generators only read an assembly named `Dev.Tools` (`ToolPipeline.RegisterFrontend`), and `ToolsProvider` / `AddDevTools` only read Dev.Tools' own internal `ToolsCatalog`.
- ~~`IToolsProvider.GetTool<T>()` is generic-only and calls `ActivatorUtilities.CreateInstance<T>`, bypassing the `AddTransient` registrations.~~ **Done in Phase 3 step 1.** `IToolsProvider` now declares only non-generic lookups — `GetTool(Type)`, `GetTool(string nameOrAlias)`, `GetToolDefinition(Type)`, `GetToolDefinition(string nameOrAlias)` — and `GetTool<T>()` / `GetToolDefinition<T>()` became extension methods over them, in the `Dev.Tools` namespace so the Web pages and generated code need no new using. Tools resolve through the container, so the registrations `AddDevTools` makes are the ones used and a host's substituted dependencies are honoured (tested with a counting `IMd5Hash`).
- ~~`ToolsProvider` doesn't index aliases, so a programmatic lookup by alias fails.~~ **Done in Phase 3 step 1**, and the premise here was wrong: `dev-tools 64e` did **not** work — the CLI generator never registered aliases and Spectre answered "No such command". The generator now calls `WithAlias` for each one. Names and aliases are indexed together; DT1001 now covers aliases (a collision is a build error, reported at both spellings), aliases are exempt from DT1002's name format (`64e` starts with a digit), and the provider reports any collision by name instead of the frozen dictionary's duplicate-key error.
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
| 18 | Review items #10-#13 (generated-header `DateTime.Now`, stale `ToolIconProvider`/RESX entries, csproj metadata, `TextEncryptorTool` key derivation) were deliberately left for Phases 2-3 as planned. | — | **#10 fixed** in Phase 2 (no timestamp in generated headers). **#11 fixed 2026-09-13**: the `rsa-generator` icon entry is gone, and the neutral Web `Resources.resx` — all 108 of its values `:placeholder:`s, 36 keys behind en-US — now holds the English for all 144 keys, kept that way by `WebResourcesTests`. **#12**: the missing `Resources.Designer.cs` was stale (both designer files exist); the `.slnx` backslash path is fixed. **#13**: the `FailedToDecrypt` misuse was fixed in Phase 3 step 5; the bare SHA-256 key derivation is **still open** — changing it breaks decryption of existing ciphertext, so it needs its own decision. |
| 19 | **The CLI ignored options it did not know, and spelled multi-word options in camelCase** (found 2026-09-13). Spectre's default relaxed parsing set an unrecognised option or argument aside, so `html-entities --encode-non-ascii` did nothing and exited 0; and the generator lowercased only the first letter of the property name, so the option was `--encodeNonAscii` — 21 of 44 options, and the README's `json-formatter --sort-keys` silently did not sort. | `Console/ServiceCollectionExtensions.cs`, `CliCommandGenerator.cs`, `Console/Core/ToolResponseHandler.cs` | **Fixed** — strict parsing, so an unknown option or a stray argument fails before the tool runs, with exit code 2 and Spectre's message (pointer to the bad argument included) on stderr - Spectre printed it to stdout, where the next command in a pipe would have read it; options and the pipe-input argument are kebab-case (`--encode-non-ascii`), through one `CliNaming` compiled into both the generator and the CLI, so an error names an argument the way it is typed. The camelCase spellings are not kept as aliases: with strict parsing they fail loudly. |

### Also removed in this pass

Dead generator code, as planned: `ValuesListGenerator` (its `[Generator]` was commented out) with its `GenerateValuesAttribute` definition, skipped test and snapshots, and the DT2001 row in `AnalyzerReleases.Unshipped.md`; the unused `GetToolDefinitionDetails` helper; the commented-out inert `.Where` guard in `ToolsCatalogGenerator`; and `CliCommandGenerator`'s leftover debug-scaffolding pipeline (6-field tuple whose values were never used), now matching the clean `ApiEndpointGenerator` shape while keeping the attribute gate that suppresses DTG1002 for assemblies that don't opt in.

---

## 8. Recommended sequencing

Each phase is independently shippable.

**Phase 0 — correctness, no design change. ✅ Done 2026-09-11.** §7 items 1-9 + dead generator code, plus five defects found only by building and running (§7.14-§7.17), plus the CLI-defaults fix pulled forward from Phase 2 and the cancellation rethrow pulled forward from Phase 3.

**Phase 1 — English metadata in core; translation limited to user-facing frontends. ✅ Done 2026-09-12.** See §3a for what landed in each of the six steps. Adding a tool is now one source file plus one test file: the RESX chore is gone from the checklist entirely, and with it the static localization provider, the second DI container, the neutral placeholder RESX and the build-time write into a sibling project. Four analyzer rules keep the text in place. Two things were found only by running the frontends rather than reading the generated code — MCP published no output schema at all, and JSON Schema carries no per-enum-value documentation — and both are fixed.

**Phase 2 — generator consolidation. ✅ Done 2026-09-12.** See §4a. One pipeline, one model with no Roslyn objects in it, one emitter, one naming helper; five new analyzer rules and two generator diagnostics in place of silent skips; deterministic output; every generator snapshot-tested and the skipped caching test passing. Output for the 22 real tools is unchanged apart from the header timestamp and the MCP holder class names. Recommendation 2 (frontends reading the catalog model) was deliberately left for later.

**Phase 3 — contract hardening. ✅ Done 2026-09-12.** `ToolError` with target/args and multiple errors; cancellation honoured and `OperationCanceledException` rethrown; `TimeProvider` + `IRandomSource` seams; `IToolsProvider.GetTool(Type/name)`; alias index; ~~`ToolDefinitionAttribute` public~~ (deferred — external tools are out of scope, see §5f).

*In progress.* Planned as five commits: (1) type and name-or-alias lookups — **done**, see §5f; (2) public `ToolDefinitionAttribute` — **deferred**, external tools are out of scope; (3) cancellation honoured inside the four `Parallel.For` generators — **done**, see §5b; (4) `TimeProvider` + `IRandomSource` — **done**, see §5c; (5) `ToolError`, last because it is the only step that changes what the frontends return — **done**, see §5a.

**Phase 4 — connectors. ⏸ Postponed 2026-09-12.** Only one tool (`ip-details`) talks to an external service, which is too little to design the connector split on — a generic transport (address, auth, timeouts) versus a per-service contract. See [docs/proposals/connectors.md](proposals/connectors.md) for the context, what is still wrong with `ip-details`, the draft proposal and the open questions. Resume when a second tool needs an external service.

**Phase 5 — `ToolContent` payloads.** Core type + implicit string conversion; generator binding per frontend; CLI stdin streaming, including the stdin-overrides-argument bug noted in §5e; migrate hash/base64 as reference cases.

*In progress.* Scope and decisions agreed 2026-09-12 (§5e-progress): every frontend offers file input for every `[PipeInput]`, `string` tools get the text and `ToolContent` tools the bytes. Planned as three steps: (1) CLI output wrapping and stdin-overrides-argument fixes — **done**; (2) `ToolContent` input with `hash` and `base64-encoder` as reference tools — **done**; (3) binary output with `base64-decoder` — later.

**Phase 6 — generic Web page + dependency-light core.** Metadata-driven `ToolPage` with per-tool UI hints and hand-written escape hatch; `Icon` in `[ToolDefinition]`; strip `Microsoft.Extensions.*` from core and move DI wiring to its own package — the precondition for VS Code / browser-extension targets.

---

## 9. Verification (per phase)

- `dotnet build` — must be warning-clean; confirm generated files under `src/*/obj/**/generated/` no longer churn between two consecutive builds (Phase 2).
- `dotnet test` — all TUnit suites; generator snapshot tests un-skipped and committed; network-dependent tests moved behind a category filter (Phase 4, postponed; none exist today).
- CLI: `echo "Hello" | dev-tools base64-encoder | dev-tools base64-decoder`; `dev-tools base64-decoder "!!!"; echo $?` must be non-zero (Phase 0); `dev-tools hash --help` shows real English descriptions (Phase 1).
- API: `docker run` or `dotnet run`, check `/ui` Scalar doc renders descriptions, and `POST /tools/base64-decoder` with bad input returns a well-formed `ProblemDetails` (Phase 3).
- MCP: call a tool over stdio, plus an intentionally-failing input, to confirm errors surface (Phase 0/3).
- Web: `dotnet run` the Blazor app, switch culture en ↔ uk-UA, confirm no `:placeholder:` strings appear and each tool page still renders (Phase 1, 6).
