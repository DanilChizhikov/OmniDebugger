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

		/// <inheritdoc/>
		public CommandDefinition Definition { get; }

		/// <summary>A value read by <paramref name="getter"/> whenever the panel or a caller asks for it.</summary>
		/// <exception cref="ArgumentException">The definition is not a <see cref="CommandKind.ReadonlyValue"/>.</exception>
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

		/// <inheritdoc/>
		public object GetValue() => _getter.Invoke();
	}
}