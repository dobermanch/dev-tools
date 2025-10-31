---
applyTo: 'src/**'
---
# GitHub Copilot Instructions

This solution is written in **.NET/C#** and includes:

- Core library of developer tools (e.g., GUID generation, Base64 encoding/decoding)
- Console application
- Web API
- Blazor frontend
- MCP server
- Test projects

## 🧠 General Coding Principles

- Follow modern .NET/C# best practices:
  - `async/await`
  - Dependency Injection
  - Nullable reference types
  - Minimal APIs
- Be concise and avoid overengineering
- Create abstractions only when they improve clarity or reuse
- Reuse existing abstractions and utilities when possible
- Prefer idiomatic C# over verbose or overly generic patterns
- Use clear naming conventions and avoid redundant comments
- Avoid static state unless explicitly required
- Use `ILogger<T>` for logging; avoid hardcoded console output in production
- Prefer `IOptions<T>` for configuration binding
- Keep public APIs minimal, well-documented, and intuitive

## 🧱 Project Structure & Architecture

- Group related features by domain, not by layer  
  _(e.g., `Features/GuidTools` instead of `Controllers/Services/Models`)_
- Apply clean architecture principles where applicable:
  - Separate concerns between domain, infrastructure, and presentation
- Avoid tight coupling between modules; prefer interfaces and DI
- For Blazor components:
  - Keep UI logic separate from business logic
  - For page components, place behind logic into separate file
- For MCP server code:
  - Follow protocol structure and keep handlers modular

## 🧪 Testing Strategy

- Use **Test Platform** avoid using **FluentAssertions**
- Cover critical paths and edge cases
- Tests should be fast, isolated, and readable
- Avoid testing implementation details
- Use mocks/stubs only when integration is impractical

## 📚 Documentation & Clarity

- Use XML comments for public methods and APIs
- Avoid redundant or obvious comments — code should be self-explanatory
- Use examples or diagrams in comments only when they clarify complex logic