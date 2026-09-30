using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal readonly struct ParsedLogQuery
	{
		public readonly string Text;
		public readonly IReadOnlyCollection<string> Tags;
		public readonly IReadOnlyCollection<string> ExcludedTags;
		public readonly LogTypeMask Types;

		public ParsedLogQuery(string text, IReadOnlyCollection<string> tags, IReadOnlyCollection<string> excludedTags, LogTypeMask types)
		{
			Text = text;
			Tags = tags;
			ExcludedTags = excludedTags;
			Types = types;
		}
	}
}
