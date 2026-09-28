using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>What to read from an <see cref="ILogFeed"/>. The default value matches everything.</summary>
	public readonly struct LogQuery
	{
		public const int DefaultLimit = 100;

		private readonly LogTypeMask _types;
		private readonly int _limit;

		/// <summary>Case-insensitive text looked up in the message and the stack trace. Blank matches all.</summary>
		public string Text { get; }

		/// <summary>Tags a record must carry. Null or empty matches all.</summary>
		public IReadOnlyCollection<string> Tags { get; }

		/// <summary>Whether a record needs every tag in <see cref="Tags"/> or any one of them.</summary>
		public LogTagMode TagMode { get; }

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
			LogTagMode tagMode = LogTagMode.All,
			LogTypeMask types = LogTypeMask.All,
			long afterId = 0,
			long beforeId = 0,
			int limit = DefaultLimit)
		{
			Text = text;
			Tags = tags;
			TagMode = tagMode;
			_types = types;
			AfterId = afterId;
			BeforeId = beforeId;
			_limit = limit;
		}

		/// <summary>The same filter, paging forward from <paramref name="id"/>.</summary>
		public LogQuery After(long id) => new (Text, Tags, TagMode, _types, id, 0, _limit);

		/// <summary>The same filter, paging back from <paramref name="id"/>.</summary>
		public LogQuery Before(long id) => new (Text, Tags, TagMode, _types, 0, id, _limit);

		/// <summary>The same filter with another page size.</summary>
		public LogQuery WithLimit(int limit) => new (Text, Tags, TagMode, _types, AfterId, BeforeId, limit);
	}
}