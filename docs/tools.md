# Available Tools

A categorized reference of all tools included in Dev Tools. Each tool is accessible via the [CLI](../README.md#cli), [REST API](../README.md#rest-api), [Web UI](../README.md#web-ui), and [MCP Server](../README.md#mcp-server-ai-assistants).

---

## Converters

| Tool | Alias | Description |
| --- | --- | --- |
| `base64-encoder` | `64e` | Encode text to Base64 with optional URL-safe encoding and line breaks |
| `base64-decoder` | `64d` | Decode Base64 back to plain text |
| `case-converter` | `case` | Convert text between 14 case formats: camelCase, snake_case, PascalCase, CONSTANT_CASE, kebab-case, dot.case, Header-Case, path/case, Sentence case, and more |
| `int-base-converter` | | Convert integers between any bases from 2 to 64 (binary, octal, decimal, hex, …) |
| `roman-converter` | | Bidirectional Roman numeral conversion for integers 1–3999 |
| `url-transcoder` | | URL-encode or URL-decode a string (RFC 3986) |
| `date-convert` | | Convert between date/time formats: ISO 8601, ISO 9075, RFC 3339, RFC 7231, Unix timestamp, milliseconds, MongoDB ObjectId, Excel date, and JS date |

---

## Text

| Tool | Alias | Description |
| --- | --- | --- |
| `char-viewer` | | Inspect every character in a string — shows Unicode code point, hex, ASCII value, and Unicode category |
| `nato` | | Spell out text using the NATO phonetic alphabet (e.g. `SOS` → Sierra, Oscar, Sierra) |
| `lorem-ipsum` | `lorem` | Generate Lorem Ipsum placeholder text with configurable paragraph, sentence, and word counts |
| `json-formatter` | `jf` | Format, compact, or transform JSON — supports key sorting, key case normalization, and empty-value exclusion |
| `xml-formatter` | `xf` | Format, compact, or transform XML — same options as the JSON formatter |

---

## Generators

| Tool | Alias | Description |
| --- | --- | --- |
| `token-generator` | `tokengen` | Generate cryptographically random tokens with configurable length, character set (uppercase, lowercase, digits, symbols), and count |
| `passphrase-generator` | `passphrase` | Generate memorable word-based passphrases with configurable word count, separator, capitalization, and salt |
| `uuid-generator` | `uuid` | Generate UUIDs in any standard variant: Nil, v3 (MD5 namespace), v4 (random), v5 (SHA-1 namespace), v7 (timestamp-sortable), or Max |
| `ulid-generator` | `ulid` | Generate ULIDs (Universally Unique Lexicographically Sortable Identifiers) — random, Min, or Max |

---

## Crypto & Security

| Tool | Alias | Description |
| --- | --- | --- |
| `hash` | | Hash text using MD5, SHA-1, SHA-256, SHA-384, or SHA-512; output as a lowercase hex string |
| `text-encryptor` | `encrypt` | Encrypt text using AES-128-CBC, AES-256-CBC, AES-256-GCM, TripleDES-CBC, or RC4; output as Base64 or hex |
| `text-decryptor` | `decrypt` | Decrypt ciphertext produced by `text-encryptor`; supports all the same algorithms and input formats |

---

## Parsers

| Tool | Alias | Description |
| --- | --- | --- |
| `jwt-parser` | | Decode a JWT token without verification — extracts header, payload, claims, algorithm, issuer, audience, and expiry timestamps |
| `url-parser` | | Parse a URL into its components: scheme, username, password, hostname, port, path, query string, and individual query parameters |

---

## Network

| Tool | Alias | Description |
| --- | --- | --- |
| `ip-details` | | Look up your current public IPv4 and IPv6 addresses |
