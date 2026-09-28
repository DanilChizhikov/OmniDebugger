using System;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// A command backed by a delegate instead of an attributed member. Use it when the command
	/// is built at runtime, lives on a type you do not own, or is easier to write than to annotate.
	/// </summary>
	[Preserve]
	public sealed class ActionCommand : IExecutableCommand
	{
		private readonly Action<object[]> _action;

		public CommandDefinition Definition { get; }

		public ActionCommand(CommandDefinition definition, Action<object[]> action)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (definition.Kind != CommandKind.Action)
			{
				throw new ArgumentException(
					$"An {nameof(ActionCommand)} needs a {nameof(CommandKind)}.{nameof(CommandKind.Action)} " +
					$"definition, but '{definition.Key}' is {definition.Kind}.",
					nameof(definition));
			}

			Definition = definition;
			_action = action ?? throw new ArgumentNullException(nameof(action));
		}

		/// <summary>Convenience overload for a command that takes no arguments.</summary>
		public ActionCommand(CommandDefinition definition, Action action)
			: this(definition, WrapParameterless(action))
		{
		}

		public void Execute(object[] arguments) => _action.Invoke(arguments);

		private static Action<object[]> WrapParameterless(Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			return _ => action.Invoke();
		}
	}
}