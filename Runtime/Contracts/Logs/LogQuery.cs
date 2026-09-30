using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>What to read from an <see cref="ILogFeed"/>. The default value matches everything.</summary>
	public readonly struct LogQuery
	{
		/// <summary>Records in one page when <see cref="Limit"/> is not set.</summary>
		public const int DefaultLimit = 64;

		private readonly LogTypeMask _types;
		private readonly int _limit;

		/// <summary>
		/// Text looked up in the message and the stack trace, ignoring case. Every word has to appear, in any
		/// order; a phrase in double quotes has to appear as written. Blank matches all.
		/// </summary>
		public string Text { get; }

		/// <summary>A record has to carry at least one of these tags, ignoring case. Null or empty matches all.</summary>
		public IReadOnlyCollection<string> Tags { get; }

		/// <summary>A record carrying any of these tags is left out, ignoring case.</summary>
		public IReadOnlyCollection<string> ExcludedTags { get; }

		/// <summary>Which kinds of record match. <see cref="LogTypeMask.None"/> reads as all.</summary>
		public LogTypeMask Types => _types == LogTypeMask.None ? LogTypeMask.All : _types;

		/// <summary>When above zero, the page starts right after this record and grows towards the newest.</summary>
		public long AfterId { get; }

		/// <summary>When above zero and <see cref="AfterId"/> is not, the page ends right before this record.</summary>
		public long BeforeId { get; }

		/// <summary>Records in one page. <see cref="DefaultLimit"/> when not above zero.</summary>
		public int Limit => _limit > 0 ? _limit : DefaultLimit;

		public LogQuery(
			string text = null,
			IReadOnlyCollection<string> tags = null,
			LogTypeMask types = LogTypeMask.All,
			long afterId = 0,
			long beforeId = 0,
			int limit = DefaultLimit,
			IReadOnlyCollection<string> excludedTags = null)
		{
			Text = text;
			Tags = tags;
			ExcludedTags = excludedTags;
			_types = types;
			AfterId = afterId;
			BeforeId = beforeId;
			_limit = limit;
		}

		/// <summary>
		/// Reads the filter syntax the Logs tab's filter bar takes: <c>tag:Net</c> carries the tag (several match any
		/// of them), <c>-tag:Ads</c> does not, <c>type:error</c> keeps a kind (<c>log</c>, <c>warning</c> or
		/// <c>error</c>; asserts and exceptions count as errors) and <c>-type:log</c> leaves one out; anything else is
		/// text, as <see cref="Text"/> reads it. Values can be quoted: <c>tag:"Game Loop"</c>. Unknown
		/// <c>type:</c> values are read as text.
		/// </summary>
		public static LogQuery Parse(string filter, int limit = DefaultLimit)
		{
			ParsedLogQuery parsed = LogQuerySyntax.Parse(filter);
			return new LogQuery(parsed.Text, parsed.Tags, parsed.Types, limit: limit, excludedTags: parsed.ExcludedTags);
		}

		/// <summary>The same filter, paging forward from <paramref name="id"/>.</summary>
		public LogQuery After(long id) => new (Text, Tags, _types, id, 0, _limit, ExcludedTags);

		/// <summary>The same filter, paging back from <paramref name="id"/>.</summary>
		public LogQuery Before(long id) => new (Text, Tags, _types, 0, id, _limit, ExcludedTags);

		/// <summary>The same filter with another page size.</summary>
		public LogQuery WithLimit(int limit) => new (Text, Tags, _types, AfterId, BeforeId, limit, ExcludedTags);
	}
}
