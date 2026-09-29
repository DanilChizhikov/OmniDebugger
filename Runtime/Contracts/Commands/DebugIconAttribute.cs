using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// An icon shown next to the command in the panel.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class DebugIconAttribute : Attribute
	{
		/// <summary>The icon, carried over to <see cref="CommandDefinition.Icon"/>.</summary>
		public CommandIcon Icon { get; }

		public DebugIconAttribute(DebugIconSource source, string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException("Icon key cannot be null or whitespace.", nameof(key));
			}

			Icon = new CommandIcon(source, key);
		}
	}
}