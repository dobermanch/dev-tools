# Proposal: connectors to external services

**Status:** postponed (2026-09-12). Resume when a second tool needs an external service.

## Why it is postponed

`ip-details` is the only tool that talks to an external service. One example is not enough to tell which parts of a connector are generic and which belong to one service, so a design now would be a guess. The context and the analysis done so far are recorded here so they do not have to be redone.

## What the feature is for

Some tools depend on an external service, and the service should be replaceable. A connector (or provider) is how a tool reaches it. It has two layers:

- **A generic transport**, the same for every connector that speaks HTTP: address, authentication (basic, bearer token, API key), timeouts. This part can be written once and configured per connector.
- **A service contract**, unique to each service. `ip-details` calls ipify, which answers with a bare IP address in the body. Another service offering the same lookup may answer with JSON in a different shape, and may require authentication. The tool, or an adapter per service, has to know that contract to get the data out. This part cannot be generic.

So making the endpoint configurable is only half of it: pointing `ip-details` at a different service is useless unless something also knows how to read that service's response. The design question is how a tool names the capability it needs ("public IP lookup"), how providers implementing it are registered and chosen by configuration, and how the generic HTTP layer is shared between them.

## State of `ip-details` today

Already fixed: the per-request `new HttpClient()` (now one shared client with a 10 s timeout, or an injected one), swallowed cancellation (rethrown), and a unit test that called the live network (now hermetic, over a stub `HttpMessageHandler`).

Still open:

- **The ipify endpoints are hardcoded**, and ipify's contract (a plain-text body) is baked into the tool.
- **Failures are invisible.** An outage, a timeout and a non-IP response all return `{ IpV4: null, IpV6: null }` with no errors. `HttpClient.Timeout` throws `TaskCanceledException`, which the generic catch turns into `null`, so a timeout cannot be told apart from "no IPv6 on this network".
- **The tool takes a plain `HttpClient` from the container.** The Web host registers a scoped `HttpClient` whose base address is the site itself, for its own use, and DI hands that client to `IpDetailsTool`. It works only because the ipify URLs are absolute. Any host registering an `HttpClient` for its own purposes silently gives it to every network tool. Resolving a scoped client through the singleton `ToolsProvider` may also fail Development-mode scope validation in WASM (not verified).

## A related problem that does not have to wait

**The MCP server emits no tool annotations, and the spec defaults are the worst case**: `readOnlyHint: false`, `destructiveHint: true`, `openWorldHint: true`. All 22 tools, base64 included, currently tell a model they may be destructive and may reach the outside world. `McpServerToolAttribute` (ModelContextProtocol 2.2) exposes `ReadOnly`, `Destructive`, `Idempotent` and `OpenWorld`. This can be fixed on its own, ahead of connectors — see item 5 below.

> **Fixed in the interim (2026-09-13).** The MCP generator now emits `ReadOnly = true` and `Destructive = false` for every tool — none has side effects — and `OpenWorld = true` only for a tool whose constructor takes an `HttpClient` (today just `ip-details`). That is read from the code rather than from `Category.Network`, which is about what a tool is for: an IP range or CIDR calculator belongs there and never leaves the process. When connectors bring their own seam, its type joins the check; declared capabilities (item 5) may replace it.

## Draft proposal

Agreed as a direction in the 2026-09-12 discussion, but it covers only the generic transport half. Revisit it against the two-layer framing above before building.

1. **An HTTP seam that is not a raw `HttpClient`**, e.g.

   ```csharp
   public interface IHttpClientSource { HttpClient GetClient(string name); }
   ```

   defaulting to one shared plain `HttpClient` (not `SocketsHttpHandler`, which does not run in the browser). Hosts with `IHttpClientFactory` (API, MCP) adapt it in one line — `name => factory.CreateClient(name)` — and get named clients plus the factory's request logging, which is where an operator sees the status code and URL the client never does. `HttpClient` stays the transport: it is the BCL abstraction, runs on `fetch` in WASM, and `HttpMessageHandler` is already the test seam. So no `IHttpRequester` that rebuilds it, and no `Microsoft.Extensions.Http` dependency in core. A tool takes it as an optional constructor parameter, like `IRandomSource` and `TimeProvider`.

2. **Options on the host, never in `Args`.** Endpoints and credentials are per-deployment settings. As arguments on the API or MCP server they would let any caller make the server fetch any URL (SSRF) or supply its secrets. Each tool or connector gets an options record with built-in defaults (for `ip-details`: `IpV4Endpoint`, `IpV6Endpoint`, `Timeout = 10s`). The catalog records the options type, and core offers an overload so a host binds every tool from configuration in one line, without core depending on `Microsoft.Extensions.Configuration`:

   ```csharp
   .AddDevTools(tool => builder.Configuration.GetSection($"DevTools:Tools:{tool.Name}").Get(tool.OptionsType))
   ```

   Adding a tool then needs no host change. Bad configuration (a relative URI, say) fails at startup, not on the first call. API and MCP would bind from configuration; Console and Web would use the defaults.

3. **The tool owns its timeout**, through a linked `CancellationTokenSource` with `CancelAfter(options.Timeout)`, so its own timeout becomes an error while the caller's cancellation is still rethrown.

4. **Network failures become `ErrorCode`s**: `ConnectorUnavailable` ("The external service is unavailable") and `ConnectorTimeout` ("The external service did not respond in time"), with uk-UA translations. They carry no target — no argument is at fault — and no status code, URL or exception text reaches the client. For `ip-details`, one address family failing stays a documented `null` (many networks have no IPv6); it is an error only when neither address could be found — `ConnectorTimeout` if every attempt timed out, `ConnectorUnavailable` otherwise, including a response that is not an IP address.

5. **Capability metadata.** `[ToolDefinition(..., Capabilities = [Capability.Network])]` flowing into `ToolDefinition.Capabilities`, an array like `Categories` and `Keywords`. The MCP generator emits `ReadOnly = true` for every tool and `OpenWorld` from `Network`, on the contract that a tool with side effects must declare a capability for them (none exists today). An analyzer rule makes it checkable: taking the HTTP seam without declaring `Network` is a build error. Capabilities also let a host exclude network tools (an air-gapped API) or the Web UI badge them.

Suggested order, one commit each: (1) capability metadata and MCP annotations; (2) HTTP seam, network error codes, `ip-details` migration and the analyzer rule; (3) options and configuration binding in the API and MCP hosts.

## Questions to answer when this resumes

- How a tool asks for a capability rather than a service, and how the provider behind it is chosen: a configuration key, a default provider shipped with the tool, several providers with fallback?
- Where the response contract lives: in the tool, or in a provider adapter the tool does not know about.
- Credentials: where they are configured, how they are kept out of errors and logs, and what happens in the Web host, where anything configured is visible to the browser (plus CORS on the external service).
- Whether the generic layer is HTTP-only or also covers other transports.
