using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// One argument of a command: what it is called, what type it takes, and what it falls
	/// back to when a caller leaves it out.
	/// </summary>
	public sealed class ArgumentDefinition
	{
		/// <summary>Argument name, as written in the source.</summary>
		public string Name { get; }

		/// <summary>Declared argument type. May be a <see cref="Nullable{T}"/>.</summary>
		public Type Type { get; }

		/// <summary>Value used when the argument is omitted. Only meaningful if <see cref="IsOptional"/>.</summary>
		public object DefaultValue { get; }

		/// <summary>Whether a caller may omit this argument.</summary>
		public bool IsOptional { get; }

		/// <summary>
		/// The range declared with <see cref="DebugRangeAttribute"/>; empty when there is none. A hint for
		/// the panel, which edits a ranged number with a slider — invocations are not checked against it.
		/// </summary>
		public ArgumentRange Range { get; }

		/// <summary>
		/// Whether the value is picked from a list, declared with <see cref="DebugOptionsAttribute"/>. The list
		/// itself is read through <see cref="ICommandRegistry.TryGetOptions"/>, since it belongs to the object
		/// behind the command, not to its description.
		/// </summary>
		public bool HasOptions { get; }

		public ArgumentDefinition(
			string name,
			Type type,
			object defaultValue = null,
			bool isOptional = false,
			ArgumentRange range = default,
			bool hasOptions = false)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Argument name cannot be null or whitespace.", nameof(name));
			}

			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			Name = name;
			Type = type;
			DefaultValue = defaultValue;
			IsOptional = isOptional;
			Range = range;
			HasOptions = hasOptions;
		}

		public override string ToString()
		{
			string text = IsOptional
				? $"{Type.Name} {Name} = {DefaultValue ?? "null"}"
				: $"{Type.Name} {Name}";

			if (!Range.IsEmpty)
			{
				text = $"{text} {Range}";
			}

			return HasOptions ? $"{text} {{options}}" : text;
		}
	}
}