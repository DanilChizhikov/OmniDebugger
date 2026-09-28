using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Floating windows: small panes that stay over the game while the panel is closed — a live
	/// stat, or a handful of commands to hit while playing. Reached through
	/// <see cref="IOmniDebugger.Windows"/>; the runtime panel shows them and its Windows tab lists them.
	/// </summary>
	public interface IWindowRegistry
	{
		/// <summary>Raised after a window was registered, unregistered, opened, closed or collapsed.</summary>
		event Action OnChanged;

		/// <summary>Every registered window, in registration order.</summary>
		IReadOnlyList<IOmniDebuggerWindow> All { get; }

		/// <summary>
		/// A window whose content you build. <paramref name="buildContent"/> runs each time the
		/// window is shown, into an empty element. A window with the same id is replaced.
		/// </summary>
		/// <param name="size">Width and maximum height; zero on an axis keeps the default.</param>
		IOmniDebuggerWindow RegisterCustom(string id, string title, Action<VisualElement> buildContent, bool open = false, Vector2 size = default);

		/// <summary>
		/// A window listing commands by key (<c>"Group/Name"</c>), ready to run. A window with the
		/// same id is replaced.
		/// </summary>
		/// <param name="size">Width and maximum height; zero on an axis keeps the default.</param>
		IOmniDebuggerWindow RegisterCommands(string id, string title, IReadOnlyList<string> commandKeys, bool open = false, Vector2 size = default);

		/// <returns><c>false</c> when no window has this id.</returns>
		bool Unregister(string id);

		/// <returns><c>false</c> when no window has this id.</returns>
		bool TryGet(string id, out IOmniDebuggerWindow window);

		/// <returns><c>false</c> when no window has this id.</returns>
		bool Open(string id);

		/// <returns><c>false</c> when no window has this id.</returns>
		bool Close(string id);

		/// <summary>Closes every window.</summary>
		void CloseAll();
	}
}