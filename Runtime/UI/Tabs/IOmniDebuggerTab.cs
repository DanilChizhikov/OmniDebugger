using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// One page of the panel. Built by an <see cref="IOmniDebuggerTabFactory"/> and thrown away
	/// whenever the panel is rebuilt — anything worth keeping belongs in
	/// <see cref="OmniDebuggerTabContext.State"/>.
	/// </summary>
	public interface IOmniDebuggerTab : IDisposable
	{
		/// <summary>The tab's content. Read once, right after the tab is built.</summary>
		VisualElement Root { get; }

		/// <summary>
		/// Called when this tab becomes the visible one, and again whenever the panel opens while it
		/// is selected. Start timers here.
		/// </summary>
		void OnOpen();

		/// <summary>
		/// Called when another tab is selected or the panel closes. Stop timers here — a scheduled
		/// item keeps firing on a hidden element and drains a device battery.
		/// </summary>
		void OnClose();

		/// <summary>
		/// Called when the catalog changed or the user asked for a refresh. The tab is visible and
		/// should re-read whatever it shows.
		/// </summary>
		void Refresh();
	}
}
