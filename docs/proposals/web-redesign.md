# Web redesign

**Status:** in progress (agreed 2026-09-15). A **temporary** working document: delete it once the last step lands.

The Web UI (`Dev.Tools.Web`) moves from stock MudBlazor to a dark, mono-accented design. The design handoff lives in `design/Dev Tools Redesign Direction/design_handoff_dev_tools_redesign/` - `README.md` for tokens and screens, `Dev Tools Redesign.dc.html` for the prototype (open it in a browser; the `TOOLS` object in its script records each tool's controls, panes and tables).

The design is a guide, not a pixel spec. Where a tool needs a control or an output the prototype does not show (a colour picker, a QR code preview, a diff view, a run button), the page gets it, built from the same components.

## Decisions

- **Dark and light.** Dark is the design's own palette and the default; the light palette came from the designer. MudBlazor themes its own components, and the `--dt-*` tokens follow `[data-theme]` on `<html>`, stamped before the first paint from the saved preference. The mode (System, Light, Dark) stays switchable from the app bar and Settings, System being the default.
- **Two accents.** Light needs a readable lime for text (`--dt-accent`) apart from the bright one for filled surfaces (`--dt-accent-fill`, with `--dt-accent-fill-contrast` on top). Dark uses the same lime for both, so components must still pick the right token.
- **Fonts** are self-hosted in `wwwroot/fonts`: Space Grotesk (Latin), Onest (Cyrillic only - Space Grotesk has none, and Onest is the closest match drawn with it), JetBrains Mono (mono, Cyrillic included). Licences sit beside them.
- **Out of scope for now:** the REST API and MCP links, the MCP server page, Pipelines, and the sidebar's INTERFACES group.
- **Home** keeps the group-by-category toggle, drops the separate Favorites section, and gains Recent. The sidebar's Favorites and Recent entries filter the index (`?view=favorites`, `?view=recent`); its categories set `?category=`.
- **Logos:** `wwwroot/icon.svg` (the bordered mark) is the app icon - favicon, PWA icons, manifest; `wwwroot/logo.svg` (the mark alone) is the header and boot-screen mark.
- **Components first.** Phase B builds every component the pages need, so that migrating a page in phase D is layout only - the `.razor.cs` tool logic stays as it is.
- **Categories** come from the C# `[ToolDefinition]` attributes, where the prototype's grouping differs.

## Working rules

- Every step is one commit, reviewed and checked in by hand.
- Every step builds warning-clean and leaves the app working.
- Every step adds its UI strings to `Resources.resx`, `Resources.en-US.resx` and `Resources.uk-UA.resx`.
- Before a step starts, its detailed plan and open questions are agreed.

## What the pages use today

Counted across `Pages/Tools/*.razor` before the redesign, so the component phase covers all of it:

- Layout: 32 `MudCard`, 68 `MudGrid`, 210 `MudItem`, 70 `DtSection`.
- 129 `DtTextField` - mostly read-only outputs or multi-line inputs, few actual options.
- 28 `DtSelectEnum`, 17 `DtSlider`, 22 bare `MudSwitch`, 3 `MudColorPicker`.
- 5 `MudTable`, 5 `MudAlert` (valid / invalid / warning), `MudButton` with progress (bcrypt, OTP), `MudProgressLinear` (OTP countdown).
- Page-specific output: an inline SVG (QR code), a hand-built diff view (Text Diff), a colour swatch (Color Converter), `DtHtmlField` (Character Viewer).

## Plan

### Phase A - Foundations

- [x] **A1. Theme and tokens.** `DevToolsTheme` (MudTheme: dark and light palettes, typography, radii, no shadows); every design token as a CSS variable in `css/tokens.css`, dark on `:root` and light under `[data-theme="light"]`, stamped on `<html>` by `devTools.setTheme` and, before the first paint, by an inline script in `index.html`. Space Grotesk, Onest and JetBrains Mono self-hosted in `wwwroot/fonts` (variable woff2, subset by unicode-range, OFL licences alongside) replace Roboto from Google Fonts; the service worker now caches `.woff2`. Fixed on the way: a theme change made in Settings did not reach the layout, because `SetThemeModeAsync` published no notification.
- [x] **A2. Brand assets.** `logo1.svg` → `icon.svg` (the bordered app icon), `logo2.svg` → `logo.svg` (the mark alone), both with their fills as attributes rather than a `<style>` block, so the two can be inlined on one page without their class names clashing. SVG favicon with a 32px PNG fallback; `icon-192/512.png`, `apple-touch-icon.png` (180px, opaque corners - iOS ignores transparency) and `icon-maskable-192/512.png` (from `icon-maskable.svg`, mark inside Android's 80% safe zone) rendered with `rsvg-convert`. Manifest named `DEV.TOOLS` with the dark background and theme colours and both icon purposes; `theme-color` and title in `index.html`; `404.html` repainted in the new palette.
- [x] **A3. Boot screen.** Static markup, CSS and a small script in `index.html`: logo with progress arc, bar, phase line, log of loaded files, "first load only" note. Driven by `--blazor-load-percentage` and `loadBootResource`, removed once `Blazor.start()` resolves. No "Skip to app" (nothing to skip to before the runtime exists) and no tool count (unknown before .NET starts). Also sorts out startup: `index.html` loads `blazor.webassembly.js` with `autostart="true"` and also calls `Blazor.start()` from a module importing `./decode.min.js`, which is not in `wwwroot`.

### Phase B - Components

Each step builds or restyles components and swaps their existing uses one for one, so nothing lands unused. Pages keep their card-and-grid layout until phase D.

- [x] **B1. Small building blocks.** `Components/Primitives`: `DtMonoLabel`, `DtTag`, `DtIconButton` (plain or bordered, named sizes 17/19/20px), `DtButton` (accent or outlined, `Loading`), `DtToggleGroup<T>` (options as a list, so a page needs no child components). Each with scoped CSS built from the `--dt-*` tokens. Converted: `KeywordChips` (no more `MudChip`), `FavoriteButton` (accent when set, with a localized tooltip it never had), the bcrypt and OTP buttons, both Home toggles. `DtChip` waits for C2, where the filter row is built.
- [ ] **B2. One shared field wrapper.** Mono label above, 36px control, error or hint line below. `DtTextField` (single line), `DtSelectEnum` and `DtSlider` (`LABEL — 12`, numeric box dropped) rebuilt on it; `Hint` / `Error` parameters replace loose `MudText` captions.
- [ ] **B3. Switch and colour picker.** `DtSwitch` (on/off pill) replaces the 22 `MudSwitch`; `DtColorPicker` wraps `MudColorPicker` in the same wrapper.
- [ ] **B4. Panes.** `DtPane` (header label, actions, body, footer). `DtInputPane`: mono textarea, open-file and clear actions, `N chars · N lines`, debounce; file loading moves here from `DtTextField` with `AllowFileInput` / `OnFileLoaded` unchanged; a page can have two. `DtOutputPane`: accent border, copy / download / refresh (generators), status line with `ok` or the tool's error; body is text or anything the page puts in it (fields, SVG, diff, HTML). Adds `JsService.DownloadAsync` and a styled "copied" snackbar.
- [ ] **B5. Output values.** `DtFieldList` / `DtFieldValue` (read-only rows with copy), `DtStatus` (replaces `MudAlert`), `DtProgress` (OTP countdown).
- [ ] **B6. Table.** `DtTable<T>`: header and row templates, row count, horizontal scroll. Replaces the 5 `MudTable`.
- [ ] **B7. Page structure and header.** `DtToolPageHeader` restyled (breadcrumb, icon, h1, description, tags, favorite and share) - every page picks it up without edits. `DtOptions` (the `OPTIONS` grid; items can span 2 columns or the full width), `DtPanes` (auto-fit pane grid), `DtSection` (title + mono count).

### Phase C - App layout and home

- [ ] **C1. App bar and frame.** Dot-grid background, sticky blurred bar, logo + wordmark + `v{AppDetails.Version}`, search button with Ctrl/⌘K opening the restyled `SearchDialog`, Settings and GitHub icon buttons.
- [ ] **C2. Home.** Hero with counts from the tool provider and live search; filter chips, group-by-category toggle, grid/list toggle; category sections; `ToolCard` / `ToolListView` rewritten; reads `?category=` and `?view=`. Adds the missing `xml-formatter` icon.
- [ ] **C3. Sidebar.** NAVIGATE (All tools, Favorites + count, Recent), CATEGORIES with counts, the "runs locally" card - reworded, since IP Details calls `api.ipify.org`. Active row follows the route; temporary drawer below `Md`.
- [ ] **C4. Recent tools.** A bounded list in preferences, recorded when a tool page opens; sidebar count and the `?view=recent` filter.

### Phase D - Tool pages (layout only)

- [ ] **D1. Text in, text out:** base-encoding, url-transcoder, html-entities, nato, roman-converter.
- [ ] **D2. Text in, text out:** json-formatter, xml-formatter, data-converter, encryption.
- [ ] **D3. Field-list output:** hash, case-converter, int-base-converter, date-convert.
- [ ] **D4. Field-list output:** ip-converter, ip-subnet, text-statistics, ip-details.
- [ ] **D5. Generators:** token-generator, passphrase-generator, uuid-generator, ulid-generator, lorem-ipsum.
- [ ] **D6. Parsers with tables:** char-viewer, url-parser, jwt-parser, regex-tester, cron-parser.
- [ ] **D7. text-diff** - two inputs, diff view.
- [ ] **D8. color-converter** - picker, swatch.
- [ ] **D9. qr-code** - SVG preview, colours.
- [ ] **D10. otp** - countdown, code check.
- [ ] **D11. bcrypt** - explicit run button.

### Phase E - Cleanup

- [ ] **E1.** Settings (theme selector dropped) and NotFound restyled.
- [ ] **E2.** Remove what is now unused (`DtHtmlField`, `LabelPositionType`, `DtPageHeader`, stale resx keys); update the Web UI section of `CLAUDE.md`; note in `docs/design-review.md` that phase 6's shared page is partly covered; delete this document.
