using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DTech.OmniDebugger
{
	internal sealed class CommandRegistry : ICommandRegistry
	{
		public event Action OnChanged;

		private static readonly IReadOnlyList<CommandDefinition> _noDefinitions = Array.Empty<CommandDefinition>();

		private readonly Dictionary<string, Entry> _entries = new (StringComparer.Ordinal);
		private readonly Dictionary<object, Registration> _targets = new (ReferenceComparer.Instance);
		private readonly ILogSink _log;

		public IReadOnlyList<CommandDefinition> All
		{
			get
			{
				MainThreadGuard.Verify(nameof(All));
				ThrowIfDisposed();
				return _snapshot;
			}
		}

		private IReadOnlyList<CommandDefinition> _snapshot = _noDefinitions;
		private bool _disposed;

		public CommandRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));
		}

		public bool TryGet(string path, out CommandDefinition definition)
		{
			MainThreadGuard.Verify(nameof(TryGet));
			ThrowIfDisposed();

			if (TryGetCommand(path, out DebugCommand command))
			{
				definition = command.Definition;
				return true;
			}

			definition = null;
			return false;
		}

		public IDisposable Register(object target)
		{
			MainThreadGuard.Verify(nameof(Register));
			ThrowIfDisposed();

			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			string owner = target.GetType().Name;

			if (_targets.ContainsKey(target))
			{
				_log.Warning($"'{owner}' is already registered and was ignored.");
				return Registration.Empty;
			}

			List<DebugCommand> commands = new List<DebugCommand>();

			try
			{
				if (!CommandBinders.Collect(target, commands))
				{
					_log.Warning($"'{owner}' declares no commands. Commands come from [DebugCommand] on public or " +
						"internal instance members, compiled in an assembly that references OmniDebugger.");

					return Registration.Empty;
				}
			}
			catch (Exception exception)
			{
				_log.Exception($"Failed to read the commands of '{owner}'.", exception);
				return Registration.Empty;
			}

			Registration registration = AddInternal(commands, owner, target);
			if (registration.IsEmpty)
			{
				return registration;
			}

			_targets.Add(target, registration);
			return registration;
		}

		public bool Unregister(object target)
		{
			MainThreadGuard.Verify(nameof(Unregister));
			ThrowIfDisposed();

			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			if (!_targets.TryGetValue(target, out Registration registration))
			{
				return false;
			}

			registration.Dispose();
			return true;
		}

		public IDisposable Add(IEnumerable<DebugCommand> commands)
		{
			MainThreadGuard.Verify(nameof(Add));
			ThrowIfDisposed();

			if (commands == null)
			{
				throw new ArgumentNullException(nameof(commands));
			}

			return AddInternal(commands, "Code", null);
		}

		public CommandBuilder Build()
		{
			MainThreadGuard.Verify(nameof(Build));
			ThrowIfDisposed();
			return new CommandBuilder(this);
		}

		public bool TryExecute(string path, params object[] arguments) =>
			TryExecute(path, new InvocationRequest(InvocationRequest.UnknownOrigin, arguments));

		public bool TryExecute(string path, in InvocationRequest request)
		{
			MainThreadGuard.Verify(nameof(TryExecute));
			ThrowIfDisposed();

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException("Command path cannot be null or whitespace.", nameof(path));
			}

			if (!TryGetCommand(path, out DebugCommand command))
			{
				_log.Error($"Command was not found. Path: {path}; Origin: {request.Origin}.");
				return false;
			}

			return Execute(command, request);
		}

		public bool TryExecute(CommandDefinition definition, in InvocationRequest request)
		{
			MainThreadGuard.Verify(nameof(TryExecute));
			ThrowIfDisposed();

			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (!TryGetCommand(definition.Path, out DebugCommand command))
			{
				_log.Error($"Command was not found. Path: {definition.Path}; Origin: {request.Origin}.");
				return false;
			}

			return Execute(command, request);
		}

		public bool TryGetValue(string path, out object value)
		{
			MainThreadGuard.Verify(nameof(TryGetValue));
			ThrowIfDisposed();

			value = null;

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException("Command path cannot be null or whitespace.", nameof(path));
			}

			if (!TryGetCommand(path, out DebugCommand command))
			{
				_log.Error($"Command was not found. Path: {path}.");
				return false;
			}

			if (!command.CanRead)
			{
				_log.Error($"Command holds no value. Path: {path}; Kind: {command.Definition.Kind}.");
				return false;
			}

			try
			{
				value = command.GetValue();
				return true;
			}
			catch (Exception exception)
			{
				_log.Exception($"Reading a command value failed. Path: {path}.", exception);
				return false;
			}
		}

		public bool TryGetOptions(string path, int argumentIndex, ICollection<object> options)
		{
			MainThreadGuard.Verify(nameof(TryGetOptions));
			ThrowIfDisposed();

			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException("Command path cannot be null or whitespace.", nameof(path));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			if (!TryGetCommand(path, out DebugCommand command))
			{
				_log.Error($"Command was not found. Path: {path}.");
				return false;
			}

			if (!command.HasOptions(argumentIndex))
			{
				_log.Error($"Command argument has no options. Path: {path}; Argument: {argumentIndex}.");
				return false;
			}

			Type type = command.Definition.Arguments[argumentIndex].Type;

			try
			{
				IEnumerable source = command.GetOptions(argumentIndex);
				if (source == null)
				{
					return true;
				}

				foreach (object option in source)
				{
					if (option == null)
					{
						continue;
					}

					if (type.IsInstanceOfType(option))
					{
						options.Add(option);
					}
					else if (CommandArguments.TryConvert(option, type, out object converted))
					{
						options.Add(converted);
					}
				}

				return true;
			}
			catch (Exception exception)
			{
				_log.Exception($"Reading command options failed. Path: {path}; Argument: {argumentIndex}.", exception);
				return false;
			}
		}

		public void Clear()
		{
			_entries.Clear();
			_targets.Clear();
			_snapshot = _noDefinitions;
			OnChanged = null;
			_disposed = true;
		}

		private static string FormatInvocation(CommandDefinition definition, string origin, object[] arguments)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("Executing '").Append(definition.Path).Append("' from ").Append(origin);

			if (arguments == null || arguments.Length == 0)
			{
				return builder.Append('.').ToString();
			}

			builder.Append(" with (");

			for (int i = 0; i < arguments.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(Format(arguments[i]));
			}

			return builder.Append(").").ToString();
		}

		private static string Format(object value) => value switch
		{
			null => "null",
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};

		private bool TryGetCommand(string path, out DebugCommand command)
		{
			if (!string.IsNullOrWhiteSpace(path) && _entries.TryGetValue(CommandPath.Normalize(path), out Entry entry))
			{
				command = entry.Command;
				return true;
			}

			command = null;
			return false;
		}

		private Registration AddInternal(IEnumerable<DebugCommand> commands, string owner, object target)
		{
			List<DebugCommand> accepted = null;

			foreach (DebugCommand command in commands)
			{
				if (command == null)
				{
					_log.Warning($"'{owner}' handed over a null command, which was skipped.");
					continue;
				}

				string path = command.Definition.Path;

				if (_entries.TryGetValue(path, out Entry existing))
				{
					_log.Error($"'{owner}' wanted the path '{path}', which '{existing.Owner}' already holds. " +
						"Paths have to be unique, so this command was left out. Rename it or move it to another group.");

					continue;
				}

				_entries.Add(path, new Entry(command, owner));
				accepted ??= new List<DebugCommand>();
				accepted.Add(command);
			}

			if (accepted == null)
			{
				return Registration.Empty;
			}

			NotifyChanged();
			return new Registration(this, accepted.ToArray(), target);
		}

		private void Remove(Registration registration)
		{
			if (_disposed)
			{
				return;
			}

			MainThreadGuard.Verify("Dispose");

			if (registration.Target != null)
			{
				_targets.Remove(registration.Target);
			}

			bool changed = false;
			DebugCommand[] commands = registration.Commands;

			for (int i = 0; i < commands.Length; i++)
			{
				string path = commands[i].Definition.Path;

				if (_entries.TryGetValue(path, out Entry entry) && ReferenceEquals(entry.Command, commands[i]))
				{
					_entries.Remove(path);
					changed = true;
				}
			}

			if (changed)
			{
				NotifyChanged();
			}
		}

		private bool Execute(DebugCommand command, in InvocationRequest request)
		{
			CommandDefinition definition = command.Definition;

			if (!command.CanExecute)
			{
				_log.Error($"Command cannot be executed. Path: {definition.Path}; Kind: {definition.Kind}; Origin: {request.Origin}.");
				return false;
			}

			BindCommandResponse response = CommandArguments.TryBind(definition, request.Arguments);
			if (!response.Success)
			{
				_log.Error($"Command arguments are invalid. Path: {definition.Path}; Origin: {request.Origin}; Reason: {response.Error}.");
				return false;
			}

			_log.Info(FormatInvocation(definition, request.Origin, response.Bound));

			try
			{
				command.Execute(response.Bound);
				return true;
			}
			catch (Exception exception)
			{
				_log.Exception($"Command threw an exception. Path: {definition.Path}; Origin: {request.Origin}.", exception);
				return false;
			}
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
			public readonly DebugCommand Command;

			public readonly string Owner;

			public Entry(DebugCommand command, string owner)
			{
				Command = command;
				Owner = owner;
			}
		}

		private sealed class Registration : IDisposable
		{
			public static readonly Registration Empty = new Registration(null, Array.Empty<DebugCommand>(), null);

			public DebugCommand[] Commands { get; }

			public object Target { get; }

			public bool IsEmpty => Commands.Length == 0;

			private CommandRegistry _owner;

			public Registration(CommandRegistry owner, DebugCommand[] commands, object target)
			{
				_owner = owner;
				Commands = commands;
				Target = target;
			}

			public void Dispose()
			{
				CommandRegistry owner = _owner;
				if (owner == null)
				{
					return;
				}

				_owner = null;
				owner.Remove(this);
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
