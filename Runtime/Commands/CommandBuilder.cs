using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Describes commands from delegates, fluently, and registers them in one batch.
	/// Every command lands in the group named by the last <see cref="Group"/>. <see cref="Icon"/>,
	/// <see cref="Tags"/>, <see cref="Description"/> and <see cref="Order"/> describe the command added last.
	/// Nothing is registered until <see cref="Register"/>, and a builder registers once.
	/// </summary>
	public sealed class CommandBuilder
	{
		private readonly ICommandRegistry _registry;
		private readonly List<Pending> _commands = new ();

		private string _group;
		private bool _registered;

		internal CommandBuilder(ICommandRegistry registry)
		{
			_registry = registry ?? throw new ArgumentNullException(nameof(registry));
		}

		/// <summary>Puts the commands that follow in the group at <paramref name="groupPath"/>.</summary>
		public CommandBuilder Group(string groupPath)
		{
			string normalized = CommandPath.Normalize(groupPath);
			if (normalized.Length == 0)
			{
				throw new ArgumentException("Group path cannot be null or whitespace.", nameof(groupPath));
			}

			_group = normalized;
			return this;
		}

		/// <summary>A command that runs <paramref name="action"/>.</summary>
		public CommandBuilder Button(string name, Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			return AddAction(name, _ => action(), Array.Empty<ArgumentDefinition>());
		}

		/// <summary>A command that takes one argument.</summary>
		public CommandBuilder Button<T>(string name, Action<T> action, Action<ArgumentBuilder> argument = null)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			ArgumentDefinition[] arguments = { Describe<T>("value", argument) };
			return AddAction(name, values => action(Cast<T>(values[0])), arguments);
		}

		/// <summary>A command that takes two arguments.</summary>
		public CommandBuilder Button<T1, T2>(
			string name,
			Action<T1, T2> action,
			Action<ArgumentBuilder> first = null,
			Action<ArgumentBuilder> second = null)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			ArgumentDefinition[] arguments = { Describe<T1>("arg1", first), Describe<T2>("arg2", second) };
			return AddAction(name, values => action(Cast<T1>(values[0]), Cast<T2>(values[1])), arguments);
		}

		/// <summary>A command that takes three arguments.</summary>
		public CommandBuilder Button<T1, T2, T3>(
			string name,
			Action<T1, T2, T3> action,
			Action<ArgumentBuilder> first = null,
			Action<ArgumentBuilder> second = null,
			Action<ArgumentBuilder> third = null)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			ArgumentDefinition[] arguments =
			{
				Describe<T1>("arg1", first),
				Describe<T2>("arg2", second),
				Describe<T3>("arg3", third),
			};

			return AddAction(
				name,
				values => action(Cast<T1>(values[0]), Cast<T2>(values[1]), Cast<T3>(values[2])),
				arguments);
		}

		/// <summary>A switch bound to a <c>bool</c>.</summary>
		public CommandBuilder Toggle(string name, Func<bool> get, Action<bool> set) => Field(name, get, set);

		/// <summary>A slider bound to a <c>float</c>, snapped to <paramref name="step"/> when it is above zero.</summary>
		public CommandBuilder Slider(string name, Func<float> get, Action<float> set, float min, float max, float step = 0f) =>
			Field(name, get, set, argument => argument.Range(min, max, step));

		/// <summary>A slider bound to an <c>int</c>.</summary>
		public CommandBuilder Slider(string name, Func<int> get, Action<int> set, int min, int max) =>
			Field(name, get, set, argument => argument.Range(min, max));

		/// <summary>A dropdown bound to an enum.</summary>
		public CommandBuilder Dropdown<TEnum>(string name, Func<TEnum> get, Action<TEnum> set)
			where TEnum : struct, Enum =>
			Field(name, get, set);

		/// <summary>
		/// A value that can be read and written, edited with whatever control fits <typeparamref name="T"/>.
		/// </summary>
		public CommandBuilder Field<T>(string name, Func<T> get, Action<T> set, Action<ArgumentBuilder> argument = null)
		{
			if (get == null)
			{
				throw new ArgumentNullException(nameof(get));
			}

			if (set == null)
			{
				throw new ArgumentNullException(nameof(set));
			}

			ArgumentDefinition[] arguments = { Describe<T>(name, argument) };
			return AddValue(name, CommandKind.Value, arguments, () => get(), value => set(Cast<T>(value)));
		}

		/// <summary>A value that is only read, shown live.</summary>
		public CommandBuilder Value<T>(string name, Func<T> get)
		{
			if (get == null)
			{
				throw new ArgumentNullException(nameof(get));
			}

			ArgumentDefinition[] arguments = { Describe<T>(name, null) };
			return AddValue(name, CommandKind.ReadonlyValue, arguments, () => get(), null);
		}

		/// <summary>Icon of the command added last: a built-in glyph name or a key the icon provider knows.</summary>
		public CommandBuilder Icon(string key)
		{
			Last(nameof(Icon)).IconKey = key;
			return this;
		}

		/// <summary>Extra phrases the command added last can be found by.</summary>
		public CommandBuilder Tags(params string[] tags)
		{
			Last(nameof(Tags)).Tags = tags;
			return this;
		}

		/// <summary>Explanation of the command added last.</summary>
		public CommandBuilder Description(string description)
		{
			Last(nameof(Description)).Description = description;
			return this;
		}

		/// <summary>Sort order of the command added last; lower comes first within its group.</summary>
		public CommandBuilder Order(int order)
		{
			Last(nameof(Order)).Order = order;
			return this;
		}

		/// <summary>Registers everything described so far.</summary>
		/// <returns>A handle whose <c>Dispose</c> removes exactly these commands again.</returns>
		public IDisposable Register()
		{
			if (_registered)
			{
				throw new InvalidOperationException("This builder has already registered its commands.");
			}

			_registered = true;

			DebugCommand[] commands = new DebugCommand[_commands.Count];
			for (int i = 0; i < _commands.Count; i++)
			{
				commands[i] = _commands[i].Create();
			}

			return _registry.Add(commands);
		}

		private static T Cast<T>(object value) => value == null ? default : (T)value;

		private static ArgumentDefinition Describe<T>(string name, Action<ArgumentBuilder> configure)
		{
			ArgumentBuilder builder = new ArgumentBuilder(name, typeof(T), default(T));
			configure?.Invoke(builder);
			return builder.Build();
		}

		private CommandBuilder AddAction(string name, Action<object[]> invoke, ArgumentDefinition[] arguments)
		{
			_commands.Add(new Pending(RequireGroup(), name, CommandKind.Action, arguments) { Invoke = invoke });
			return this;
		}

		private CommandBuilder AddValue(
			string name,
			CommandKind kind,
			ArgumentDefinition[] arguments,
			Func<object> get,
			Action<object> set)
		{
			_commands.Add(new Pending(RequireGroup(), name, kind, arguments) { Get = get, Set = set });
			return this;
		}

		private string RequireGroup()
		{
			if (_registered)
			{
				throw new InvalidOperationException("This builder has already registered its commands.");
			}

			return _group ?? throw new InvalidOperationException("Call Group(path) before adding a command.");
		}

		private Pending Last(string modifier)
		{
			if (_commands.Count == 0)
			{
				throw new InvalidOperationException($"{modifier}() describes the command added last, and there is none yet.");
			}

			return _commands[_commands.Count - 1];
		}

		private sealed class Pending
		{
			private readonly string _group;
			private readonly string _name;
			private readonly CommandKind _kind;
			private readonly ArgumentDefinition[] _arguments;

			public Action<object[]> Invoke;
			public Func<object> Get;
			public Action<object> Set;
			public string IconKey;
			public string[] Tags;
			public string Description;
			public int Order = CommandDefinition.DefaultSortOrder;

			public Pending(string group, string name, CommandKind kind, ArgumentDefinition[] arguments)
			{
				CommandPath.Combine(group, name);

				_group = group;
				_name = name;
				_kind = kind;
				_arguments = arguments;
			}

			public DebugCommand Create()
			{
				CommandDefinition definition = new CommandDefinition(
					_name,
					_group,
					_kind,
					Order,
					_arguments,
					Tags,
					Description,
					IconKey);

				return new DebugCommand(definition, Invoke, Get, Set);
			}
		}
	}
}
