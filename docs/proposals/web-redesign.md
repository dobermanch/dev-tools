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
- [x] **B2a. The shared field wrapper, select and slider.** `DtField` - mono label above, control, then one line for an error or a hint - and `DtSelectEnum` and `DtSlider` rebuilt on it. The select is a native `<select>` styled by the tokens (the browser draws the list, which `color-scheme` keeps in theme); the slider is a native range whose label carries the value as `LABEL — 12`, with the numeric box dropped: it follows the handle while dragging and re-runs the tool once it is let go. `Hint` and `Error` parameters replace loose `MudText` captions. `LabelPosition` is accepted but ignored until Settings is restyled in E1.
- [x] **B2b. The text field.** `DtTextField` (single line) rebuilt on `DtField`: a native input, mono, copy and load-file as `DtIconButton`s, and its own debounce in place of MudBlazor's. Values convert through Blazor's `BindConverter` with the invariant culture, covering the `string`, `int`, `long`, `char?` and `DateTime?` uses (the last with a `Format` and a `datetime-local` input); text that does not parse leaves the last good value in place and marks the field. A `Validation` predicate that throws on a value - `int.Parse` on an empty string - counts as invalid rather than crashing. Multi-line fields still render through MudBlazor until B4 turns them into panes, so the component has two paths meanwhile.
- [x] **B3. Switch and colour picker.** `DtSwitch` - the design's pill with the state spelled out beside it, built as a `button` with `role="switch"` so it is keyboard- and screen-reader-friendly - replaces all 22 `MudSwitch` across 12 pages; "on"/"off" are localized. `DtColorPicker` keeps MudBlazor's picker (alpha, palette) inside `DtField`, restyling only the trigger to match the other 36px controls; it covers the QR page's two colours and the colour converter.
- [x] **B4. Panes.** `DtPane` (header label, actions, body, footer). `DtInputPane`: mono textarea, open-file and clear actions, `N chars · N lines`, debounce; `AllowFileInput` / `OnFileLoaded` behave as on the field, and a page can have two. `DtOutputPane`: accent border, copy / download / refresh (generators only - the action appears when a page hands it a handler), status line showing `ok` or the tool's error; the body is text or anything the page puts in it (fields, SVG, diff, HTML). File reading moved into a shared `FileInput` helper used by the pane and by `DtTextField` while both exist; `JsService.DownloadAsync` writes a blob through `site.js`. Copy shows a tick on the icon for a moment instead of raising a toast. JSON Formatter migrated as the pilot, so the panes land in use.
- [x] **B5. Output values.** `DtFieldValue` (mono microlabel over a tinted row that wraps, with a copy that ticks like the pane's) and `DtFieldList` (their stack, as a pane body). `DtStatus` - a bordered strip with an icon, so an answer reads as a result - replaces every `MudAlert`: bcrypt's match and truncation, OTP's valid and invalid, and the Settings storage note. `DtProgress` replaces `MudProgressLinear` for the OTP countdown. Hash migrated as the pilot: input pane with the `ToolContent` file path, output pane whose body is the digest list, with copy and download over every digest at once.
- [x] **B6. Table.** `DtTable<T>`: the mono table in a rounded border, with an optional title and a row count beside it, horizontal scroll, and an optional `MaxHeight` that scrolls inside while the header stays in view. Pages write plain `<th>`/`<td>` in the two templates and the styling reaches them through `::deep`. Replaces all 5 `MudTable` (Character Viewer, JWT claims, URL parameters, Regex matches, Cron next runs); the matches and next-runs tables gained the heading they never had, so their count has somewhere to sit.
- [x] **B7. Page structure and header.** `DtToolPageHeader` restyled - breadcrumb, accent icon, title, description, keyword tags, and favorite and share in bordered squares - which all 32 pages pick up without edits; share offers the system sheet where there is one and copies the link otherwise, ticking the icon to say so, and `FavoriteButton` gained a bordered form for it. `DtOptions` is the `OPTIONS` grid (a control spans two columns with `dt-span-2` or the row with `dt-span-all`), `DtPanes` the auto-fit pane grid, and `DtSection` now carries a title with a mono count; `ShowDivider` stays accepted-and-ignored until the pages passing it move in phase D. JSON Formatter and Hash converted fully - no `MudCard`, `MudGrid` or `MudItem` left on either - so the grids land in use.

### Phase C - App layout and home

- [x] **C1. App bar and frame.** Dot-grid background and 40px gutters (the `MudContainer` width cap is gone), sticky blurred bar with the logo, wordmark and a mono line beneath - the version when a build has one, `offline-first` either way. A search button carrying the shortcut chip, labelled `⌘K` or `Ctrl K` by what the browser reports, opens the `SearchDialog`; the key listener lives in `site.js` and calls back into the layout, and is unregistered when the layout goes away. Theme, Settings and a GitHub link sit beside it, the last moved up from the drawer footer. The shell's CSS lives in `app.css`, not a `.razor.css`: the layout is built from MudBlazor components, and scoped CSS does not reach a child component's own element.
- [x] **C2. Home.** Hero with a count badge built from the tool provider, a headline split into two strings so the accent lands on a real phrase in each language, and a search that filters title, slug and keywords as you type (300ms), with an empty state when nothing matches. Filter row: mono label, `DtChip` for `all` and each category with its count, the group-by-category toggle and the grid/list toggle. `ToolCard` and `ToolListView` rewritten to the design, with starring no longer opening the tool. The category filter travels in the URL (`?category=`), so it can be linked to and the back button undoes it, and `?view=favorites` narrows the index to starred tools, ready for the sidebar in C3. The count badge says tools and categories only: the design's "0 requests" is not true of IP Details. Adds the missing `xml-formatter` icon.
- [x] **C3. Sidebar.** `NavMenu` rebuilt as the design's panel: NAVIGATE (All tools, Favorites + count) and CATEGORIES with counts, each under a mono label, plus the "runs locally" card - worded to stay true, since IP Details asks a lookup service for your address. The active row is read off the URL, so it follows the index's own chips as well as these links. The hamburger now **collapses the panel to a 64px icon rail** (`MudDrawer` `Variant="Mini"`, 40×40 targets, labels as tooltips) rather than hiding it, and below `Md` the same state opens it as a temporary drawer; the rail persists through the existing `IsDrawerOpen` preference, so no new setting was needed. The drawer header (a second wordmark) and the version footer are gone - the app bar carries both - which leaves `AppDetails` and the `Subtitle`, `NavMenu.Home` and `NavMenu.Settings` strings unused for E2 to remove. Recent waits for C4 rather than shipping a row that goes nowhere.
- [x] **C4. Recent tools.** `UserPreferences.RecentDetails` keeps the last 8 tools, most recent first; opening one again moves it back to the front. Recorded in `DtToolPageHeader`, the one component every tool page already goes through, so no page was touched, and announced by a `RecentChangedNotification` the sidebar listens to. The sidebar row shows the count and links to `?view=recent`, which the index answers in the saved order rather than the catalogue's - for this view, when you opened something is the point.

### Phase D - Tool pages (layout only)

- [x] **D1. Text in, text out:** base-encoding, url-transcoder, html-entities, nato, roman-converter. Each is now header → `DtOptions` → `DtPanes`, with no `MudCard`, `MudGrid`, `MudItem` or `DtSection` left; the code-behinds were not touched. Two changes worth knowing: every one of these now shows the tool's error in the output pane's status line, where three of them showed nothing at all before, and the roman converter's input loses its `InputType.Number` and its `Validation` lambda - the tool's own range check reports `InputNotValid`, which is what the status line shows, instead of a predicate that threw on an empty field. url-transcoder, html-entities and nato gained a load-file button, which the pane offers for any text input.
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
