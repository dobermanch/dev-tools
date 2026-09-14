# Proposal: tool presets

**Status:** proposed (2026-09-13). Not started.

## Context

Tools that belong together are one tool with a mode argument - see [docs/tool-design.md](../tool-design.md). `base64-encoder` and `base64-decoder` became `base-encoding` with `--transcoding Decode`, and `text-encryptor` and `text-decryptor` became `encryption` with `--operation Decrypt`. The merge was a clean break: the old names were removed everywhere.

An alias could not have kept them. An alias names a tool, not its arguments, so `base64-decoder` as an alias of `base-encoding` would run it with the default Encode and silently encode what it was given to decode.

A preset is an alias that also fixes arguments. It would give back short commands for common modes (`dev-tools base64-decoder`, `dev-tools 64d`) and a way to rename or merge tools later without breaking scripts, links and favourites.

## Proposal

A preset is declared on the tool, beside its definition:

```csharp
[ToolDefinition(Name = "base-encoding", ...)]
[ToolPreset("base64-decoder", nameof(Args.Transcoding), TranscodingType.Decode, Aliases = ["64d"])]
public sealed class BaseEncodingTool : ToolBase<BaseEncodingTool.Args, BaseEncodingTool.Result>
```

- **Name.** A preset name and its aliases share the namespace of tool names and aliases, and are held to the same uniqueness (DT1001, and the check in `ToolsProvider`).
- **Fixed arguments.** One argument per attribute use is enough for every case so far; repeating the attribute with the same name could fix more. The value must be a constant of the argument's type.
- **Description.** Optional; by default generated from the tool's title and the fixed value ("Base64 Encoder/Decoder, Decode").

### Per frontend

| Frontend | Behaviour |
|---|---|
| CLI | A command per preset, with the fixed arguments set and their options not offered. `dev-tools base64-decoder SGk=` runs `base-encoding` with `Transcoding = Decode`. Help lists presets under the tool they belong to. |
| Library | `ToolsProvider.GetTool("base64-decoder")` returns the tool; a new `GetPreset(name)` returns the tool with its fixed arguments, for hosts that run tools by name. |
| Web UI | The tool's page also answers `/tools/{preset}`, opening with the fixed arguments selected. A favourite stored under a preset name resolves to its tool. |
| API | Not exposed at first. A route per preset is possible (`POST /tools/base64-decoder`, the fixed arguments absent from its schema) if a client needs it. |
| MCP | Not exposed. Presets would bring back the tool count the merge removed; a model reads the mode argument without difficulty. |

### Generator and analyzer work

- `ToolModel` gains the presets: name, aliases, fixed argument and value.
- `CliCommandGenerator` emits a settings class without the fixed options and a command that sets them before running the tool.
- `ToolsCatalogGenerator` adds presets to `ToolDefinition`, so the provider and the Web UI can resolve them.
- New analyzer rules: the argument named by a preset exists, and the value is of its type; a preset does not fix the `[PipeInput]` argument.

## Open questions

- Whether presets should also serve renames (a preset that fixes nothing is an alias with a description) or aliases stay the only way to add a name.
- Whether `--help` of a preset shows the tool's full description or only the preset's.
- Whether the API routes are worth their OpenAPI noise.
