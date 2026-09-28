using System;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// What a field is asked to edit. <see cref="ValueType"/> is already unwrapped, so a handler for
	/// <c>int</c> also serves <c>int?</c> without knowing about nullability.
	/// </summary>
	public readonly struct ArgumentFieldRequest
	{
		/// <summary>The argument as the catalog describes it.</summary>
		public ArgumentDefinition Argument { get; }

		/// <summary>The argument's type with <see cref="Nullable{T}"/> peeled off.</summary>
		public Type ValueType { get; }

		/// <summary>Whether the declared type was nullable, so "no value" is a legal answer.</summary>
		public bool IsNullable { get; }

		/// <summary>Whether the field should show the argument's name next to itself.</summary>
		public bool ShowLabel { get; }

		/// <summary>
		/// What to show at first: a remembered entry, the argument's default, or null for an empty
		/// field.
		/// </summary>
		public object InitialValue { get; }

		public ArgumentFieldRequest(
			ArgumentDefinition argument,
			Type valueType,
			bool isNullable,
			bool showLabel,
			object initialValue)
		{
			Argument = argument;
			ValueType = valueType;
			IsNullable = isNullable;
			ShowLabel = showLabel;
			InitialValue = initialValue;
		}

		/// <summary>
		/// Builds a request from a definition, unwrapping <see cref="Nullable{T}"/> and falling back to
		/// the argument's default when no value is remembered.
		/// </summary>
		public static ArgumentFieldRequest For(ArgumentDefinition argument, object initialValue, bool showLabel)
		{
			if (argument == null)
			{
				throw new ArgumentNullException(nameof(argument));
			}

			Type declared = argument.Type;
			Type underlying = Nullable.GetUnderlyingType(declared);

			return new ArgumentFieldRequest(
				argument,
				underlying ?? declared,
				underlying != null,
				showLabel,
				initialValue ?? argument.DefaultValue);
		}
	}
}