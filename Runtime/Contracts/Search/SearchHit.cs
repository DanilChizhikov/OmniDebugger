namespace DTech.OmniDebugger
{
	/// <summary>
	/// One matching item. Holds the item's id rather than the item itself, so a result list
	/// costs nothing to keep around; resolve it with the index's indexer.
	/// </summary>
	public readonly struct SearchHit
	{
		/// <summary>Id of the item, as handed out by <see cref="SearchIndex{T}.Add"/>.</summary>
		public int Id { get; }

		/// <summary>Higher is a better match. Only comparable within one query.</summary>
		public int Score { get; }

		/// <summary>The stage that completed this item's match.</summary>
		public SearchStage Stage { get; }

		public SearchHit(int id, int score, SearchStage stage)
		{
			Id = id;
			Score = score;
			Stage = stage;
		}

		public override string ToString() => $"#{Id} ({Score}, {Stage})";
	}
}