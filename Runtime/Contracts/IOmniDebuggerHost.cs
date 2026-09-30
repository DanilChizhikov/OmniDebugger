using System;
using DTech.OmniDebugger.UI;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger: its commands, captured logs, and everything the panel offers — info sections, tabs,
	/// themes, icons and argument fields. Constructed and owned by the game — see
	/// <see cref="OmniDebuggerHost"/>. Every registry lives and dies with the debugger.
	/// </summary>
	public interface IOmniDebuggerHost : IDisposable
	{
		/// <summary>
		/// Raised by <see cref="Refresh"/>. Every view of this debugger listens to it; a tab or info section of
		/// your own can too, to redraw what it built.
		/// </summary>
		event Action OnRefreshRequested;

		/// <summary>Which commands exist, and running and reading them.</summary>
		ICommandRegistry Commands { get; }

		/// <summary>Commands pinned to the hotbar over the game.</summary>
		IHotbar Hotbar { get; }

		/// <summary>Display order of command groups.</summary>
		IGroupOrder Groups { get; }

		/// <summary>Unity's console, captured since the debugger was built.</summary>
		ILogFeed Logs { get; }

		/// <summary>Sections of the Info tab, and which of them float over the game.</summary>
		IInfoRegistry Info { get; }

		/// <summary>Tabs the panel offers.</summary>
		ITabRegistry Tabs { get; }

		/// <summary>Themes the panel can be switched to.</summary>
		IThemeRegistry Themes { get; }

		/// <summary>Where command icons come from.</summary>
		IIconRegistry Icons { get; }

		/// <summary>Which control edits which argument type.</summary>
		IArgumentFieldRegistry Fields { get; }

		/// <summary>
		/// Makes every view of this debugger — the runtime panel, the editor window and the floating sections —
		/// re-read and redraw what it shows. Values on screen already follow the game on their own a few times
		/// a second, so a property changed from code shows up without this; call it when that is not soon
		/// enough, or after a change only a redraw picks up, such as an icon registered late. Calls made within
		/// one frame are merged into one redraw. Main thread only.
		/// </summary>
		void Refresh();
	}
}
