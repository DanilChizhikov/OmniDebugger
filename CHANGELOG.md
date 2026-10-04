# Changelog

## unreleased

### Added
- Check for updates on startup with update window. Also, for check update added menu item `Tools/DTech/OmniDebugger/Check Update`.
- `[DebugOptions(memberName)]` on a command property or parameter: the panel edits it with a dropdown of the values
  another member yields — a field, property or parameterless method returning `IEnumerable<T>`, or a `Func` returning
  one. Read live each time the dropdown opens. Checked at compile time (`OMNI014`–`OMNI016`).
- `ArgumentDefinition.HasOptions`, `ICommandRegistry.TryGetOptions`, the `options` parameter of `DebugCommand`,
  `ArgumentBuilder.Options`, `CommandBuilder.Dropdown<T>(name, options, get, set)` and `ArgumentFieldRequest.Options`.
- The source generator DLL carries its own version (2.2.0, `<Version>` in its project) and stamps it into the
  `[GeneratedCode]` attribute of the code it writes.
- Logs tab: tapping a log (or *Submit* on it) opens a pane under the list with the full message and stack trace;
  tapping it again or *Close* hides it. The selected row is highlighted.

### Fixed
- Logs tab: every message was drawn in the error colour. Messages now use the regular text colour; the type is shown by
  the stripe, the type label and a faint tint on warning and error cards.

## [2.1.0] - 2026-10-03

### Added
- Gamepad and keyboard navigation in the runtime panel. Opening it focuses the control focused last, or the selected
  tab, so navigation reaches the panel at once; lists scroll to the focused control; *Cancel* closes the open popup,
  then the panel; closing it hands the focus back. A popup opened while navigating focuses its first item and gives
  the focus back to its anchor when it hides.
- A focus ring (`.od-focus-ring`, colored by `--od-color-focus`) drawn while the focus is moved by a gamepad or the
  keyboard, and hidden after a touch or a click.
- `OmniDebuggerOpenOptions.GamepadCombo` (`OmniDebuggerGamepadButtons`): gamepad buttons that toggle the runtime
  panel, Select + Start by default. Needs the Input System package.

### Fixed
- Srolling for touch screen
- Command palette results could not take focus, so a gamepad could not run them.
- Scrollbars took focus and trapped gamepad navigation.
- Navigation stopped at the last control on screen: UI Toolkit looks for the next control inside the panel's bounds
  only. A move now scrolls the next control into view first, and past the last one scrolls the rest of the list.
- The D-pad of an Xbox controller on macOS did not navigate: Input System 1.19's `XboxGamepadMacOSNative` layout keeps
  the D-pad buttons outside the D-pad's own state, so actions bound to `<Gamepad>/dpad` never fire. The panel reads
  the D-pad itself when a layout is like that.

## [2.0.0] - 2026-09-30

A redesign of registration and of the panel. Breaking: see *Migration* below.

### Added
- A Roslyn source generator (`Runtime/Analyzers/DTech.OmniDebugger.SourceGenerator.dll`) that turns `[DebugCommand]`
  members into registration code at compile time. No reflection at runtime, nothing for the IL2CPP linker to strip,
  and a member that cannot be a command is a compile error (`OMNI001`–`OMNI013`) instead of a runtime warning.
  Internal members and base-class commands are supported.
- `ICommandRegistry` (`debugger.Commands`): `Register(obj)` / `Unregister(obj)` returning a disposable handle,
  `Add(IEnumerable<DebugCommand>)`, `TryExecute(path, params object[])`, `TryGet`, `TryGetValue`, `All`, `OnChanged`.
- `CommandBuilder` (`debugger.Commands.Build()`): `Group`, `Button` (up to three typed arguments described by
  `ArgumentBuilder`), `Toggle`, `Slider`, `Dropdown`, `Field`, `Value`, with `Icon`, `Tags`, `Description` and `Order`.
- Hierarchical command paths, `"Economy/Coins/Add"`, with `CommandPath` to build and read them. `IGroupOrder` orders
  groups at any depth.
- `DebugCommand`: the one command type — a `CommandDefinition` plus its delegates.
- The hotbar (`IHotbar`, `debugger.Hotbar`): pinned commands in a strip along the bottom or top of the screen
  (`OmniDebuggerPanelOptions.HotbarEdge`), saved on the device, foldable.
- Info providers (`IInfoProvider`, `IInfoSection`, `debugger.Info`): the Info tab is a section per provider, with
  text, live values, charts, command rows (`IInfoSection.Command`) and custom elements, one or two to a line. Built
  in: Performance (FPS chart), Memory (allocation chart), Graphics, Quality, Screen, Build and Device.
- Floating sections: any Info section floats over the game while the panel is closed or floating — from the button
  on its header, or `debugger.Info.Float` / `Dock` / `IsFloating` / `OnFloatingChanged`. A card starts at half the
  size of its section in the tab, and a grip in its corner scales it from half to three times that; which sections
  float and the scale of each are saved on the device.
- The command palette: Ctrl+K / Cmd+K, or the magnifier in the panel's header.
- `LogQuery.Parse` and a filter bar in the Logs tab: `tag:Net -tag:Ads type:error text "a phrase"`, with the terms as
  removable chips and the known tags as suggestions. `LogQuery.ExcludedTags`; text matches every word.
- `LogRecord.RepeatCount` and `LastTimestampUtc`: a message repeated back to back is one record, shown with ×N.
- Tags from `Debug.unityLogger.Log(tag, message)`, which Unity writes as `"tag: message"`.
- Typed arguments are remembered per command and saved on the device, in the panel and in the editor window.
- Built-in vector glyphs by name for icons (`OmniGlyphs`: `coin`, `bolt`, `bug`, `gear`, `play` and more).
- An error badge with a count on the open button.
- The editor window's own UI: a tree of commands with an inspector of native editor controls, and an Info view.

### Changed
- `IOmniDebuggerHost.Commands` is now the `ICommandRegistry`; `Catalog` is gone.
- `IGroupOrder.SetOrder` / `GetOrder` take a group path (`groupPath`, was `groupName`), at any depth.
- `[DebugCommand(groupPath)]` takes the group path only; `Name`, `Order` and `Description` are named arguments.
- The Commands tab is one list with a search box and group chips instead of collapsible sections and group pages.
- Tabs: Commands, Logs, Info, in that order. Search moved into the Commands tab and the palette.
- Floating windows are floating Info sections now: a pane over the game is an `IInfoProvider` — command rows through
  `IInfoSection.Command`, anything else through `Custom` — floated with `debugger.Info.Float`.
- `OmniDebuggerViewSettings.HostWindows` / `hostWindows` is `HostOverlays` / `hostOverlays`.
- Project settings reach a player as one of its preloaded assets, added for the length of the build, instead of a
  temporary asset in a `Resources` folder.
- The open button drags at once, no hold, and snaps to the nearest edge of the screen.
- `LogQuery.Tags` matches any of the tags, ignoring case.
- `LogRecord.Flags` (`LogFlags`) replaces `IsMessageTruncated` and `IsStackTraceTruncated`.
- `IIconRegistry` has one `Provider` (`IOmniDebuggerIconProvider.TryGetIcon(string key, …)`), `ResourcesIconProvider`
  by default. `CommandDefinition.IconKey`, `DebugIconAttribute(string key)` and `IOmniDebuggerTabFactory.Icon` are
  strings.
- The editor window keeps its state in a `ScriptableSingleton` in the editor's preferences folder.

### Removed
- `ICommandCatalog`, `ICommandInvoker`, `ICommandSource`, `IDebugCommand`, `IExecutableCommand`, `IReadableCommand`,
  `ActionCommand`, `ValueCommand`, `ReadonlyValueCommand`, `CommandKey` and the reflection scanner.
- The generated `link.xml` (`CommandLinkerStep`, `CommandPreservation`): nothing needs preserving any more.
- Favourites and the *Pinned* window — both are the hotbar now.
- `IOmniDebuggerHost.Windows`, `IWindowRegistry`, `IOmniDebuggerWindow`, the Windows tab and window menu, collapsing,
  the fade of an idle window and the one scale shared by every window.
- `LogTagMode` and the tag page of the Logs tab.
- `DebugIconSource`, `CommandIcon`, `IconEntry`, `OmniDebuggerIconCatalog`, `OmniDebuggerOptions.IconCatalogs`.
- The editor window's zoom, layout and theme settings (`Project Settings → DTech → OmniDebugger → UI`).

### Migration

| 1.x | 2.0 |
|---|---|
| `debugger.Catalog.AddSource(obj)` | `debugger.Commands.Register(obj)` (keep the handle, or call `Unregister(obj)`) |
| `debugger.Catalog.RemoveSource(obj)` | `handle.Dispose()` or `debugger.Commands.Unregister(obj)` |
| `debugger.Catalog.AddCommand(new ActionCommand(definition, action))` | `debugger.Commands.Build().Group(group).Button(name, action).Register()` |
| `ValueCommand` / `ReadonlyValueCommand` | `.Field(…)` / `.Toggle(…)` / `.Slider(…)` / `.Value(…)` on the builder |
| `ICommandSource` | `debugger.Commands.Add(IEnumerable<DebugCommand>)` |
| `[DebugCommand("Group", "Name", 10)]` | `[DebugCommand("Group", Name = "Name", Order = 10)]` |
| `"Group/Name"`, `CommandKey.Create` | `"Group/Sub/Name"`, `CommandPath.Combine` |
| `debugger.Commands.TryExecute(key, request)` | unchanged, or `TryExecute(path, 500)` |
| `debugger.Catalog.TryGetDefinition(key, …)` | `debugger.Commands.TryGet(path, …)` |
| `definition.Key` / `GroupName` / `Icon` | `definition.Path` / `GroupPath` / `IconKey` |
| `DebugIcon(DebugIconSource.Resources, "Icons/Coin")` | `DebugIcon("Icons/Coin")`, or a glyph name such as `DebugIcon("coin")` |
| Icon catalogs, `debugger.Icons.Register(provider)` | `debugger.Icons.Provider = provider` |
| `new LogQuery(tags: …, tagMode: LogTagMode.Any)` | `new LogQuery(tags: …)`, or `LogQuery.Parse("tag:A tag:B")` |
| `record.IsMessageTruncated` | `(record.Flags & LogFlags.MessageTruncated) != 0` |
| A private, static or value-returning `[DebugCommand]` member (skipped with a warning) | A compile error to fix |
| `debugger.Windows.RegisterCommands(id, title, paths, open: true)` | an `IInfoProvider` with `section.Command(path)` rows, `debugger.Info.Register(p)` and `debugger.Info.Float(p)` |
| `debugger.Windows.RegisterCustom(id, title, build, open: true)` | an `IInfoProvider` with `section.Custom(…)`, registered and floated the same way |
| `window.Open()` / `Close()` / `debugger.Windows.Unregister(id)` | `debugger.Info.Float(p)` / `Dock(p)` / `Unregister(p)` |

## [1.0.0] - 2026-09-26

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
- `OmniDebuggerHost.Shared`: the first debugger built and not disposed yet, or — while there is none — one built on the
  spot from the project settings and owned by the package, disposed in the editor once play mode is over.
  `OmniDebuggerHost.TryGetShared` reads it without building one. A debugger built next to it never replaces it and
  warns when both put a panel on screen. `OmniDebuggerOptions.CreateOnStartup` builds it before the first scene
  loads.
- A `link.xml` generated for every build with `OMNI_DEBUGGER` on: every type declaring a command, kept whole by the
  same rules the catalog registers commands with, and every enum used in a command signature. Only assemblies that
  reference OmniDebugger are searched, and a type whose commands cannot be read is named in a warning rather than
  dropped in silence.
- `Project Settings → DTech → OmniDebugger` to toggle the `OMNI_DEBUGGER` define per build target, with a *Support*
  link to the author's DonationAlerts page at the bottom. The same link is in the README and in `.github/FUNDING.yml`
  (GitHub's *Sponsor* button).
- UI Toolkit panel: one `OmniDebuggerView` mounted either over the running game or by
  `Window → DTech → OmniDebugger`. `new OmniDebuggerHost()` builds the runtime panel itself in play mode and
  destroys it on `Dispose`; `OmniDebuggerOptions` configures it, and `CreatePanel = false` hands the job to an
  `OmniDebuggerPanel` component, which carries no options of its own and reads the project settings too.
- Glass HUD look, dark and light: a translucent panel over the game with a lit edge, soft shadows and glows drawn
  by the panel itself (UI Toolkit has no backdrop blur), line icons and one electric-blue accent. Portrait runs
  edge to edge with a strip of icon tabs; landscape moves the tabs into a sidebar and floats the panel in a window
  over the game, or runs it edge to edge with `OmniDebuggerPanelOptions.LandscapeLayout`. The floating window leaves
  the game live around it (a tap beside it never closes it), is dragged by its top bar, compact by default and sized by
  `OmniDebuggerPanelOptions.FloatingScale`. Sections flow into two
  columns when there is room. Tabs carry icons: vector glyphs for the built-in ones, `IOmniDebuggerTabFactory.Icon`
  for your own.
- Scaling: `OmniDebuggerScaleMode.ScreenSize` (360-unit-wide reference, leaning to height in landscape),
  `PhysicalSize` (96 DPI), or `Auto` by platform, times a `Scale` multiplier.
- Built-in tabs: Info (build, device, live runtime figures), Commands (a section per group, favourites first, that
  unfolds in place or opens as a page; each command a row with its control — a switch, a slider, a field, a dropdown,
  a run button or a live value — and a ⋯ menu for favourite, pin and description), Search (debounced, optional case
  sensitivity), Logs (captured console with type/text/tag filters, copy, clear, follow mode, older pages on demand)
  and Windows (a switch per window, hide all, and one scale for every window).
- Live values: every row on screen re-reads its command four times a second, so a property changed from code moves
  its switch, slider or field too; a control being edited is left alone until the edit is committed.
  `IOmniDebuggerHost.Refresh()` redraws every view of the debugger at once (merged to one redraw per frame), and
  `IOmniDebuggerHost.OnRefreshRequested` passes the request on to content of your own.
- `[DebugRange]` on a numeric property or parameter, carried as `ArgumentDefinition.Range` (`ArgumentRange`): the
  panel edits it with a slider and a value box, snapped to an optional `Step`.
- `ILogFeed` on `IOmniDebuggerHost.Logs`: thread-safe capture of `Application.logMessageReceivedThreaded`, `[Tag]`
  parsing and id-based paging. Storage is bounded twice — 16 384 records and a budget of about 4 million characters
  of text (some 8 MB) — and
  a message repeated word for word is stored once, its tags parsed once, and read once per filter however many
  records share it.
- Floating windows through `IOmniDebuggerHost.Windows` (custom content or a list of commands, each handed back as an
  `IOmniDebuggerWindow` to open, close or collapse), shown while the panel is closed or floating — the one touched
  last, window or panel, on top — draggable and collapsible, plus a built-in
  Pinned window. One scale, ×0.5 to ×2 from the Windows tab, sizes every window and is saved.
- Open button: a square glass button; tap it (or a configurable series of taps), it lights up on every tap, hold
  0.6 seconds to drag with animated corner brackets, position remembered on the device until `ButtonAnchor` changes,
  turns red and pulses on new errors. Starts at any corner or edge middle, with a
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
  `OmniDebuggerOptions.Themes` or added through `IOmniDebuggerHost.Themes.Register` are offered next to the built-in
  ones in a dropdown, starting from the default theme. The theme and favourites are the only choices the panel
  saves, plus the floating windows' scale and the open button's position in the game: the editor window keeps them in
  `EditorPrefs` and the game in `PlayerPrefs`, so the two are independent. Pins, the open tab and group and the
  floating panel's position last for the session only.
- Extension seams, all hanging off the debugger and documented with examples in the README: `IOmniDebuggerTabFactory`
  / `IOmniDebuggerHost.Tabs` for additional tabs — the built-in ones (`info`, `commands`, `search`, `logs`, `windows`)
  can be unregistered or replaced; `IOmniDebuggerHost.Windows` for floating windows of your own;
  `IArgumentFieldHandler` / `IOmniDebuggerHost.Fields` for additional argument types; `IOmniDebuggerIconProvider` /
  `IOmniDebuggerHost.Icons` for icons from anywhere; `IOmniDebuggerGesture` / `OmniDebuggerPanel.SetGesture` for how
  the panel is opened on a device; `OmniDebuggerView` to mount the panel in UI of your own; and
  `IOmniDebuggerHost.OnRefreshRequested` for content of your own to redraw with the panel.
- An optional PIN or password in front of the runtime panel (`OmniDebuggerPanelOptions.Lock`): a keypad for a
  PIN, a masked field for a password, stored only as a salted SHA-256 hash. It is asked for on every open, once
  per session or once per device until the secret changes; a run of wrong entries pauses the prompt, and play mode
  in the editor skips it by default.
- `Project Settings → DTech → OmniDebugger → Panel` for every panel option: startup, landscape layout, floating scale, scaling and sorting order,
  panel settings, the open button, shortcuts, the lock, the default theme and extra themes, and icon catalogs kept outside
  `Resources`. Stored in `ProjectSettings/OmniDebuggerSettings.asset`; `new OmniDebuggerHost()` and
  `OmniDebuggerOptions.Default` read a copy of it. Play mode reads it live, and a debugger built without options in
  code — with its panel — applies every edit at once; a build gets a snapshot written to a
  generated `Resources` asset just before the build and deleted after it, and only while `OMNI_DEBUGGER` is on for
  the target.
- `Project Settings → DTech → OmniDebugger → UI` for the editor window's theme, layout (auto, portrait or landscape)
  and zoom; the window scales the panel to fit itself the way `ScreenSize` does on a device.
- EditMode test suite covering scanning, ranges, the catalog, invocation, argument binding, search, group and command
  ordering, argument-array building, theme selection, view state, log storage, sharing and tags, shortcuts and tap
  series, favourites and pins, the window registry, scaling, options and their project-settings copies, the lock
  (secret hashing, PIN rules, attempts and cooldown, unlock memory), `Refresh`, the shared debugger and the generated
  `link.xml`.

### Notes
- No package dependencies. No UniTask, no Newtonsoft. The Input System package and uGUI are used when present
  (`OMNI_DEBUGGER_INPUT_SYSTEM`, `OMNI_DEBUGGER_UGUI` version defines) and never required.
- The runtime assembly is gated behind `OMNI_DEBUGGER`; without the define nothing is compiled, so consumer
  calls belong inside `#if OMNI_DEBUGGER`.
- `defineConstraints` gate code, not assets: the panel's `Runtime/UI/Resources/OmniDebugger` folder — three
  `.uss`, one `.tss`, one `PanelSettings`; icons are drawn as vectors — ships even with `OMNI_DEBUGGER` off. Delete that folder in a build
  that must not carry it.
- The README's screenshots live in `Documentation~/`, a folder Unity never imports, so they reach neither a project's
  assets nor a build.
