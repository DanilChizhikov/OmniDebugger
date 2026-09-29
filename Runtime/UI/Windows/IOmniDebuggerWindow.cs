namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A floating window registered with <see cref="IOmniDebuggerHost.Windows"/>. Windows float over the
	/// game while the panel is closed or floating, and hide while it covers the screen.
	/// </summary>
	public interface IOmniDebuggerWindow
	{
		string Id { get; }

		string Title { get; }

		bool IsOpen { get; }

		/// <summary>Only the header shows while collapsed.</summary>
		bool IsCollapsed { get; }

		void Open();

		void Close();

		void SetCollapsed(bool collapsed);
	}
}
