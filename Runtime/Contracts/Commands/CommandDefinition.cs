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

		private static readonly ArgumentDefinition[] NoArguments = Array.Empty<ArgumentDefinition>();

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

		/// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="groupName"/> is null or whitespace.</exception>
		public CommandDefinition(
			string name,
			string groupName,
			CommandKind kind,
			int sortOrder = DefaultSortOrder,
			IReadOnlyList<ArgumentDefinition> arguments = null,
			IEnumerable<string> tags = null,
			string description = null)
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
			Arguments = arguments ?? NoArguments;
			Key = CommandKey.Create(groupName, name);
			_searchTags = CollectDefaultTerms(name, groupName, tags);
		}
		
		/// <inheritdoc/>
		/// <remarks>
		/// Offers <see cref="Name"/> and <see cref="GroupName"/> first, then any declared
		/// tags, so a command is found by its own name as readily as by the group it lives in.
		/// </remarks>
		public void CollectIndexTerms(ICollection<string> terms)
		{
			for (int i = 0; i < _searchTags.Length; i++)
			{
				terms.Add(_searchTags[i]);
			}
		}

		public override string ToString() => $"{Key} ({Kind})";

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