using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Extra phrases a command can be found by in search, beyond its own name and group.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class DebugTagsAttribute : Attribute
	{
		/// <summary>The declared tags. Never null; empty entries are dropped.</summary>
		public IReadOnlyList<string> Tags { get; }

		public DebugTagsAttribute(params string[] tags)
		{
			if (tags == null || tags.Length == 0)
			{
				Tags = Array.Empty<string>();
				return;
			}

			List<string> cleaned = new List<string>(tags.Length);
			for (int i = 0; i < tags.Length; i++)
			{
				if (string.IsNullOrWhiteSpace(tags[i]))
				{
					continue;
				}

				cleaned.Add(tags[i]);
			}

			Tags = cleaned.ToArray();
		}
	}
}