# Available Tools

A categorized reference of all tools included in Dev Tools. Each tool is accessible via the [CLI](../README.md#cli), [REST API](../README.md#rest-api), [Web UI](../README.md#web-ui), and [MCP Server](../README.md#mcp-server-ai-assistants).

---

## Converters

| Tool | Alias | Description |
| --- | --- | --- |
| `base-encoding` | `base64` | Encode text or a file to Base64, or decode Base64 back to text, with an optional URL-safe alphabet and line breaks |
| `case-converter` | `case` | Convert text between 14 case formats: camelCase, snake_case, PascalCase, CONSTANT_CASE, kebab-case, dot.case, Header-Case, path/case, Sentence case, and more |
| `int-base-converter` | | Convert integers between binary, octal, decimal, hexadecimal and base 64 |
| `roman-converter` | | Bidirectional Roman numeral conversion for integers 1–3999 |
| `html-entities` | | Escape text for HTML (`&`, `<`, `>`, `"`, `'`), optionally writing every non-ASCII character as a numeric reference, or decode HTML character references back to text |
| `url-transcoder` | | URL-encode or URL-decode a string (RFC 3986) |
| `date-convert` | | Convert between date/time formats: ISO 8601, ISO 9075, RFC 3339, RFC 7231, Unix timestamp, milliseconds, MongoDB ObjectId, Excel date, and JS date; omit the input to convert the current time |
| `data-converter` | | Convert structured data between JSON, YAML, TOML and XML, or write it as CSV; converting a format to itself reformats it |

---

## Text

| Tool | Alias | Description |
| --- | --- | --- |
| `char-viewer` | | Inspect every character in a string — shows Unicode code point, hex, ASCII value, and Unicode category |
| `nato` | | Spell out text using the NATO phonetic alphabet (e.g. `SOS` → Sierra, Oscar, Sierra) |
| `text-statistics` | `wc` | Count characters (as a reader sees them), words, lines and paragraphs, and measure the length in UTF-16 code units and UTF-8 bytes |
| `lorem-ipsum` | `lorem` | Generate Lorem Ipsum placeholder text with configurable paragraph, sentence, and word counts |
| `json-formatter` | `jf` | Format, compact, or transform JSON — supports key sorting, key case normalization, and empty-value exclusion |
| `xml-formatter` | `xf` | Format, compact, or transform XML — same options as the JSON formatter |

---

## Generators

| Tool | Alias | Description |
| --- | --- | --- |
| `token-generator` | `tokengen` | Generate cryptographically random tokens with configurable length, character set (uppercase, lowercase, digits, symbols), and count |
| `passphrase-generator` | `passphrase` | Generate memorable word-based passphrases with configurable word count, separator, capitalization, and salt |
| `uuid-generator` | `uuid` | Generate UUIDs in any standard variant: v4 (random, the default), v3 (MD5 namespace), v5 (SHA-1 namespace), v7 (timestamp-sortable), Nil, or Max |
| `ulid-generator` | `ulid` | Generate ULIDs (Universally Unique Lexicographically Sortable Identifiers) — random, Min, or Max |

---

## Crypto & Security

| Tool | Alias | Description |
| --- | --- | --- |
| `hash` | | Hash text or a file using MD5, SHA-1, SHA-256, SHA-384, or SHA-512, or compute an HMAC when given a key; output as a lowercase hex string |
| `bcrypt` | | Hash a password with bcrypt (a random salt, cost 4-15), or check a password against a `$2a$`, `$2b$` or `$2y$` hash |
| `encryption` | `cipher` | Encrypt text with AES-128-CBC, AES-256-CBC, AES-256-GCM, TripleDES-CBC or RC4 to Base64 or hex, or decrypt what it encrypted |
| `otp` | `totp`, `hotp` | Generate or check TOTP and HOTP codes (RFC 6238, RFC 4226) from a Base32 secret, or generate a secret, with the previous and next codes and the `otpauth://` URI for authenticator apps |

---

## Parsers

| Tool | Alias | Description |
| --- | --- | --- |
| `jwt-parser` | | Decode a JWT token without verification — extracts header, payload, claims, algorithm, issuer, audience, and expiry timestamps |
| `url-parser` | | Parse a URL into its components: scheme, username, password, hostname, port, path, query string, and individual query parameters |

---

## Development

| Tool | Alias | Description |
| --- | --- | --- |
| `regex-tester` | `regex` | Test a .NET regular expression against text: every match with its position and capture groups, and an optional replacement; a match that backtracks too long is reported instead of hanging |
| `cron-parser` | `cron` | Explain a cron expression in English and list its next run times in a time zone, daylight saving changes included; five fields or six with seconds, `L`, `W`, `#` and the `@daily`-style macros |
| `text-diff` | `diff` | Compare two texts line by line as a unified diff, the format of `diff -u` and `git diff`, optionally ignoring whitespace and case |

---

## Network

| Tool | Alias | Description |
| --- | --- | --- |
| `ip-details` | | Look up your current public IPv4 and IPv6 addresses |
| `ip-subnet` | `subnet` | Calculate an IPv4 or IPv6 network from CIDR, a dotted mask, an address or a range: network, masks, broadcast, host range and counts, and the CIDR blocks that cover a range exactly |
| `ip-converter` | | Convert an IP address between dotted IPv4, IPv6 (short and expanded, with IPv4-mapped addresses) and one decimal, hexadecimal or binary number |
