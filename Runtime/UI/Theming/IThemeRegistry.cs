using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// The themes the panel offers. Nothing configured, they are the built-in dark and light
	/// themes. <see cref="OmniDebuggerOptions.DefaultTheme"/> alone replaces both, so the panel keeps
	/// that one theme and hides its switcher. Themes listed in <see cref="OmniDebuggerOptions.Themes"/>
	/// or added through <see cref="Register"/> are offered next to the built-in ones.
	/// </summary>
	public interface IThemeRegistry
	{
		/// <summary>
		/// Raised after the set of themes or <see cref="Default"/> changed, so open panels can rebuild
		/// their picker.
		/// </summary>
		event Action OnChanged;

		/// <summary>Every known theme, sorted by sort order and then by display name.</summary>
		IReadOnlyList<OmniDebuggerTheme> All { get; }

		/// <summary>
		/// The theme used when nothing was chosen or the chosen one no longer exists:
		/// <see cref="OmniDebuggerOptions.DefaultTheme"/> when the debugger was given one, the built-in
		/// dark theme otherwise. Never null.
		/// </summary>
		OmniDebuggerTheme Default { get; }

		/// <summary>Looks up a theme by <see cref="OmniDebuggerTheme.Id"/>.</summary>
		bool TryGet(string id, out OmniDebuggerTheme theme);

		/// <summary>
		/// Adds a theme next to the built-in ones — one created at runtime, or one loaded from
		/// Addressables or an asset bundle.
		/// </summary>
		/// <returns><c>false</c> when it was already registered.</returns>
		bool Register(OmniDebuggerTheme theme);

		/// <summary>Removes a theme added through <see cref="Register"/>.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(OmniDebuggerTheme theme);
	}
}