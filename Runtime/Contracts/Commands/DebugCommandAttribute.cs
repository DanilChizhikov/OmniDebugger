using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Marks a method or property as a debug command. OmniDebugger's source generator turns every marked
	/// member into registration code at compile time, so nothing is read through reflection at runtime and
	/// nothing has to be kept from the IL2CPP linker. Hand an instance to
	/// <see cref="ICommandRegistry.Register(object)"/> to put its commands on the panel.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class DebugCommandAttribute : Attribute
	{
		/// <summary>Path of the group the command is listed under, such as <c>"Economy/Coins"</c>.</summary>
		public string GroupPath { get; }

		/// <summary>Display name. Falls back to the member name when not given.</summary>
		public string Name { get; set; }

		/// <summary>Lower values are listed first within the group.</summary>
		public int Order { get; set; } = CommandDefinition.DefaultSortOrder;

		/// <summary>Optional explanation shown next to the command.</summary>
		public string Description { get; set; }

		public DebugCommandAttribute(string groupPath)
		{
			if (string.IsNullOrWhiteSpace(groupPath))
			{
				throw new ArgumentException("Group path cannot be null or whitespace.", nameof(groupPath));
			}

			GroupPath = groupPath;
		}
	}
}
