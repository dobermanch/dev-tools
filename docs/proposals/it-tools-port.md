# Proposal: tools ported from it-tools

**Status:** agreed (2026-09-13). Not started. Update the tables as tools land, and add each finished tool to [docs/tools.md](../tools.md).

## Context

[it-tools](https://github.com/CorentinTh/it-tools) is a web-only collection of 83 developer tools. Its catalog was reviewed on 2026-09-13 to decide what is worth bringing here. A tool in this project is a function from `Args` to `Result` surfaced in the CLI, the API, the MCP server and the Web UI, so the review asked of each one:

- **Does it have that shape?** A page that only works in a browser (a camera, a stopwatch, a keyboard event viewer) or a cheat sheet does not.
- **Is it useful outside the browser?** The MCP server is the tie-breaker: deterministic work a model gets wrong - counting, hashing, cron schedules, subnet maths, running a regex, TOTP codes, diffs - is worth the most.
- **What does it cost to ship?** `Dev.Tools` is downloaded whole by the Blazor WASM app, so every library it takes lands in every frontend.

## Decisions

- **Libraries.** When the logic is very simple, write it. Otherwise use a well-tested third-party library rather than a home-grown parser. Check the WASM download size when a library is added.
- **HMAC is an option on `hash`**, not a new tool: an optional `Key` switches it to HMAC, and file input comes for free.
- **One `data-converter`** with `From`/`To` formats replaces it-tools' ten per-pair converters. The tool-flow pipeline connects to one contract instead of ten.
- **QR codes are SVG text**, so they ship before binary output (Phase 5 step 3). PNG can be added once binary output exists.
- **New `Category` and `Keyword` values** are added as the tools need them - a `Development` category for the regex tester, cron parser, diffs and chmod calculator, and keywords such as `Yaml`, `Toml`, `Csv`, `Regex`, `Cron`, `Diff`, `Color`, `Otp`, `Qr`.

## Already covered

token-generator, hash-text, uuid-generator, ulid-generator, encryption, date-time-converter, integer-base-converter, roman-numeral-converter, base64-string-converter, case-converter, text-to-nato-alphabet, url-encoder, url-parser, jwt-parser, lorem-ipsum-generator, xml-formatter, json-viewer and json-minify (`json-formatter`). base64-file-converter is Phase 5: file input is done, decoding to a file comes with binary output.

## Tier 1 - port

In the suggested order.

| Tool | Replaces | Implementation |
|---|---|---|
| `text-statistics` | text-statistics | Own. Characters, words, lines, paragraphs, UTF-8 bytes, UTF-16 length. **Done.** |
| `html-entities` | html-entities | Encoding is own (the five HTML-special characters, optionally every non-ASCII one), decoding BCL `WebUtility`. Encode/decode switch, like `url-transcoder`. **Done.** |
| `hash` with `Key` | hmac-generator | BCL `IncrementalHash.CreateHMAC` for SHA, streaming. HMAC-MD5 is own over `IMd5Hash` (RFC 2104), since the browser has no MD5. The key is UTF-8 text. **Done.** |
| `data-converter` | json↔yaml, json↔toml, yaml↔toml, xml↔json, json-to-csv, yaml-viewer | Every format is read into `JsonNode` and written from it (`src/Dev.Tools/DataFormats/`). YAML through YamlDotNet's parser and emitter, typed by the 1.2 core schema, aliases expanded up to a node limit. TOML through Tomlyn's untyped `TomlTable`, validated strictly first because the serializer lets duplicate keys through. XML and CSV are own: XML maps attributes to `@name`, mixed text to `#text` and repeated elements to arrays, all values strings, DTDs refused; CSV is written only. **Done.** |
| `regex-tester` | regex-tester | BCL. The title and description say it is the .NET flavour. A match timeout is required: the API is public and a catastrophic pattern must not hang it. **Done** - 2 seconds per match (`MatchTimeout`), cancellation between matches, every match counted and the first 1,000 listed. |
| `cron-parser` | crontab-generator | Cronos for next occurrences (`TimeProvider`), CronExpressionDescriptor for the English description. **Done** - Cronos decides what is valid; macros are written out first because the descriptor does not read them; time zone by IANA id, UTC by default; `H` and Quartz's year field are refused. |
| `ip-subnet` | ipv4-subnet-calculator, ipv4-range-expander | BCL `IPNetwork`. CIDR in: network, broadcast, mask, wildcard, host range and count. Range in: the CIDR blocks covering it. **Done** - own `UInt128` arithmetic instead of `IPNetwork`, whose parser refuses a CIDR with host bits set (`192.168.1.10/24`); IPv4 and IPv6; a range gives the smallest network holding it and the exact CIDR blocks; IPv4 parsed strictly (no `10.1` or octal forms); counts are decimal strings, since an IPv6 count can exceed 64 bits. |
| `ip-converter` | ipv4-address-converter | Own. Dotted, decimal, hex, binary, IPv4-mapped IPv6. **Done** - IPv6 too; input in any of those notations (hex after `0x`, binary after `0b`), a number up to 32 bits read as IPv4; address parsing shared with `ip-subnet` in `Networking/IpAddressText.cs`. |
| `otp` | otp-code-generator-and-validator | Own - HOTP/TOTP (RFC 4226/6238) and Base32 are short; test against the RFC vectors. Secret via `IRandomSource`, time via `TimeProvider`. Returns the current code, validation, and the `otpauth://` URI. **Done** - SHA-1/256/512, 6-10 digits, previous and next codes, validation within a window (TOTP both ways, HOTP ahead) with the matched offset; HMAC from the BCL, which runs in the browser. The Web page refreshes the TOTP code every second; a QR code of the URI waits for `qr-code`. |
| `text-diff` | text-diff | DiffPlex. The second text is an ordinary argument beside the `[PipeInput]`. **Done** - DiffPlex finds the differences; line splitting, the ignore options and the unified diff are own, since DiffPlex's renderer miscounts text that ends in a line break and never writes "\ No newline at end of file". Checked against GNU `diff -u`. |
| `bcrypt` | bcrypt | BCrypt.Net-Next. Hash and verify modes. The salt is built from `IRandomSource` and passed in, not generated by the library. **Done** - one tool with `Operation` Hash or Verify and no pipe output, since a hash and a match are different values; cost held between 4 and 15 and costlier hashes refused, so a request cannot hold the server or the single-threaded browser for minutes; passwords over 72 bytes flagged. The Web page runs on a button, not per keystroke. |
| `color-converter` | color-converter | Own. Hex, RGB, HSL, HWB, CMYK, CSS named colours. **Done** - reads every CSS notation of those, commas or spaces, `/ alpha`, hue units, clamping as CSS does; every output written from the 8-bit channels so they agree with the hex; CMYK by formula, no profile; the 148 names generated from the MIT color-name package. |
| `qr-code` | qr-code-generator, wifi-qr-code-generator | QRCoder: `SvgQRCode`, and its payload generator for the Wi-Fi mode. **Done** - with Net.Codecrete.QrCodeGenerator (MIT, a port of Nayuki's reference generator) instead of QRCoder, which depends on System.Drawing.Common on .NET 6 and later. The Wi-Fi payload is own: the ZXing WIFI: format, reserved characters escaped, hex-looking values not quoted. SVG text out; colors checked as hex, since they are written into the markup; too much data is `DataTooLong`. |

## Tier 2 - later, on demand

- **Cron builder, Web UI only (wanted):** a panel on the `cron-parser` page where minutes, hours, days of month, months and days of week are picked as fields and written into the expression field, so the tool itself stays a parser. Own code - each field becomes `*`, a list, a range or a step. Neither Cronos nor CronExpressionDescriptor builds expressions, and free English text to cron is not worth a home-grown parser: a model writes the expression and checks it with `cron-parser`.
- **`json-diff` (moved from tier 1, 2026-09-13):** own over `JsonNode`. A list of changes, each with its JSON path and old and new value, plus the same changes as an RFC 6902 JSON Patch; arrays compared by index at first. Its own tool rather than a mode of `text-diff`: the subject is structure, not lines, and the result is a different shape. Until then `json-formatter` with sorted keys piped into `text-diff` covers formatting and key order, though not types, null versus missing, or paths.
- **Very simple, own:** chmod-calculator, slugify-string, basic-auth-generator, list-converter (sort, dedupe, trim, prefix/suffix, join).
- **Library:** markdown-to-html (Markdig), user-agent-parser (UAParser - its pattern data goes stale), iban-validator-and-parser (IbanNet), math-evaluator (NCalc).
- **Needs a rule exception:** rsa-key-pair-generator. The BCL cannot take an injected random source, so tests could only check the key's shape.
- **Weak fit:** password-strength-analyser (the .NET zxcvbn ports are poor), http-status-codes and mime-types (lookup tables a model already knows), text-to-binary and text-to-unicode (could be options on `char-viewer`), svg-placeholder-generator.

## Not ported

- **Browser only:** camera-recorder, device-information, keycode-info, chronometer, benchmark-builder, html-wysiwyg-editor, emoji-picker.
- **Cheat sheets:** git-memo, regex-memo.
- **Too heavy or stale data:** mac-address-lookup (bundled IEEE OUI database), phone-parser-and-formatter (libphonenumber metadata is several MB), pdf-signature-checker (PDF and CMS parsing; iText is AGPL), docker-run-to-docker-compose-converter (a Docker flag table to maintain), sql-prettify (no good multi-dialect .NET formatter).
- **Novelty or niche:** numeronym-generator, string-obfuscator, ascii-text-drawer, email-normalizer, random-port-generator, mac-address-generator, ipv6-ula-generator, bip39-generator, eta-calculator, percentage-calculator, meta-tag-generator, safelink-decoder, temperature-converter (worth it only as a general unit converter).
