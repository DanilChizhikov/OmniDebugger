using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// The themes the panel offers. A consumer needs no code at all — an
	/// <see cref="OmniDebuggerTheme"/> asset saved under any <c>Resources/OmniDebugger</c> folder
	/// shows up on its own — and may still register one built at runtime through
	/// <see cref="IOmniDebugger.Themes"/>.
	/// </summary>
	public interface IThemeRegistry
	{
		/// <summary>Raised after the set of themes changed, so open panels can rebuild their picker.</summary>
		event Action OnChanged;

		/// <summary>Every known theme, sorted by sort order and then by display name.</summary>
		IReadOnlyList<OmniDebuggerTheme> All { get; }

		/// <summary>
		/// The theme used when nothing was chosen or the chosen one no longer exists. Never null.
		/// </summary>
		OmniDebuggerTheme Default { get; }

		/// <summary>Looks up a theme by <see cref="OmniDebuggerTheme.Id"/>.</summary>
		bool TryGet(string id, out OmniDebuggerTheme theme);

		/// <summary>
		/// Adds a theme that no <c>Resources</c> folder can find — one created at runtime, or one
		/// loaded from Addressables or an asset bundle.
		/// </summary>
		/// <returns><c>false</c> when it was already registered.</returns>
		bool Register(OmniDebuggerTheme theme);

		/// <summary>Removes a theme added through <see cref="Register"/>.</summary>
		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(OmniDebuggerTheme theme);

		/// <summary>
		/// Re-reads the <c>Resources</c> folders. Needed only after a theme asset was created or
		/// deleted while a panel was open; registered themes are kept.
		/// </summary>
		void Refresh();
	}
}