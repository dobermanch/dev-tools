# Dev Tools

[![CI](https://github.com/dobermanch/dev-tools/actions/workflows/ci.yml/badge.svg)](https://github.com/dobermanch/dev-tools/actions/workflows/ci.yml) [![Release](https://github.com/dobermanch/dev-tools/actions/workflows/release.yml/badge.svg)](https://github.com/dobermanch/dev-tools/actions/workflows/release.yml) [![GitHub Release](https://img.shields.io/github/v/release/dobermanch/dev-tools)](https://github.com/dobermanch/dev-tools/releases/latest) [![.NET](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com/download/dotnet/10.0) [![License: MIT](https://img.shields.io/github/license/dobermanch/dev-tools)](LICENSE) [![Live Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://dobermanch.github.io/dev-tools/)

> A growing collection of developer utility tools — available as a CLI, REST API, and MCP server, all powered by a single .NET library.

Dev Tools is built around one idea: **write a tool once, expose it everywhere**. Every tool is a single C# class decorated with `[ToolDefinition]`. At build time, Roslyn source generators automatically produce a CLI command, an API endpoint, a Blazor page, and an MCP tool definition — no boilerplate, no duplication.

The library covers everyday developer tasks: encoding and decoding (Base64, URL, case formats), cryptographic operations (hashing, encryption, JWT parsing), identifier generation (UUID v3-v7, ULID, tokens, passphrases), data formatting (JSON, XML), and more. Tools are independently installable and usable across all four frontends:

- **CLI**: a `dotnet` global tool, fully pipeable
- **REST API**: an ASP.NET Core service with interactive Scalar docs
- **Web UI**: a Blazor WebAssembly app (MudBlazor)
- **MCP Server**: plugs directly into Claude, Cursor, and other AI assistants
- **NuGet**: embed the tools directly in your .NET application

Tools are localized (English and Ukrainian), multi-arch Docker images are published on every release.

Try the **[live demo](https://dobermanch.github.io/dev-tools/)** — no installation needed.

---

## Table of Contents

- [Dev Tools](#dev-tools)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Available Tools](#available-tools)
  - [Usage](#usage)
    - [Web UI](#web-ui)
    - [CLI](#cli)
    - [REST API](#rest-api)
    - [MCP Server (AI assistants)](#mcp-server-ai-assistants)
      - [Run via DotNet Tool](#run-via-dotnet-tool)
      - [Run via Docker](#run-via-docker)
    - [NuGet Library](#nuget-library)
    - [All-in-one Docker Image](#all-in-one-docker-image)
  - [Building Locally](#building-locally)
    - [Prerequisites](#prerequisites)
    - [Clone And Build](#clone-and-build)
    - [Make Targets](#make-targets)
  - [License](#license)

---

## Features

- **Write once, expose everywhere** — CLI, REST API, and MCP server all auto-generated from a single `[ToolDefinition]` attribute via Roslyn source generators
- **Growing tool library** — encoding, hashing, encryption, UUID/ULID generation, JWT parsing, formatters, and more
- **Pipeable CLI** — compose tools with Unix pipes: `echo "hello" | dev-tools base64-encoder`
- **AI assistant integration** — MCP server works with Claude, Cursor, and any MCP-compatible host
- **Localization** — English and Ukrainian
- **Multi-arch Docker images** — `linux/amd64` and `linux/arm64`, published on every release
- **Extensible** — add a new tool in a single `.cs` file and UI component; the rest is wired up at build time

---

## Available Tools

See [docs/tools.md](docs/tools.md) for the full categorized reference with aliases and descriptions.

---

## Usage

### Web UI

Run locally with Docker:

```bash
docker run -p 8080:8080 ghcr.io/dobermanch/dev-tools-web
```

Open `http://localhost:8080` in your browser.

---

### CLI

Install as a dotnet global tool:

```bash
dotnet tool install --global Dev.Tools.Console
```

```bash
# Encode to Base64
dev-tools base64-encoder "Hello, World!"
# → SGVsbG8sIFdvcmxkIQ==

# Pipe tools together
echo "Hello, World!" | dev-tools base64-encoder | dev-tools base64-decoder

# Format JSON (sort keys)
echo '{"b":2,"a":1}' | dev-tools json-formatter --sort-keys Ascending

# Get help
dev-tools --help
dev-tools hash --help
```

Or run without installing via Docker:

```bash
docker run --rm ghcr.io/dobermanch/dev-tools-console base64-encoder "Hello, World!"
# → SGVsbG8sIFdvcmxkIQ==

# Pipe into the container
echo "Hello, World!" | docker run --rm -i ghcr.io/dobermanch/dev-tools-console base64-encoder
```

---

### REST API

Run as a Docker container:

```bash
docker run -p 8080:8080 ghcr.io/dobermanch/dev-tools-api
```

Call an endpoint:

```bash
# Encode to Base64
curl -X POST http://localhost:8080/tools/base64-encoder \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello, World!"}'
```

Interactive API documentation (Scalar UI) is available at `http://localhost:8080/ui`.

---

### MCP Server (AI assistants)

Once registered, your assistant can use any tool directly — e.g., *"hash this text with SHA-256"* or *"generate 5 UUIDs"*.

#### Run via DotNet Tool

Install the MCP server as a global tool and register it in your AI assistant configuration.

```bash
dotnet tool install --global Dev.Tools.Mcp
```

Add to AI agent MCP config

```json
"dev-tools": {
  "type": "stdio",
  "command": "dev-tools-mcp"
}
```

#### Run via Docker

Use Docker `dev-tools-mcp` image and register it in your AI assistant configuration.

**Using stdio transport:**

```json
"dev-tools": {
  "type": "stdio",
  "command": "docker",
  "args": ["run", "--rm", "-i", "-e", "MCP_TRANSPORT=stdio", "ghcr.io/dobermanch/dev-tools-mcp"]
}
```

**Using HTTP transport:**

```json
"dev-tools": {
  "type": "http",
  "command": "docker",
  "args": ["run", "--rm", "-i", "ghcr.io/dobermanch/dev-tools-mcp"]
}
```

---

### NuGet Library

Embed the tools directly in your .NET application.

```bash
dotnet add package Dev.Tools
```

```csharp
// Base64 encode
var encoded = await new Base64EncoderTool().RunAsync(
    new Base64EncoderTool.Args("Hello, World!"),
    CancellationToken.None);

Console.WriteLine(encoded.Output); // SGVsbG8sIFdvcmxkIQ==
```

---

### All-in-one Docker Image

The `dev-tools` image bundles the Web UI, REST API, and MCP server into a single container — useful for self-hosting or quick local demos.

```bash
docker run -p 80:80 -p 8080:8080 -p 8081:8081 ghcr.io/dobermanch/dev-tools
```

| Port   | Service                     | Urls                        |
| ------ | --------------------------- | --------------------------- |
| `80`   | Web UI                      | `http://localhost`          |
| `8080` | REST API                    | `http://localhost:8080`, `http://localhost:8080/ui`     |
| `8081` | MCP server (HTTP transport) | `http://localhost:8081`     |

---

## Building Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (optional — for container builds)
- Make (optional — for convenience targets)

### Clone And Build

```bash
git clone https://github.com/dobermanch/dev-tools.git
cd dev-tools

dotnet restore
dotnet build
dotnet test
```

Run a specific test class:

```bash
dotnet test --filter "ClassName=Base64EncoderToolTests"
```

### Make Targets

```bash
make tool      # Pack and install Dev.Tools.Console globally
make mcp       # Pack and install Dev.Tools.Mcp globally
make nuget     # Pack the Dev.Tools NuGet package
make docker    # Build all Docker images locally
make clean     # Remove build artifacts and Docker images
```

---

## License

[MIT](LICENSE)
