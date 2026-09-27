namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger core, free of any UI. Constructed and owned by the game — see
	/// <see cref="OmniDebugger"/>.
	/// </summary>
	public interface IOmniDebugger
	{
		/// <summary>Which commands exist.</summary>
		ICommandCatalog Catalog { get; }

		/// <summary>Running and reading them.</summary>
		ICommandInvoker Commands { get; }

		/// <summary>Display order of command groups.</summary>
		IGroupOrder Groups { get; }
	}
}