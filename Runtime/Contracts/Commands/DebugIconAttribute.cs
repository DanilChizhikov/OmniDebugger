using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// An icon shown next to the command in the panel: the name of a built-in glyph such as <c>"coin"</c> or
	/// <c>"bolt"</c>, or any key the debugger's icon provider knows how to load.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class DebugIconAttribute : Attribute
	{
		/// <summary>The icon key, carried over to <see cref="CommandDefinition.IconKey"/>.</summary>
		public string Key { get; }

		public DebugIconAttribute(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Icon key cannot be null or whitespace.", nameof(key));
			}

			Key = key;
		}
	}
}
