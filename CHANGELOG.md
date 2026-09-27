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
- `Project Settings → DTech → OmniDebugger` to toggle the `OMNI_DEBUGGER` define per build target.
- EditMode test suite covering scanning, the catalog, invocation, argument binding, and search.

### Notes
- No package dependencies. No UniTask, no Newtonsoft, no uGUI.
- The runtime assembly is gated behind `OMNI_DEBUGGER`; without the define nothing is compiled, so consumer
  calls belong inside `#if OMNI_DEBUGGER`.
- The UI Toolkit panel is not part of this release.
