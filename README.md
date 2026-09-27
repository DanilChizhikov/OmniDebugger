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

> **Current state:** the command core and the search engine are in place. The UI Toolkit panel is not built yet,
> so today this is the API a panel — or a console, or a test harness — is driven through.

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

There is no static `Instance`: you construct the debugger, you hold it, and you dispose it. Disposing releases
every registered source — MonoBehaviours included — and clears the catalog's subscribers.

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

## Code Stripping

OmniDebugger does not generate a `link.xml` and does not hook the build pipeline.

Usually nothing is needed: you pass a source *instance* to `AddSource`, so your own code references the type and
the managed stripper keeps it. Two cases do need a hint:

- a command method that nothing but OmniDebugger ever calls can still be stripped from an IL2CPP release build;
- an **enum used only in a command signature** keeps its type but can lose its field names, which breaks anything
  built from `Enum.GetNames`.

Mark those with `[UnityEngine.Scripting.Preserve]`, or preserve them through whichever `link.xml` your project
already maintains.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
