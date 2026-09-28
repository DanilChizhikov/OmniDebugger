using System;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Marks a method or property as a debug command.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class DebugCommandAttribute : Attribute
	{
		/// <summary>Group the command is listed under.</summary>
		public string GroupName { get; }

		/// <summary>Display name. Falls back to the member name when not given.</summary>
		public string Name { get; }

		/// <summary>Lower values are listed first within the group.</summary>
		public int SortOrder { get; }

		/// <summary>Optional explanation shown next to the command.</summary>
		public string Description { get; set; }

		public DebugCommandAttribute(
			string groupName,
			string name = null,
			int sortOrder = CommandDefinition.DefaultSortOrder)
		{
			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw new ArgumentException("Group name cannot be null or whitespace.", nameof(groupName));
			}

			GroupName = groupName;
			Name = string.IsNullOrWhiteSpace(name) ? null : name;
			SortOrder = sortOrder;
		}
	}
}