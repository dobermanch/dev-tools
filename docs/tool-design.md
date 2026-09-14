# Designing a Tool

A tool is one function from `Args` to `Result`, and the generators expose it as a CLI command, an API endpoint, an MCP tool and a Web page. The decisions on this page come before any code: what one tool is, and what belongs in its arguments instead.

## One tool or several

The Unix principle of one tool per purpose is the starting point, but the purpose is the transformation, not its direction: `base64 -d`, `gzip -d` and `openssl enc -d` are each one tool with a switch for the inverse. The same reading applies here.

### One tool, when all of these hold

1. **One subject.** The operations work on one format or scheme - Base64, HTML entities, an IP network, one-time passwords - so the name stays specific.
2. **The operations belong together.** They are inverses (encode and decode, encrypt and decrypt) or views of the same thing (generate a code and check one, describe a network given as CIDR or as a range).
3. **One contract fits every mode.** The `[PipeInput]` and `[PipeOutput]` are the same kind of value in each mode - text in and text out, say - and one `Result` serves all of them without most of its fields empty in some mode.
4. **Few arguments belong to one mode only.** Up to about three, and each says so in its description: "TOTP only: seconds each code lasts".

The mode is an enum argument named for what it chooses (`Transcoding`, `Type`), defaulting to the most common operation.

### Separate tools, when any of these holds

- **The results differ in shape.** A parser returns the parts of a token; a builder takes those parts and returns a token (`jwt-parser` and a JWT signer).
- **Each side has its own arguments.** A combined tool would mark most of them "X only".
- **One side reads input and the other has none.** A generator next to a parser of what it generates.
- **The name would turn vague.** "converter" over things that have nothing in common.

### Variants are options, not tools

The algorithm, format, alphabet or version of one operation is an argument: the hash algorithms of `hash`, the formats of `data-converter`, the UUID versions of `uuid-generator`, the SHA variants of `otp`. A new variant is a new enum member, never a new tool.

### Why it matters here

- **MCP.** Every tool's name, description and schema is sent to the model in every session, and some clients cap how many tools they load. A mode argument costs a few words; a second tool costs a whole schema.
- **Pipelines.** A step in a [tool flow](proposals/tool-flow.md) carries its arguments, so a mode costs a pipeline nothing, while a contract that changes shape between modes would break what it connects to.
- **Web UI.** One page with a mode switch is one place to find, bookmark and mark as a favourite.

### Examples

| Tools | Outcome | Because |
|---|---|---|
| `html-entities`, `url-transcoder`, `base-encoding` | One tool, Encode/Decode | Inverses of one transformation, text in and out; an option such as line breaks may apply to one direction only |
| `encryption` | One tool, Encrypt/Decrypt | The key, algorithm and ciphertext format are the same on both sides |
| `otp` | One tool, generates and checks | The same scheme; checking adds two arguments and two result fields |
| `ip-subnet`, `ip-converter` | Two tools | A network and a single address give results of different shape |
| `hash` with a `Key` | One tool | HMAC is a variant of hashing: same input, same output |
| `data-converter` | One tool, `From`/`To` | Formats are variants of one conversion |
| `jwt-parser` and a JWT signer | Two tools | Different arguments and results |
