using System;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// A read/write value backed by delegates instead of an attributed property.
	/// Executing it writes; <see cref="GetValue"/> reads.
	/// </summary>
	[Preserve]
	public sealed class ValueCommand : IExecutableCommand, IReadableCommand
	{
		private readonly Func<object> _getter;
		private readonly Action<object> _setter;

		/// <inheritdoc/>
		public CommandDefinition Definition { get; }

		/// <summary>
		/// A value read by <paramref name="getter"/> and written by <paramref name="setter"/>. Give the
		/// definition exactly one <see cref="ArgumentDefinition"/> describing the value: its type picks the
		/// panel's control, and without it the panel has nothing to edit the value with.
		/// </summary>
		/// <exception cref="ArgumentException">The definition is not a <see cref="CommandKind.Value"/>.</exception>
		public ValueCommand(CommandDefinition definition, Func<object> getter, Action<object> setter)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			if (definition.Kind != CommandKind.Value)
			{
				throw new ArgumentException(
					$"A {nameof(ValueCommand)} needs a {nameof(CommandKind)}.{nameof(CommandKind.Value)} " +
					$"definition, but '{definition.Key}' is {definition.Kind}.",
					nameof(definition));
			}

			Definition = definition;
			_getter = getter ?? throw new ArgumentNullException(nameof(getter));
			_setter = setter ?? throw new ArgumentNullException(nameof(setter));
		}

		/// <inheritdoc/>
		public void Execute(object[] arguments)
		{
			if (arguments == null || arguments.Length != 1)
			{
				throw new ArgumentException(
					$"Command '{Definition.Key}' writes a single value and needs exactly one argument, " +
					$"but got {arguments?.Length ?? 0}.",
					nameof(arguments));
			}

			_setter.Invoke(arguments[0]);
		}

		/// <inheritdoc/>
		public object GetValue() => _getter.Invoke();
	}
}