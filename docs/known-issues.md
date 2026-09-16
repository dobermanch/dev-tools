# Known issues

Open defects with enough detail to be picked up in a fresh session. Delete an entry when it is fixed.

## IP Details takes the Web app down (DI lifetime), 2026-09-15

**Symptom.** Opening `/tools/ip-details` in the Blazor WASM app throws before anything renders, and the
unhandled-error bar appears at the bottom of the page. No HTTP request is ever made.

```
crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
System.InvalidOperationException: ScopedResolvedFromRootException, Dev.Tools.Tools.IpDetailsTool, System.Net.Http.HttpClient, scoped
  at Dev.Tools.Providers.ToolsProvider.Resolve(ToolDefinition definition) ToolsProvider.cs:77
  at Dev.Tools.ToolsProviderExtensions.GetTool[IpDetailsTool](IToolsProvider provider) ToolsProviderExtensions.cs:13
  at Dev.Tools.Web.Pages.Tools.IpDetailsToolPage.OnInitializedAsync() IpDetailsToolPage.razor.cs:20
```

**Cause.** `AddDevTools` registers `IToolsProvider` as a **singleton**, so `ToolsProvider` captures the
**root** service provider and resolves every tool from it (`ToolsProvider.Resolve`). `IpDetailsTool` is
transient and takes an `HttpClient`, which `Dev.Tools.Web/Program.cs` registers as **scoped**. Blazor
WebAssembly leaves scope validation on, so the container refuses the resolution. Nothing to do with the
lookup itself: ipify allows the cross-origin GET (`access-control-allow-origin: *`, and a plain
`GetAsync` is a simple request, so no preflight).

It is the only tool with a scoped dependency, which is why it is the only one that fails. The API, CLI
and MCP server register no `HttpClient` at all, so there the constructor's optional parameter falls back
to the tool's own static client and everything works.

**Fix, in order of preference.**

1. **Use `IHttpClientFactory`.** `builder.Services.AddHttpClient()` (package `Microsoft.Extensions.Http`)
   registers the factory as a singleton; hand tools a client from it rather than a scoped one:
   `services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient())`. A transient
   resolves from the root provider without complaint, handler lifetime stops being a static field's
   business, and any future tool that takes an `HttpClient` is covered. Check what the Web app's own
   `HttpClient` registration (base address = host environment) is used for before changing its lifetime -
   it is the one the framework hands to components for fetching app assets.
2. Failing that, register the Web's `HttpClient` as a singleton: in WebAssembly there is a single user and
   a single scope, and the client is a thin wrapper over `fetch`. The template's scoped registration is a
   server-side convention (one client per circuit).
3. Making tool resolution scope-aware (`IToolsProvider` scoped) cascades: `WebContext`,
   `ToolsSearchProvider` and the localization provider are singletons holding it, so each would have to
   become scoped too, in every frontend.

**Two further defects on the same page, worth fixing in the same pass.**

- **A tool must report a failure, not return empty.** `IpDetailsTool.GetIpAsync` swallows every exception
  and returns `null`, so a total lookup failure is indistinguishable from success with no address. It
  should return a `ToolError` when *both* lookups fail - a new `ErrorCode` (`ServiceUnavailable`) with its
  English `[Description]` and a `Enums.ErrorCode.*` entry in
  `Dev.Tools.Localization/Resources/ToolResources.uk-UA.resx`, which `LocalizationTests` checks per
  culture. `IpDetailsToolTests.WhenLookupFails_ShouldReturnNullAddresses` asserts today's silent nulls and
  changes with it.
- **The page blocks on the network.** `IpDetailsToolPage.OnInitializedAsync` awaits the lookup before the
  first render, and `api6.ipify.org` does not answer at all from some networks, so the page stays blank
  for the full 10-second timeout. It should paint first, look up after the first render with a loading
  state, and show the error with a retry rather than letting anything escape into the renderer.

**The rule this broke.** A tool returns `ToolError`s; it does not throw. `ToolBase` already turns a stray
exception into `ErrorCode.Unknown`, so the tool itself was never the problem here - the throw came from
*resolving* the tool, outside that guard, straight into a component lifecycle method. Resolution belongs
inside the same guard as execution.
