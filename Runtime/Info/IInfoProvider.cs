namespace DTech.OmniDebugger
{
	/// <summary>
	/// One section of the Info tab: a title and the rows under it. Register one with
	/// <see cref="IOmniDebuggerHost.Info"/> to show figures of your own next to the built-in Performance,
	/// Memory, Graphics, Quality, Screen, Build and Device sections — and, through
	/// <see cref="IInfoRegistry.Float"/>, float it over the game.
	/// </summary>
	public interface IInfoProvider
	{
		/// <summary>Heading of the section.</summary>
		string Title { get; }

		/// <summary>Position among the sections; lower comes first. The built-in ones sit at 0 to 60.</summary>
		int Order { get; }

		/// <summary>
		/// Lists the rows of the section. Called whenever the section is built — each time the tab is rebuilt —
		/// so read static values here and hand live ones over as delegates.
		/// </summary>
		void Describe(IInfoSection section);
	}
}
