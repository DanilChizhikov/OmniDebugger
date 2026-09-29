# Changelog

## [1.0.0] - unreleased

Initial release.

### Added
- Command core: `[DebugCommand]` / `[DebugTags]` on public instance methods and properties, turned into
  `CommandDefinition` metadata by a reflection scan that runs once per type for the lifetime of the domain.
- `ICommandCatalog` — explicit registration through `AddSource` / `RemoveSource` / `AddCommand` /
  `RemoveCommand`, with copy-on-write snapshots so a UI can enumerate safely. Removal never re-reflects.
  An `OnChanged` event announces every add and remove, and `TryGetCommand` / `TryGetDefinition` look one up
  by its `"Group/Name"` key.
- `ICommandInvoker` — runs and reads commands by a readable `"Group/Name"` key, coercing loosely typed
  arguments with the invariant culture and no JSON dependency. Every invocation is audited before it runs and
  a throwing command is caught, unwrapped, and logged rather than escaping into the caller.
- `ActionCommand` / `ValueCommand` / `ReadonlyValueCommand` and `ICommandSource` for registering commands
  without attributes.
- `IGroupOrder` for the display order of command groups, and `CommandKey` to build and parse the
  `"Group/Name"` keys everything is addressed by.
- `SearchIndex<T>` / `SearchSession<T>` over `ISearchIndexable`, which `CommandDefinition` implements —
  an inverted index that streams results across four stages of decreasing strictness (exact, prefix,
  infix, fuzzy) and republishes `Hits` after each one, best first. Slicing is paid for by the caller
  through `Advance(long budgetTicks)`, so a query never owns a frame; `RunAsync` spends a budget per
  frame for callers who would rather not write the loop. Each phrase is also indexed run together and
  by its initials, so *Add Coins* answers to `addcoins` and `ac`.
- `SearchIndex.RebuildAsync` builds a large index on a worker through `UnityEngine.Awaitable` while
  queries keep answering from the previous one; `Add` and `Remove` write to a delta segment that is
  folded into the bulk data once the changes add up.
- Main-thread enforcement across the catalog, the invoker, group order, and the search index — an
  off-thread call throws `InvalidOperationException` naming the operation and the thread.
- `OmniDebugger.Shared`: the first debugger built and not disposed yet, or — while there is none — one built on the
  spot from the project settings and owned by the package, disposed in the editor once play mode is over.
  `OmniDebugger.TryGetShared` reads it without building one. A debugger built next to it never replaces it and
  warns when both put a panel on screen. `OmniDebuggerOptions.CreateOnStartup` builds it before the first scene
  loads.
- A `link.xml` generated for every build with `OMNI_DEBUGGER` on: every type declaring a command, kept whole by the
  same rules the catalog registers commands with, and every enum used in a command signature. Only assemblies that
  reference OmniDebugger are searched.
- `Project Settings → DTech → OmniDebugger` to toggle the `OMNI_DEBUGGER` define per build target.
- UI Toolkit panel: one `OmniDebuggerView` mounted either over the running game or by
  `Window → DTech → OmniDebugger`. `new OmniDebugger()` builds the runtime panel itself in play mode and
  destroys it on `Dispose`; `OmniDebuggerOptions` configures it, and `CreatePanel = false` hands the job to an
  `OmniDebuggerPanel` component, which carries no options of its own and reads the project settings too. Tabs
  sit on top in portrait and in a sidebar in landscape; cards flow into two
  columns when there is room; the header paints the notch.
- Scaling: `OmniDebuggerScaleMode.ScreenSize` (360-unit-wide reference, leaning to height in landscape),
  `PhysicalSize` (96 DPI), or `Auto` by platform, times a `Scale` multiplier.
- Built-in tabs: Info (build, device, live runtime figures), Commands (group grid with favourites, cards with
  icon, tags, description popup, pin and star), Search (debounced, optional case sensitivity), Logs (captured
  console with type/text/tag filters, copy, clear, follow mode, older pages on demand) and Windows.
- `ILogFeed` on `IOmniDebugger.Logs`: thread-safe capture of `Application.logMessageReceivedThreaded`, `[Tag]`
  parsing and id-based paging. Storage is bounded twice — 16 384 records and a budget of about 8 MB of text — and
  a message repeated word for word is stored once, its tags parsed once, and read once per filter however many
  records share it.
- Floating windows through `IOmniDebugger.Windows` (custom content or a list of commands), shown while the panel
  is closed, draggable and collapsible, plus a built-in Pinned window.
- Open button: tap (or a configurable series of taps), lights up on every tap, hold 0.6 seconds to drag with animated
  corner brackets, position kept for the session, blinks on new errors. Starts at any corner or edge middle, with a
  configurable resting opacity. Keyboard shortcuts (single keys or chords, modifiers on either side) toggle the panel
  under either input backend, and are bound in Project Settings by pressing the keys.
- `[DebugIcon]` with Resources, `OmniDebuggerIconCatalog` and `IOmniDebuggerIconProvider` lookups;
  `CommandDefinition.Tags` and `CommandDefinition.Icon`.
- Enum arguments pick from a list in the panel's own popup layer.
- Every list in the panel scrolls by dragging — with a mouse as well as a finger — and coasts after release;
  a press only becomes a drag past a small threshold, so buttons under it still take a tap.
- The panel keeps its input object alive across an EventSystem being destroyed and recreated.
- Themes as `OmniDebuggerTheme` assets holding plain `.uss` sheets, applied after the panel's own skin so a
  theme that redefines a single `--od-*` variable wins. Dark and light are built in and switched with one
  button. `OmniDebuggerOptions.DefaultTheme` alone replaces both and hides the switcher; themes listed in
  `OmniDebuggerOptions.Themes` or added through `IOmniDebugger.Themes.Register` are offered next to the built-in
  ones in a dropdown, starting from the default theme. The theme and favourites are the only choices the panel
  saves: the editor window keeps them in `EditorPrefs` and the game in `PlayerPrefs`, so the two are
  independent. Pins, the open tab and group, and the open button position last for the session only.
- Extension seams: `IOmniDebuggerTabFactory` / `IOmniDebugger.Tabs` for additional tabs,
  `IArgumentFieldHandler` / `IOmniDebugger.Fields` for additional argument types, `IOmniDebuggerGesture` for how the
  panel is opened on a device.
- An optional PIN or password in front of the runtime panel (`OmniDebuggerPanelOptions.Lock`): a keypad for a
  PIN, a masked field for a password, stored only as a salted SHA-256 hash. It is asked for on every open, once
  per session or once per device until the secret changes; a run of wrong entries pauses the prompt, and play mode
  in the editor skips it by default.
- `Project Settings → DTech → OmniDebugger → Panel` for every panel option: startup, scaling and sorting order,
  panel settings, the open button, shortcuts, the lock, the default theme and extra themes, and icon catalogs kept outside
  `Resources`. Stored in `ProjectSettings/OmniDebuggerSettings.asset`; `new OmniDebugger()` and
  `OmniDebuggerOptions.Default` read a copy of it. Play mode reads it live, and a debugger built without options in
  code — with its panel — applies every edit at once; a build gets a snapshot written to a
  generated `Resources` asset just before the build and deleted after it, and only while `OMNI_DEBUGGER` is on for
  the target.
- `Project Settings → DTech → OmniDebugger → UI` for the editor window's theme, layout (auto, portrait or landscape)
  and zoom; the window scales the panel to fit itself the way `ScreenSize` does on a device.
- EditMode test suite covering scanning, the catalog, invocation, argument binding, search, group and command
  ordering, argument-array building, theme selection, view state, log storage, sharing and tags, shortcuts and tap
  series, favourites and pins, the window registry, scaling, options and their project-settings copies, the shared
  debugger and the generated `link.xml`.

### Notes
- No package dependencies. No UniTask, no Newtonsoft. The Input System package and uGUI are used when present
  (`OMNI_DEBUGGER_INPUT_SYSTEM`, `OMNI_DEBUGGER_UGUI` version defines) and never required.
- The runtime assembly is gated behind `OMNI_DEBUGGER`; without the define nothing is compiled, so consumer
  calls belong inside `#if OMNI_DEBUGGER`.
- `defineConstraints` gate code, not assets: the panel's `Runtime/UI/Resources/OmniDebugger` folder — three
  `.uss`, one `.tss`, one `PanelSettings`; icons are drawn as vectors — ships even with `OMNI_DEBUGGER` off. Delete that folder in a build
  that must not carry it.
