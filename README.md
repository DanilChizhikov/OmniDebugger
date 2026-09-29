# OmniDebugger
[![Unity Version](https://img.shields.io/badge/unity-6000.0+-000.svg)](https://unity.com/releases/editor/archive)
![Unity Tests](https://github.com/DanilChizhikov/OmniDebugger/actions/workflows/tests.yml/badge.svg?branch=master)

## Overview
OmniDebugger is a runtime cheat and debug panel for Unity. It is built on UI Toolkit, so it draws on top of any
project without pulling in uGUI, TextMeshPro, or any other dependency.

Annotate a method or a property, hand the object over, and it becomes a debug command you can run by name:

```csharp
[DebugCommand("Economy", "Add Coins", Description = "Adds coins to the wallet.")]
[DebugTags("money", "wallet")]
public void AddCoins(int amount = 100) => _wallet.Coins += amount;
```

Then show it, on device or in a dockable editor window — the same panel either way:

```csharp
_debugger = new OmniDebugger();
_debugger.Catalog.AddSource(this);
OmniDebuggerPanel.Create(_debugger);
```

## Table of Contents
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Manual Installation](#manual-installation)
    - [UPM Installation](#upm-installation)
- [Enabling the Debugger](#enabling-the-debugger)
- [Usage](#usage)
    - [Declaring Commands](#declaring-commands)
    - [Commands Without Attributes](#commands-without-attributes)
    - [Running Commands](#running-commands)
    - [Searching](#searching)
- [The Panel](#the-panel)
    - [At Runtime](#at-runtime)
    - [Opening It](#opening-it)
    - [Locking It](#locking-it)
    - [Tabs](#tabs)
    - [Floating Windows](#floating-windows)
    - [Command Icons](#command-icons)
    - [In the Editor](#in-the-editor)
- [Themes](#themes)
- [Extending the Panel](#extending-the-panel)
    - [Your Own Tab](#your-own-tab)
    - [Your Own Argument Field](#your-own-argument-field)
- [Code Stripping](#code-stripping)
- [License](#license)

## Getting Started

### Prerequisites
- [GIT](https://git-scm.com/downloads)
- [Unity](https://unity.com/releases/editor/archive) 6000.0+

### Manual Installation
1. Download the .unitypackage from the [releases](https://github.com/DanilChizhikov/OmniDebugger/releases/) page.
2. Import com.dtech.omnidebugger.x.x.x.unitypackage into your project.

### UPM Installation
1. Open the manifest.json file in your project's Packages folder.
2. Add the following line to the dependencies section:
    ```json
    "com.dtech.omnidebugger": "https://github.com/DanilChizhikov/OmniDebugger.git",
    ```
3. Unity will automatically import the package.

If you want to set a target version, OmniDebugger uses the `v*.*.*` release tag so you can specify a version like #v1.0.0.

For example `https://github.com/DanilChizhikov/OmniDebugger.git#v1.0.0`.

## Enabling the Debugger

The runtime assembly is gated behind the `OMNI_DEBUGGER` scripting define. Without it the assembly is not
compiled at all, so a shipping build carries none of it — no stripped-down stub, no no-op calls, nothing.

Turn it on per build target in **Project Settings → DTech → OmniDebugger**. Because the define may be on for
one platform and off for another, and off entirely in a release build, **your own calls into OmniDebugger belong
inside `#if OMNI_DEBUGGER`**:

```csharp
#if OMNI_DEBUGGER
private OmniDebugger _debugger;
#endif

private void Awake()
{
#if OMNI_DEBUGGER
    _debugger = new OmniDebugger();
    _debugger.Catalog.AddSource(this);
    _debugger.Groups.SetOrder("Economy", 10);
#endif
}

private void OnDestroy()
{
#if OMNI_DEBUGGER
    _debugger?.Dispose();
#endif
}
```

That debugger is yours: you construct it, you hold it, and you dispose it. Disposing releases every registered
source — MonoBehaviours included — and clears the catalog's subscribers.

Or leave the lifetime to the package and read `OmniDebugger.Shared`. It hands out the first debugger built and not
disposed yet, and while there is none it builds one from the project settings right there, so any script reaches
the same debugger without passing it around:

```csharp
#if OMNI_DEBUGGER
private void OnEnable() => OmniDebugger.Shared.Catalog.AddSource(this);

private void OnDisable()
{
    if (OmniDebugger.TryGetShared(out OmniDebugger debugger))
    {
        debugger.Catalog.RemoveSource(this);
    }
}
#endif
```

`TryGetShared` reads the slot without building anything, which is what teardown code wants. A debugger `Shared`
built itself is disposed by the package once play mode is over in the editor; one your code built stays yours to
dispose, and a debugger built next to it never replaces it. Turn on *Create On Startup* (see
[At Runtime](#at-runtime)) to have the shared debugger built before the first scene loads.

## Usage

### Declaring Commands

Put `[DebugCommand(group, name, sortOrder)]` on a **public instance** member. `name` defaults to the member name,
and `Description` is an optional named argument. `[DebugTags]` adds extra phrases the command can be searched by.

| Member | Becomes | Notes |
|---|---|---|
| `void` method | `CommandKind.Action` | One argument per parameter; optional parameters stay optional |
| property with public get **and** set | `CommandKind.Value` | Running it writes the value |
| property with public get only | `CommandKind.ReadonlyValue` | A private setter counts as read-only |

Static members, non-public methods, methods that return a value, generic methods, `ref`/`out` parameters, and
properties without a public getter are skipped with a warning naming the member and the reason — so a rejected
command is never silently missing. A property is judged by its getter alone: a public get makes it a command
however private its setter is.

Arguments accept anything `IConvertible` (all numerics, `bool`, `char`, `string`, `DateTime`), any `enum`, and
`Nullable<T>` of those. Text is always read with the invariant culture, so a device locale can never turn `"1.5"`
into `15`.

Registration is always explicit. Nothing scans your assemblies, so startup costs only what you hand over — and
each type is reflected over exactly once for the lifetime of the domain, however many instances you register.

```csharp
_debugger.Catalog.AddSource(myShopService);
_debugger.Catalog.RemoveSource(myShopService);   // removes exactly what that object contributed
```

### Commands Without Attributes

For commands built at runtime, or on a type you do not own:

```csharp
_debugger.Catalog.AddCommand(new ActionCommand(
    new CommandDefinition("Reload Scene", "Flow", CommandKind.Action),
    () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)));

_debugger.Catalog.AddCommand(new ReadonlyValueCommand(
    new CommandDefinition("FPS", "Stats", CommandKind.ReadonlyValue),
    () => 1f / Time.smoothDeltaTime));
```

A whole object can also supply its own list by implementing `ICommandSource`, which opts it out of reflection
entirely.

### Running Commands

Commands are addressed by a readable key, `"Group/Name"`, so it can be typed into a console or stored in a config.

```csharp
_debugger.Commands.TryExecute("Economy/Add Coins", InvocationRequest.From("Console", 500));
_debugger.Commands.TryExecute("Economy/Add Coins", InvocationRequest.From("Console", "500"));  // text is coerced
_debugger.Commands.TryGetValue("Stats/FPS", out object fps);
```

`TryExecute` returns `false` and logs the reason when the command is missing, is not runnable, gets arguments it
cannot bind, or throws. A command that throws never escapes into the caller, and the exception is unwrapped so the
console shows your stack rather than reflection's. A null or blank key is the one case that is not a `false`: it
throws `ArgumentException`, because an empty key is a bug in the caller rather than a command that failed.

Every invocation is logged *before* it runs — a command that hard-kills the process still leaves a record of what
did it.

The catalog itself is readable the same way: `Commands` is an `IReadOnlyList<CommandDefinition>` snapshot,
`TryGetDefinition` looks one up by key, and `TryGetCommand` hands back the `IDebugCommand` behind it when you want
to call `Execute` or `GetValue` yourself.

```csharp
_debugger.Catalog.OnChanged += Refresh;   // fires after every add or remove
```

`OnChanged` is what a panel — or a search index — hangs off of, since registration can happen at any point in a
session. Disposing the debugger drops every subscriber along with the commands.

All of `ICommandCatalog`, `ICommandInvoker`, and `IGroupOrder` are main-thread only and throw
`InvalidOperationException` when called from anywhere else, rather than quietly doing nothing.

### Searching

`SearchIndex<T>` finds anything implementing `ISearchIndexable`, which `CommandDefinition` does. Items sit behind
an inverted index, so a query reads the words that were typed and the items those words belong to — never the
whole catalog.

```csharp
SearchIndex<CommandDefinition> index = new SearchIndex<CommandDefinition>();
index.Rebuild(_debugger.Catalog.Commands);          // re-index when the catalog changes

SearchSession<CommandDefinition> session = index.BeginQuery(_field.value, default);
session.OnUpdated += Repaint;                       // hits are republished after each stage

// Drive it with whatever slice of the frame you can spare.
session.Advance(budgetTicks: Stopwatch.Frequency / 2000);   // ~0.5 ms

foreach (SearchHit hit in session.Hits)
{
    CommandDefinition command = index[hit.Id];      // a hit is an id, a score, and the stage that found it
}
```

A hit does not carry the item — `SearchHit` is `Id`, `Score`, and `Stage`, and the indexer turns the id back into
what you put in. Ids are stable for the life of an entry and are never handed out twice, so one can be held across
frames; `index.Count` is how many entries are live.

An index keeps a single session. `BeginQuery` restarts the one it owns, so a second call replaces the query that
was running rather than starting a second one — which is what a panel that re-queries on every keystroke wants,
and why the returned session should not be stored beyond the current query.

Work is handed out in slices the caller pays for, so a search never owns a frame. Stages run cheapest first —
`SearchStage.Exact` (whole word), `Prefix`, `Infix` (substring), then `Fuzzy` (near-miss) — and `session.Hits` is
rebuilt after each one, best first. The first slices already hold what a person is most likely to want, while the
expensive near-miss work is still outstanding; `session.Stage` says what is being worked on and
`session.IsComplete` says when there is nothing left. `hit.Stage` is how good a match it was, so it also reads as
"exact matches above guesses".

`Advance` takes a budget in `Stopwatch` ticks and returns `true` while work remains. A budget of zero still does
one unit of work, so too small a slice cannot livelock the caller. To spend a budget per frame without writing the
loop, `await session.RunAsync(budgetTicks, cancellationToken)`; `session.Cancel()` stops a query where it stands
and leaves `Hits` readable.

Each phrase is indexed as its own words, as the words run together, and as their initials, so *Add Coins* is
found by `"coins"`, `"add coins"`, `"addcoins"` and `"ac"` alike. Run-together names are split on camel humps and
where letters meet digits. Near-miss matching uses edit distance, so `"coibs"` still finds *Add Coins*. Every
extra word narrows the result rather than widening it.

`QuerySettings` caps how many hits are kept and can turn stages off (`SearchOptions.ExactOnly`,
`SearchOptions.NoFuzzy`) or demand matching letter case (`SearchOptions.CaseSensitive`). An empty term completes
immediately with no hits — read that as "show everything".

For a large set, `RebuildAsync` gathers phrases on the main thread and builds the index on a worker, swapping it
in when it is ready; queries keep answering from the previous index until then. `Add` and `Remove` keep
single-item changes cheap, and the index folds them into the bulk data on its own once they add up. `Clear` empties
it without throwing the index away, and `Dispose` retires it for good — every call after that throws
`ObjectDisposedException`.

Like the catalog, the index is main-thread only, `RebuildAsync`'s worker excepted, and says so by throwing rather
than by misbehaving quietly.

## The Panel

The panel is one view (`OmniDebuggerView`) mounted in two places: over the running game, and in an editor
window. Both show the same tabs and themes; they differ only in where they draw and where they save choices.

The theme and favourites are the only choices the panel saves. Pins, the open tab and group, and where the open
button was dragged to survive the panel being rebuilt, but not a restart.

### At Runtime

Constructing the debugger in play mode is all it takes. It builds the panel, the open button and the log capture
itself, and takes them down again on `Dispose`:

```csharp
#if OMNI_DEBUGGER
private OmniDebugger _debugger;

private void Awake()
{
    _debugger = new OmniDebugger();
    _debugger.Catalog.AddSource(this);
}

private void OnDestroy() => _debugger?.Dispose();
#endif
```

With *Create On Startup* on, not even that is needed: the package builds `OmniDebugger.Shared` before the first
scene loads, so the log is captured from the very first frame and the panel is there without a line of code.
Scripts register their commands through `OmniDebugger.Shared`. A debugger built with `new` next to it puts a second
panel on screen, and says so in the console.

How it looks and opens is set in **Project Settings → DTech → OmniDebugger → Panel**. The page shows up while
`OMNI_DEBUGGER` is on for the active build target:

| Section | Options |
|---|---|
| Startup | `CreateOnStartup`, `CreatePanel`, `OpenOnStart` |
| Scaling and Layering | `ScaleMode`, `Scale`, `SortingOrder`, `PanelSettings` (left empty, the shipped asset is cloned, never edited) |
| Open Button | `ButtonEnabled` (*Open Button Enabled*), `ButtonClicks`, `MultiClickWindow`, `ButtonAnchor`, `ButtonOpacity` |
| Shortcuts | `Shortcuts` |
| Themes | `DefaultTheme`, extra `Themes` |
| Icons | `IconCatalogs` |

The settings live in `ProjectSettings/OmniDebuggerSettings.asset`: versioned with the project, nothing added to
`Assets`. Play mode reads them live: a debugger built without options in code, and its panel, pick up every edit
made on the page while the game runs. A build gets a snapshot — right before it
starts, the settings are written to a generated `Resources` asset, which is deleted again once the build is done.
That only happens while `OMNI_DEBUGGER` is on for the target, so a release build carries neither the settings nor
the themes, catalogs and panel settings they point at.

`new OmniDebugger()` reads these settings, and so does `OmniDebuggerOptions.Default`, which hands out a copy — change
a few values in code and pass it on:

```csharp
OmniDebuggerOptions options = OmniDebuggerOptions.Default;       // the project settings, copied
options.Panel.Open.Shortcuts.Add(new OmniDebuggerShortcut(KeyCode.LeftControl, KeyCode.BackQuote));

_debugger = new OmniDebugger(options);
```

Options passed to the debugger replace the project settings as a whole — later edits to the page no longer reach it;
`new OmniDebuggerOptions()` starts from the package's built-in defaults instead.

Set `CreatePanel = false` to build the panel yourself instead: drop `OmniDebuggerPanel` on a GameObject and call
`Bind(debugger)`. The component has no options of its own — it reads the same project settings, and code can change
`panel.Options` before binding. Its `UIDocument` is optional: leave it empty and one is created.

**Scaling.** Every size is written for a portrait phone 360 units wide. `ScreenSize` scales that with the screen,
so the panel looks the same on every phone and leans towards the height in landscape instead of tripling in size.
`PhysicalSize` reads the same units as desktop pixels at 96 DPI. `Auto`, the default, picks `ScreenSize` on mobile
platforms and `PhysicalSize` elsewhere. `OmniDebuggerPanel.SetScale` changes it on the spot.

**Layout.** Held upright, the tabs run along the top; turned sideways, they move into a sidebar. Card grids switch
to two columns once there is room. The panel keeps out of the notch and the home indicator, and the header paints
the notch in its own colour.

**Input after an EventSystem is rebuilt.** With uGUI in the project, UI Toolkit routes taps through an object Unity
parents under the EventSystem. The panel owns that object itself and re-asserts it every frame, so destroying the
EventSystem and creating a new one does not leave it deaf to taps.

### Opening It

| Way in | Configured by |
|---|---|
| The floating button — tap it, or tap it `ButtonClicks` times in a row | Panel settings → *Open Button* (`Open.ButtonEnabled`, `Open.ButtonClicks`, `Open.MultiClickWindow`, `Open.ButtonAnchor`, `Open.ButtonOpacity`) |
| A keyboard shortcut — one key, or a chord that fires when its last key goes down; each toggles the panel | Panel settings → *Shortcuts* (`Open.Shortcuts`) |
| Code | `OmniDebuggerPanel.Open()` / `Close()` / `Toggle()` |

It starts at the corner or edge `ButtonAnchor` names, at `ButtonOpacity` (0.5 by default), and lights up — full
opacity, accent border — on every tap, so a series of taps shows each one landed.
Hold the button for 0.6 seconds and corner brackets slide out: now it can be dragged. It stays where it was dropped
for the session, and inside the safe area when the screen turns. It blinks red for 45 seconds whenever an error is
logged, until it is tapped. Hide it at runtime with `SetOpenButtonEnabled(false)`, or replace it with
`SetGesture(IOmniDebuggerGesture)`.

To bind a shortcut, press *Add Shortcut*, click the new field, hold the keys and release them; `Esc` cancels and `×`
removes the row. Shortcuts are `KeyCode`s whichever input backend runs: the Input System package is used when it is
installed and active, the legacy Input Manager otherwise. Shift, Ctrl, Alt and Cmd/Win match either side of the
keyboard. Nothing is required: with neither backend, shortcuts stay silent and the button still works.

### Locking It

Panel settings → *Lock* makes the runtime panel ask for a PIN or a password before it shows. Every way in — the
button, a shortcut, `Open()` and *Open On Start* — goes through the prompt; the editor window never asks.

| Option | What it does |
|---|---|
| `Lock.Mode` | `None`, `Pin` — 4 to 12 digits on the panel's own keypad (a hardware keyboard types too), or `Password` — any characters in a masked field |
| *New PIN / New Password* → *Set* | Stores a salted SHA-256 hash of what was typed; *Clear* forgets it. From code: `Lock.SetSecret("1234")` / `Lock.ClearSecret()` |
| `Lock.UnlockScope` | `EveryOpen` asks each time, `Session` once until the app restarts, `Device` once on this device until the secret changes |
| `Lock.SkipInEditor` | On by default: play mode in the editor opens without asking |
| `Lock.MaxAttempts` / `Lock.CooldownSeconds` | After 5 wrong entries in a row the prompt pauses for 30 seconds; 0 attempts never pauses |

Neither the project settings nor a build hold the secret itself, only its hash. That keeps testers and players from
wandering in, not a determined attacker: the hash ships with the build, and a short PIN is quick to guess from it.
With a mode picked but no secret set, the panel opens without asking and the settings page warns about it.

### Tabs

| Tab | What it does |
|---|---|
| **Info** | Build, application, display, device, and live runtime figures (FPS, frame time, memory, battery, network) |
| **Commands** | A grid of groups, favourites first; open one for its commands. Each card shows the icon, tags, id, an ⓘ for the description, a pin and a star, then the controls: arguments and *Execute*, a value to edit, or a live read-only value |
| **Search** | Finds commands by name, group or tag, optionally case-sensitive; tap a result to run it |
| **Logs** | Unity's console, captured since the debugger was built: type toggles with counts, text search over messages and stack traces, `[Tag]` prefixes to filter by (all or any), copy one or everything, clear. It follows new logs while scrolled to the bottom and loads older ones at the top |
| **Windows** | Every floating window, to show or hide it |

Favourites are saved; pins last for the session only. The captured log is also readable in code through
`debugger.Logs` (`ILogFeed`).

The log keeps up to 16 384 records within a budget of about 8 MB of text, dropping the oldest first. A message
repeated word for word is stored once and shared by its records, so repeats cost a record but no text, and a
filter reads each distinct message once rather than once per repeat.

### Floating Windows

Windows float over the game while the panel is closed — a live readout, or a few commands to hit while playing.
Drag one by its header, collapse it, close it; it turns opaque while you use it.

```csharp
debugger.Windows.RegisterCustom("stats", "Stats", content => content.Add(new Label("…")), open: true);
debugger.Windows.RegisterCommands("cheats", "Cheats", new[] { "Economy/AddCoins", "Economy/Coins" });
debugger.Windows.Open("cheats");
debugger.Windows.Unregister("stats");
```

Pinning a command collects it in the built-in *Pinned* window.

### Command Icons

```csharp
[DebugCommand("Economy"), DebugIcon(DebugIconSource.Resources, "Icons/Coin")]
public void AddCoins(int amount) { … }
```

`Resources` keys load a sprite or texture from a `Resources` folder. `Catalog` keys are looked up in
`OmniDebuggerIconCatalog` assets (**Create → DTech → OmniDebugger → Icon Catalog**) found in a
`Resources/OmniDebugger` folder, listed under *Icons* in the Panel settings, or passed to `debugger.Icons.AddCatalog`. Register an
`IOmniDebuggerIconProvider` to serve icons from anywhere else.

### In the Editor

`Window → DTech → OmniDebugger` opens the same panel in a dockable window. It binds on its own to the newest live
debugger — every `OmniDebugger` announces itself on construction.

The window saves its theme and favourites in `EditorPrefs` while the game saves its own in `PlayerPrefs`, so the
two never move each other. The window's theme can also be picked in **Project Settings → DTech → OmniDebugger → UI**. Floating windows and pins belong to the runtime panel only.

The same page sets how the window shows the panel, per user in `EditorPrefs` and live:

- **Layout** — *Auto* follows the window's shape (tabs on top while it is taller than wide, in a sidebar otherwise);
  *Portrait* and *Landscape* keep one layout whatever the shape.
- **Zoom** — the panel is scaled to fit the window the way `ScreenSize` scales it on a device; 1 is the size it has
  on a phone as big as the window, lower is smaller and fits more. It runs from 0.5 to 1.25.

## Themes

A theme is an `OmniDebuggerTheme` asset holding an ordered list of plain `.uss` sheets. The panel applies its own
skin first, then a complete set of dark tokens, then your sheets — last, so your values win. Because the token
set underneath is always complete, a theme that redefines one variable is perfectly valid.

```css
/* Assets/UI/OceanTheme.uss */
.od-root {
    --od-color-bg: rgb(15, 23, 36);
    --od-color-card: rgb(22, 33, 51);
    --od-color-text: rgb(226, 236, 248);
    --od-color-accent: rgb(56, 189, 248);
    --od-color-control-selected: rgb(12, 74, 110);
    --od-radius-lg: 10px;
}
```

1. **Create → DTech → OmniDebugger → Theme**, name it, drag the `.uss` into *Style Sheets*.
2. Set it as *Default Theme* or list it under *Themes* in the Panel settings, or call
   `debugger.Themes.Register(theme)`.
3. The panel's header offers it, and the editor window's settings list it. No code.

What the header offers depends on what is set:

| Set | Themes offered | Switcher |
|---|---|---|
| Nothing | built-in Dark and Light | one button toggling the two |
| *Default Theme* only | that theme alone | hidden |
| *Themes* (or `Register`), with or without *Default Theme* | built-in Dark and Light, the listed ones and the default | dropdown, starting from *Default Theme* |

The package's `Example/Themes/ExampleSunsetTheme` is a ready-made theme to try either way.

Every colour, spacing, radius, font size and metric the panel draws with is a `--od-*` variable; the full list is
in `Runtime/UI/Resources/OmniDebugger/OmniDebuggerTokensDark.uss`. Want a light base? List the package's
`OmniDebuggerTokensLight.uss` first in your theme, then your own sheet. One variable is read from C# and must
stay unitless: `--od-value-refresh-ms` (how often read-only values are re-read). The built-in palettes are
Graphite + Teal, dark and light.

Use `.uss`, not `.tss`: Unity marks a theme style sheet as a default sheet, and default sheets lose every
specificity tie, so overrides in a `.tss` would silently do nothing. The one `.tss` the package ships is the
panel-level theme a runtime panel needs for Unity's own controls to render at all.

## Extending the Panel

### Your Own Tab

```csharp
internal sealed class SavesTabFactory : IOmniDebuggerTabFactory
{
    public string Id => "saves";
    public string DisplayName => "Saves";
    public int Order => 35;   // built-ins: Info 0, Commands 10, Search 20, Logs 30, Windows 40

    public IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context) => new SavesTab(context);
}

debugger.Tabs.Register(new SavesTabFactory());
```

The context hands you the debugger, the preference store, the origin string to invoke with, and a state object
that outlives your elements — park anything that must survive a rebuild in `context.State` rather than in a
field. `OnOpen` / `OnClose` tell you when to start and stop timers; a tab that keeps ticking while hidden is how
a debug panel drains a battery.

### Your Own Argument Field

```csharp
internal sealed class Vector3FieldHandler : IArgumentFieldHandler
{
    public int Priority => 10;                                    // above the built-ins
    public bool CanHandle(Type valueType) => valueType == typeof(Vector3);
    public IArgumentField Create(in ArgumentFieldRequest request) => new Vector3ArgumentField(request);
}

debugger.Fields.Register(new Vector3FieldHandler());
```

Built in already: `bool`, every enum, every numeric type, `char`, `string`, anything else convertible from text,
and `Nullable<T>` of all of them. A type nobody claims still renders — as a disabled field saying so — and the
command stays runnable when that argument is optional.

## Code Stripping

Nothing to do. While `OMNI_DEBUGGER` is on for the build target, OmniDebugger hands the linker a `link.xml` it
generates for that build (`Temp/OmniDebugger/CommandsLink.xml`). It keeps, whole, every type that declares a
command — by the same rules the catalog registers them — and every enum used in a command's arguments or value, so
a command nothing else calls survives an IL2CPP build and an enum keeps the names its dropdown shows. Only
assemblies that reference OmniDebugger are searched, since no other assembly can declare a command. With the
define off, nothing is generated. The only other build hook copies the project settings into the build (see
[At Runtime](#at-runtime)).

The search reads the scripts as the editor compiled them, so two cases still need a hint:

- a command inside a precompiled DLL, which is not searched;
- a command declared under `#if !UNITY_EDITOR`, which the editor never sees.

Mark those with `[UnityEngine.Scripting.Preserve]`, or preserve them through whichever `link.xml` your project
already maintains.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
