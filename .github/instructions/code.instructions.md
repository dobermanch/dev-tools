---
applyTo: 'src/**'
---
# Coding Instructions

This solution is written in **.NET/C#** and includes:

- Core library of developer tools (e.g., GUID generation, Base64 encoding/decoding)
- Console application
- Web API
- Blazor frontend
- MCP server
- Test projects

## C# Code Style

Follow the [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

### Braces & Blocks

- Place opening brace `{` on its own line (Allman style)
- Always use braces `{}` for `if`, `else`, `for`, `foreach`, `while`, and `do` bodies — even single-line ones
- Exception: expression-bodied members (`=>`) are preferred for simple one-liners

```csharp
// correct
if (condition)
{
    DoSomething();
}

// correct — expression body
public string Name => _name;

// wrong — omitted braces
if (condition)
    DoSomething();
```

### Naming

| Element | Convention | Example |
|---|---|---|
| Classes, records, enums, methods, properties | PascalCase | `ToolBase`, `RunAsync` |
| Interfaces | `I` + PascalCase | `IToolsProvider` |
| Private fields | `_camelCase` | `_toolDefinition` |
| Parameters & locals | camelCase | `cancellationToken` |
| Constants | PascalCase | `MaxRetryCount` |

### `var` Usage

Use `var` when the type is apparent from the right-hand side; use explicit types otherwise.

```csharp
var result = new ToolResult();          // type is obvious
var items = GetItems();                 // avoid — type unclear
IReadOnlyList<string> items = GetItems(); // correct
```

### General

- Prefer `is` pattern matching over casts: `if (obj is MyType t)`
- Use `string.IsNullOrEmpty` / `string.IsNullOrWhiteSpace` over manual null checks
- Use collection expressions (`[...]`) where supported (C# 12+)
- Avoid static state unless explicitly required
- Use `ILogger<T>` for logging; avoid hardcoded console output in production
- Prefer `IOptions<T>` for configuration binding
- Be concise — create abstractions only when they improve clarity or reuse

## Project Structure & Architecture

- Group related features by domain, not by layer
  _(e.g., `Features/GuidTools` instead of `Controllers/Services/Models`)_
- Separate concerns between domain, infrastructure, and presentation
- Avoid tight coupling between modules; prefer interfaces and DI
- For Blazor components: keep UI logic separate from business logic; place page code-behind in a separate `.razor.cs` file
- For MCP server code: follow protocol structure and keep handlers modular

## Testing Strategy

- Use **TUnit** — do not use xUnit, NUnit, or FluentAssertions
- Cover critical paths and edge cases
- Tests should be fast, isolated, and readable
- Use mocks/stubs only when integration is impractical

## Documentation & Clarity

- Use XML comments for public methods and APIs
- Avoid redundant or obvious comments — code should be self-explanatory
- Use examples in comments only when they clarify complex logic