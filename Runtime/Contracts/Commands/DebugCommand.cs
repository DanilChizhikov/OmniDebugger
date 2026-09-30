using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// A command ready to register: its <see cref="CommandDefinition"/> plus the delegates that run it.
	/// The one command type there is — <see cref="CommandBuilder"/> and the generated code behind
	/// <see cref="DebugCommandAttribute"/> both produce it, and <see cref="ICommandRegistry.Add"/> takes it
	/// from anywhere else.
	/// </summary>
	public sealed class DebugCommand
	{
		private readonly Action<object[]> _invoke;
		private readonly Func<object> _get;
		private readonly Action<object> _set;

		/// <summary>What the command is. Never changes.</summary>
		public CommandDefinition Definition { get; }

		/// <summary>Whether the command can be run.</summary>
		public bool CanExecute => _invoke != null || _set != null;

		/// <summary>Whether the command reports a value.</summary>
		public bool CanRead => _get != null;

		/// <summary>Pairs <paramref name="definition"/> with the delegates its <see cref="CommandDefinition.Kind"/> needs.</summary>
		public DebugCommand(
			CommandDefinition definition,
			Action<object[]> invoke = null,
			Func<object> get = null,
			Action<object> set = null)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));

			switch (definition.Kind)
			{
				case CommandKind.Action:
					Require(invoke != null, "an action needs an invoke delegate");
					Require(get == null && set == null, "an action cannot have a getter or a setter");
					break;

				case CommandKind.Value:
					Require(get != null && set != null, "a value needs a getter and a setter");
					Require(invoke == null, "a value is written through its setter, not an invoke delegate");
					Require(definition.Arguments.Count == 1, "a value is described by exactly one argument");
					break;

				case CommandKind.ReadonlyValue:
					Require(get != null, "a read-only value needs a getter");
					Require(invoke == null && set == null, "a read-only value cannot be run");
					Require(definition.Arguments.Count == 1, "a read-only value is described by exactly one argument");
					break;

				default:
					throw new ArgumentException($"Unknown command kind: {definition.Kind}.", nameof(definition));
			}

			_invoke = invoke;
			_get = get;
			_set = set;

			void Require(bool condition, string reason)
			{
				if (!condition)
				{
					throw new ArgumentException($"Command '{definition.Path}' is invalid: {reason}.", nameof(definition));
				}
			}
		}

		public override string ToString() => Definition.ToString();

		internal void Execute(object[] arguments)
		{
			if (_set != null)
			{
				_set(arguments[0]);
				return;
			}

			_invoke(arguments);
		}

		internal object GetValue() => _get();
	}
}
