namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Contributes a tab to the panel. Register one with <see cref="IOmniDebugger.Tabs"/> and it shows
	/// up in both mounts — the runtime panel and the editor window.
	/// </summary>
	public interface IOmniDebuggerTabFactory
	{
		/// <summary>
		/// Stable id used to remember which tab was open. Must not be null or whitespace, and must be
		/// unique across factories.
		/// </summary>
		string Id { get; }

		/// <summary>Label shown on the tab.</summary>
		string DisplayName { get; }

		/// <summary>Position in the tab bar. Lower values come first.</summary>
		int Order { get; }

		/// <summary>Builds the tab. Called lazily, the first time the tab is selected.</summary>
		IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context);
	}
}
