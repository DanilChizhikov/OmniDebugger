namespace DTech.OmniDebugger
{
	/// <summary>
	/// How much of a query has been done. Stages run cheapest first and each one scores lower
	/// than the one before it, so a caller that stops early still has the best hits.
	/// </summary>
	public enum SearchStage : byte
	{
		/// <summary>The typed word is a whole term.</summary>
		Exact = 0,

		/// <summary>A term starts with the typed word.</summary>
		Prefix = 1,

		/// <summary>A term contains the typed word somewhere inside it.</summary>
		Infix = 2,

		/// <summary>A term is within a small edit distance of the typed word.</summary>
		Fuzzy = 3,

		/// <summary>Nothing left to do.</summary>
		Done = 4,
	}
}