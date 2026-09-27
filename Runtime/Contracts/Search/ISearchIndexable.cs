using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// Something a <see cref="SearchIndex{T}"/> can find. The item pushes the phrases it wants
	/// to be found by instead of exposing a collection, so the index can fold them straight
	/// into its own storage and keep nothing of the item alive afterwards.
	/// </summary>
	public interface ISearchIndexable
	{
		/// <summary>
		/// Adds every phrase this item should be findable by. Called on the Unity main thread,
		/// once per indexing pass. Each phrase is split into terms on punctuation, on camel
		/// humps and between letters and digits, so <c>"Build Version"</c> is also found by
		/// <c>"version"</c>, by <c>"buildversion"</c> and by <c>"bv"</c>.
		/// </summary>
		/// <param name="terms">Collection to add to. Never null. Nulls and blanks are ignored.</param>
		void CollectIndexTerms(ICollection<string> terms);
	}
}