namespace DTech.OmniDebugger
{
	/// <summary>
	/// Display order of command groups. Purely presentational: nothing in the core reads it,
	/// it exists so a UI can put the groups you care about at the top.
	/// </summary>
	public interface IGroupOrder
	{
		/// <summary>
		/// Order returned for a group that was never configured. Exposed so a UI can show
		/// and compare against the same fallback the implementation uses.
		/// </summary>
		int DefaultOrder { get; }

		/// <summary>Sets the order of a group. Lower values are listed first.</summary>
		void SetOrder(string groupName, int order);

		/// <summary>Reads a group's order, or <see cref="DefaultOrder"/> when it has none.</summary>
		int GetOrder(string groupName);
	}
}