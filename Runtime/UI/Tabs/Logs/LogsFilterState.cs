using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LogsFilterState
	{
		private readonly HashSet<string> _tags = new (StringComparer.Ordinal);

		public string Text { get; set; } = string.Empty;

		public LogTypeMask Types { get; set; } = LogTypeMask.All;

		public LogTagMode TagMode { get; set; } = LogTagMode.All;

		public IReadOnlyCollection<string> Tags => _tags;

		public bool HasTag(string tag) => _tags.Contains(tag);

		public void ToggleTag(string tag)
		{
			if (!_tags.Remove(tag))
			{
				_tags.Add(tag);
			}
		}

		public void ClearTags() => _tags.Clear();

		public bool PruneTags(ICollection<string> known)
		{
			return _tags.RemoveWhere(tag => !known.Contains(tag)) > 0;
		}

		public LogQuery ToQuery(int limit) =>
			new (Text, _tags.Count > 0 ? _tags : null, TagMode, Types, limit: limit);
	}
}