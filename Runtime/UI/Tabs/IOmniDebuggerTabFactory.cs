namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Contributes a tab to the runtime panel. Register one with <see cref="IOmniDebuggerHost.Tabs"/>.
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

		/// <summary>
		/// Icon shown with <see cref="DisplayName"/>: a built-in glyph name (see <see cref="OmniGlyphs"/>) or a
		/// key <see cref="IIconRegistry.Provider"/> loads. An image is tinted with the tab's text colour, so ship
		/// it white on transparent. Null for a generic glyph.
		/// </summary>
		string Icon { get; }

		/// <summary>Position in the tab bar. Lower values come first.</summary>
		int Order { get; }

		/// <summary>Builds the tab. Called lazily, the first time the tab is selected.</summary>
		IOmniDebuggerTab CreateTab(in OmniDebuggerTabContext context);
	}
}
