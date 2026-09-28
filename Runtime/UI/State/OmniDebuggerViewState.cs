using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// What a panel should still know after its elements were thrown away and rebuilt. Created by
	/// whoever mounts the panel — a component or an editor window — and handed to every
	/// <see cref="OmniDebuggerView"/> built for that mount.
	/// </summary>
	public sealed class OmniDebuggerViewState
	{
		private readonly Dictionary<string, OmniDebuggerTabState> _tabs = new (StringComparer.Ordinal);

		/// <summary>Id of the tab that was open, or null when the panel never opened one.</summary>
		public string SelectedTabId { get; set; }

		/// <summary>
		/// Id of the selected theme, or null to fall back to the last saved choice and then the default.
		/// Ignored while the mount fixes its theme.
		/// </summary>
		public string ThemeId { get; set; }

		/// <summary>Whether the landscape layout, with the tabs in a sidebar, was in use.</summary>
		public bool Landscape { get; set; }

		/// <summary>Whether the panel was open. Restored on a rebuild, never persisted to disk.</summary>
		public bool IsOpen { get; set; }

		internal ArgumentMemory Arguments { get; } = new ();

		internal CommandKeySet Favorites { get; } = new ();

		internal CommandKeySet Pins { get; } = new ();

		/// <summary>The state of one tab, created the first time that tab asks for it.</summary>
		public OmniDebuggerTabState GetTabState(string tabId)
		{
			if (string.IsNullOrWhiteSpace(tabId))
			{
				throw new ArgumentException("Tab id must not be null or whitespace.", nameof(tabId));
			}

			if (_tabs.TryGetValue(tabId, out OmniDebuggerTabState state))
			{
				return state;
			}

			state = new OmniDebuggerTabState();
			_tabs.Add(tabId, state);
			return state;
		}

		/// <summary>Forgets everything, including every tab's state.</summary>
		public void Clear()
		{
			SelectedTabId = null;
			ThemeId = null;
			Landscape = false;
			IsOpen = false;
			Arguments.Clear();
			Favorites.Clear();
			Pins.Clear();
			_tabs.Clear();
		}
	}
}