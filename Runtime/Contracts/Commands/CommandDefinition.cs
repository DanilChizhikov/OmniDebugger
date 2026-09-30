using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Everything a caller or a UI needs to know about a command without invoking it.
	/// Immutable, and independent of whatever object actually backs the command, so one
	/// definition can be shared by every instance of the same declaring type.
	/// </summary>
	public sealed class CommandDefinition : ISearchIndexable
	{
		/// <summary>Sort order applied when a command does not state one.</summary>
		public const int DefaultSortOrder = 1000;
		
		private static readonly ArgumentDefinition[] _noArguments = Array.Empty<ArgumentDefinition>();
		private static readonly string[] _noTags = Array.Empty<string>();

		private readonly string[] _searchTags;

		/// <summary>Stable identifier, <c>"Group/Subgroup/Name"</c>. See <see cref="CommandPath"/>.</summary>
		public string Path { get; }

		/// <summary>Display name, unique within the group.</summary>
		public string Name { get; }

		/// <summary>Path of the group this command is listed under, <c>"Group/Subgroup"</c>.</summary>
		public string GroupPath { get; }

		/// <summary>How the command behaves when called.</summary>
		public CommandKind Kind { get; }

		/// <summary>Optional human readable explanation. May be null.</summary>
		public string Description { get; }

		/// <summary>Lower values are listed first within a group.</summary>
		public int SortOrder { get; }

		/// <summary>
		/// Arguments in call order. Never null; empty when the command takes none.
		/// For <see cref="CommandKind.Value"/> and <see cref="CommandKind.ReadonlyValue"/>
		/// this holds exactly one entry describing the value itself.
		/// </summary>
		public IReadOnlyList<ArgumentDefinition> Arguments { get; }

		/// <summary>Tags declared with <see cref="DebugTagsAttribute"/>, without the name and group. Never null.</summary>
		public IReadOnlyList<string> Tags { get; }

		/// <summary>
		/// The icon declared with <see cref="DebugIconAttribute"/>: the name of a built-in glyph, or a key the
		/// icon provider resolves. Null when there is none.
		/// </summary>
		public string IconKey { get; }

		/// <summary>Describes the command <paramref name="name"/> in the group <paramref name="groupPath"/>.</summary>
		public CommandDefinition(
			string name,
			string groupPath,
			CommandKind kind,
			int sortOrder = DefaultSortOrder,
			IReadOnlyList<ArgumentDefinition> arguments = null,
			IEnumerable<string> tags = null,
			string description = null,
			string iconKey = null)
		{
			Path = CommandPath.Combine(groupPath, name);
			Name = CommandPath.GetName(Path);
			GroupPath = CommandPath.GetParent(Path);
			Kind = kind;
			SortOrder = sortOrder;
			Description = string.IsNullOrWhiteSpace(description) ? null : description;
			Arguments = arguments ?? _noArguments;
			IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
			Tags = CollectDeclaredTags(tags);
			_searchTags = CollectDefaultTerms(Name, GroupPath, tags);
		}
		
		/// <inheritdoc/>
		public void CollectIndexTerms(ICollection<string> terms)
		{
			for (int i = 0; i < _searchTags.Length; i++)
			{
				terms.Add(_searchTags[i]);
			}
		}

		public override string ToString() => $"{Path} ({Kind})";

		private static string[] CollectDeclaredTags(IEnumerable<string> tags)
		{
			if (tags == null)
			{
				return _noTags;
			}

			List<string> result = null;

			foreach (string tag in tags)
			{
				if (string.IsNullOrWhiteSpace(tag))
				{
					continue;
				}

				result ??= new List<string>();

				if (!result.Contains(tag))
				{
					result.Add(tag);
				}
			}

			return result == null ? _noTags : result.ToArray();
		}

		private static string[] CollectDefaultTerms(string name, string groupPath, IEnumerable<string> tags)
		{
			List<string> result = new List<string>(4) { name };

			string[] groups = CommandPath.Split(groupPath);
			for (int i = 0; i < groups.Length; i++)
			{
				if (!result.Contains(groups[i]))
				{
					result.Add(groups[i]);
				}
			}

			if (tags != null)
			{
				foreach (string tag in tags)
				{
					if (string.IsNullOrWhiteSpace(tag) || result.Contains(tag))
					{
						continue;
					}

					result.Add(tag);
				}
			}

			return result.ToArray();
		}
	}
}