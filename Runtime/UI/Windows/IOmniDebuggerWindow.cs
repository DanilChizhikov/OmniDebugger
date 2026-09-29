namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A floating window registered with <see cref="IOmniDebuggerHost.Windows"/>. Windows float over the
	/// game while the panel is closed or floating, and hide while it covers the screen.
	/// </summary>
	public interface IOmniDebuggerWindow
	{
		/// <summary>
		/// The id it was registered under. Compared ordinally; registering the same id again replaces the
		/// window.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Shown in the window's header and in the Windows tab. Falls back to <see cref="Id"/> when left blank.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Whether the window is shown. A window hidden behind an edge-to-edge panel still counts as open.
		/// </summary>
		bool IsOpen { get; }

		/// <summary>Only the header shows while collapsed.</summary>
		bool IsCollapsed { get; }

		/// <summary>
		/// Shows the window. Its content is built again each time it is shown. Does nothing once the window was
		/// unregistered or replaced.
		/// </summary>
		void Open();

		/// <summary>Hides the window, keeping its place for the session.</summary>
		void Close();

		/// <summary>Folds the window down to its header, or unfolds it.</summary>
		void SetCollapsed(bool collapsed);
	}
}
