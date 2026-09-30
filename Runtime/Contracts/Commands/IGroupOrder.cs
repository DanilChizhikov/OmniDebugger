namespace DTech.OmniDebugger
{
	/// <summary>
	/// Display order of command groups, at any depth of their path. Purely presentational: nothing in the core reads it,
	/// it exists so a UI can put the groups you care about at the top.
	/// </summary>
	public interface IGroupOrder
	{
		/// <summary>
		/// Order returned for a group that was never configured. Exposed so a UI can show
		/// and compare against the same fallback the implementation uses.
		/// </summary>
		int DefaultOrder { get; }

		/// <summary>Sets the order of a group among its siblings, by its full path (<c>"Economy/Coins"</c>). Lower values are listed first.</summary>
		void SetOrder(string groupPath, int order);

		/// <summary>Reads a group's order, or <see cref="DefaultOrder"/> when it has none.</summary>
		int GetOrder(string groupPath);
	}
}