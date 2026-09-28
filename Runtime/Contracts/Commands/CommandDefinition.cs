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
		public const int DefaultSortOrder = 100;
		
		private static readonly ArgumentDefinition[] _noArguments = Array.Empty<ArgumentDefinition>();
		private static readonly string[] _noTags = Array.Empty<string>();

		private readonly string[] _searchTags;

		/// <summary>Stable identifier, <c>"GroupName/Name"</c>. See <see cref="CommandKey"/>.</summary>
		public string Key { get; }

		/// <summary>Display name, unique within the group.</summary>
		public string Name { get; }

		/// <summary>Group this command is listed under.</summary>
		public string GroupName { get; }

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

		/// <summary>The icon declared with <see cref="DebugIconAttribute"/>; empty when there is none.</summary>
		public CommandIcon Icon { get; }

		public CommandDefinition(
			string name,
			string groupName,
			CommandKind kind,
			int sortOrder = DefaultSortOrder,
			IReadOnlyList<ArgumentDefinition> arguments = null,
			IEnumerable<string> tags = null,
			string description = null,
			CommandIcon icon = default)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Command name cannot be null or whitespace.", nameof(name));
			}

			if (string.IsNullOrWhiteSpace(groupName))
			{
				throw new ArgumentException("Group name cannot be null or whitespace.", nameof(groupName));
			}

			Name = name;
			GroupName = groupName;
			Kind = kind;
			SortOrder = sortOrder;
			Description = string.IsNullOrWhiteSpace(description) ? null : description;
			Arguments = arguments ?? _noArguments;
			Icon = icon;
			Key = CommandKey.Create(groupName, name);
			Tags = CollectDeclaredTags(tags);
			_searchTags = CollectDefaultTerms(name, groupName, tags);
		}
		
		/// <inheritdoc/>
		public void CollectIndexTerms(ICollection<string> terms)
		{
			for (int i = 0; i < _searchTags.Length; i++)
			{
				terms.Add(_searchTags[i]);
			}
		}

		public override string ToString() => $"{Key} ({Kind})";

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

		private static string[] CollectDefaultTerms(string name, string groupName, IEnumerable<string> tags)
		{
			List<string> result = new List<string>(4) { name, groupName };

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