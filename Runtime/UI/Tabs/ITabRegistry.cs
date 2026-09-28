using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Which tabs the panel offers. Info, Commands, Search, Logs and Windows are built in; register
	/// your own through <see cref="IOmniDebugger.Tabs"/> right after constructing the debugger, and
	/// every panel showing that debugger picks it up.
	/// </summary>
	public interface ITabRegistry
	{
		/// <summary>Raised after a factory was added or removed, so open panels rebuild their tab bar.</summary>
		event Action OnChanged;

		/// <summary>Every registered factory, ordered by <see cref="IOmniDebuggerTabFactory.Order"/>.</summary>
		IReadOnlyList<IOmniDebuggerTabFactory> All { get; }

		/// <summary>Adds a factory.</summary>
		/// <returns><c>false</c> when it was already registered or its id is taken.</returns>
		bool Register(IOmniDebuggerTabFactory factory);

		/// <summary>Removes a factory. Open panels drop the tab it built.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(IOmniDebuggerTabFactory factory);
	}
}
