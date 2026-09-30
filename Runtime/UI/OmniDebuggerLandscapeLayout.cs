namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// How the runtime panel sits on a landscape screen. Portrait always runs edge to edge, and the
	/// editor window always fills the window.
	/// </summary>
	public enum OmniDebuggerLandscapeLayout : byte
	{
		/// <summary>
		/// A floating window over the game, which stays visible and takes taps around it. It starts
		/// centred, moves when dragged by its top bar and is sized by
		/// <see cref="OmniDebuggerPanelOptions.FloatingScale"/>. Floating Info sections stay on screen next to it.
		/// </summary>
		Floating = 0,

		/// <summary>
		/// Edge to edge: no margin, corner radius or shadow. The glass reaches under the notch and the
		/// content keeps out of it.
		/// </summary>
		FullScreen = 1,
	}
}
