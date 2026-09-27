namespace DTech.OmniDebugger
{
	/// <summary>
	/// What one query is allowed to do. <c>default</c> is a sane search box: every stage,
	/// case ignored, at most <see cref="DefaultMaxResults"/> hits kept.
	/// </summary>
	public readonly struct QuerySettings
	{
		private const int DefaultMaxResults = 200;

		private readonly int _maxResults;

		/// <summary>
		/// How many hits to keep. Only the best ones survive, so this bounds the work a
		/// large result set can cause: nothing beyond this is ever sorted or stored.
		/// </summary>
		public int MaxResults => _maxResults > 0 ? _maxResults : DefaultMaxResults;

		/// <summary>Stage and case-matching knobs.</summary>
		public SearchOptions Options { get; }

		public QuerySettings(int maxResults, SearchOptions options = SearchOptions.None)
		{
			_maxResults = maxResults;
			Options = options;
		}

		public QuerySettings(SearchOptions options) : this(0, options)
		{
		}
	}
}