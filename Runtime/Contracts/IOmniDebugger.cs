using System;
using DTech.OmniDebugger.UI;

namespace DTech.OmniDebugger
{
	/// <summary>
	/// The debugger: its commands, captured logs, and everything the panel offers — windows, tabs,
	/// themes, icons and argument fields. Constructed and owned by the game — see
	/// <see cref="OmniDebugger"/>. Every registry lives and dies with the debugger.
	/// </summary>
	public interface IOmniDebugger : IDisposable
	{
		/// <summary>Which commands exist.</summary>
		ICommandCatalog Catalog { get; }

		/// <summary>Running and reading them.</summary>
		ICommandInvoker Commands { get; }

		/// <summary>Display order of command groups.</summary>
		IGroupOrder Groups { get; }

		/// <summary>Unity's console, captured since the debugger was built.</summary>
		ILogFeed Logs { get; }

		/// <summary>Floating windows shown over the game while the panel is closed.</summary>
		IWindowRegistry Windows { get; }

		/// <summary>Tabs the panel offers.</summary>
		ITabRegistry Tabs { get; }

		/// <summary>Themes the panel can be switched to.</summary>
		IThemeRegistry Themes { get; }

		/// <summary>Where command icons come from.</summary>
		IIconRegistry Icons { get; }

		/// <summary>Which control edits which argument type.</summary>
		IArgumentFieldRegistry Fields { get; }
	}
}
