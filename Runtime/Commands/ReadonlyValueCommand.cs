using System;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// A read-only value backed by a delegate instead of an attributed property.
	/// </summary>
	[Preserve]
	public sealed class ReadonlyValueCommand : IReadableCommand
	{
		private readonly Func<object> _getter;

		public CommandDefinition Definition { get; }

		public ReadonlyValueCommand(CommandDefinition definition, Func<object> getter)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (definition.Kind != CommandKind.ReadonlyValue)
			{
				throw new ArgumentException(
					$"A {nameof(ReadonlyValueCommand)} needs a {nameof(CommandKind)}." +
					$"{nameof(CommandKind.ReadonlyValue)} definition, but '{definition.Key}' is {definition.Kind}.",
					nameof(definition));
			}

			Definition = definition;
			_getter = getter ?? throw new ArgumentNullException(nameof(getter));
		}

		public object GetValue() => _getter.Invoke();
	}
}