using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DTech.OmniDebugger
{
	internal sealed class CommandCatalog : ICommandCatalog
	{
		public event Action OnChanged;
		
		private static readonly IReadOnlyList<CommandDefinition> _noDefinitions = Array.Empty<CommandDefinition>();

		private readonly Dictionary<string, Entry> _entries = new (StringComparer.Ordinal);
		private readonly Dictionary<object, IDebugCommand[]> _sources = new (ReferenceComparer.Instance);
		private readonly ILogSink _log;

		public IReadOnlyList<CommandDefinition> Commands
		{
			get
			{
				MainThreadGuard.Verify(nameof(Commands));
				ThrowIfDisposed();
				return _snapshot;
			}
		}

		private IReadOnlyList<CommandDefinition> _snapshot = _noDefinitions;
		private bool _disposed;

		public CommandCatalog(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public bool TryGetCommand(string key, out IDebugCommand command)
		{
			MainThreadGuard.Verify(nameof(TryGetCommand));
			ThrowIfDisposed();

			if (!string.IsNullOrWhiteSpace(key) && _entries.TryGetValue(key, out Entry entry))
			{
				command = entry.Command;
				return true;
			}

			command = null;
			return false;
		}

		public bool TryGetDefinition(string key, out CommandDefinition definition)
		{
			MainThreadGuard.Verify(nameof(TryGetDefinition));
			ThrowIfDisposed();

			if (!string.IsNullOrWhiteSpace(key) && _entries.TryGetValue(key, out Entry entry))
			{
				definition = entry.Command.Definition;
				return true;
			}

			definition = null;
			return false;
		}

		public bool AddSource(object source)
		{
			MainThreadGuard.Verify(nameof(AddSource));
			ThrowIfDisposed();

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			string owner = source.GetType().Name;

			if (_sources.ContainsKey(source))
			{
				_log.Warning($"Source is already registered and was ignored. Source: {owner}.");
				return false;
			}

			ScanResult scan;
			try
			{
				scan = SourceScanner.Scan(source, _log);
			}
			catch (Exception exception)
			{
				_log.Exception($"Failed to scan command source '{owner}'.", exception);
				return false;
			}

			if (scan.IsEmpty)
			{
				_log.Warning($"Source produced no commands and was not registered. Source: {owner}.");
				return false;
			}

			List<IDebugCommand> accepted = new List<IDebugCommand>(scan.Commands.Count);

			for (int i = 0; i < scan.Commands.Count; i++)
			{
				IDebugCommand command = scan.Commands[i];
				if (TryAddInternal(command, owner))
				{
					accepted.Add(command);
				}
			}

			if (accepted.Count == 0)
			{
				return false;
			}

			_sources[source] = accepted.ToArray();
			NotifyChanged();
			return true;
		}

		public bool RemoveSource(object source)
		{
			MainThreadGuard.Verify(nameof(RemoveSource));
			ThrowIfDisposed();

			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (!_sources.TryGetValue(source, out IDebugCommand[] registered))
			{
				_log.Warning($"Source is not registered. Source: {source.GetType().Name}.");
				return false;
			}

			_sources.Remove(source);

			for (int i = 0; i < registered.Length; i++)
			{
				RemoveInternal(registered[i]);
			}

			NotifyChanged();
			return true;
		}

		public bool AddCommand(IDebugCommand command)
		{
			MainThreadGuard.Verify(nameof(AddCommand));
			ThrowIfDisposed();

			if (command == null)
			{
				throw new ArgumentNullException(nameof(command));
			}

			if (command.Definition == null)
			{
				_log.Error($"Command of type '{command.GetType().Name}' has no definition and was skipped.");
				return false;
			}

			if (!TryAddInternal(command, command.GetType().Name))
			{
				return false;
			}

			NotifyChanged();
			return true;
		}

		public bool RemoveCommand(IDebugCommand command)
		{
			MainThreadGuard.Verify(nameof(RemoveCommand));
			ThrowIfDisposed();

			if (command == null)
			{
				throw new ArgumentNullException(nameof(command));
			}

			if (command.Definition == null || !RemoveInternal(command))
			{
				return false;
			}

			NotifyChanged();
			return true;
		}

		public void Clear()
		{
			_entries.Clear();
			_sources.Clear();
			_snapshot = _noDefinitions;
			OnChanged = null;
			_disposed = true;
		}

		private bool TryAddInternal(IDebugCommand command, string owner)
		{
			CommandDefinition definition = command.Definition;

			if (_entries.TryGetValue(definition.Key, out Entry existing))
			{
				_log.Error($"'{owner}' wanted the key '{definition.Key}', which '{existing.Owner}' already holds. " +
					"Keys have to be unique, so this command was left out. Rename it or move it to another group.");

				return false;
			}

			_entries.Add(definition.Key, new Entry(command, owner));
			return true;
		}

		private bool RemoveInternal(IDebugCommand command)
		{
			string key = command.Definition.Key;

			if (!_entries.TryGetValue(key, out Entry entry) || !ReferenceEquals(entry.Command, command))
			{
				return false;
			}

			_entries.Remove(key);
			return true;
		}

		private void NotifyChanged()
		{
			CommandDefinition[] snapshot = new CommandDefinition[_entries.Count];

			int index = 0;
			foreach (KeyValuePair<string, Entry> pair in _entries)
			{
				snapshot[index++] = pair.Value.Command.Definition;
			}

			_snapshot = snapshot;
			OnChanged?.Invoke();
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("OmniDebuggerHost");
			}
		}

		private readonly struct Entry
		{
			public readonly IDebugCommand Command;

			public readonly string Owner;

			public Entry(IDebugCommand command, string owner)
			{
				Command = command;
				Owner = owner;
			}
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new ReferenceComparer();

			public new bool Equals(object x, object y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}