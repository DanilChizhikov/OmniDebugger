namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// A floating window registered with <see cref="IOmniDebugger.Windows"/>. Windows float over the
	/// game while the panel is closed and hide while it is open.
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
