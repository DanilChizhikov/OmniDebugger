using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Where icons come from. A key is first read as the name of a built-in glyph (see
	/// <see cref="OmniGlyphs"/>); anything else is asked of the one <see cref="Provider"/>. Reached through
	/// <see cref="IOmniDebuggerHost.Icons"/>.
	/// </summary>
	public interface IIconRegistry
	{
		/// <summary>Raised when <see cref="Provider"/> is replaced, so views can redraw their icons.</summary>
		event Action OnChanged;

		/// <summary>
		/// Loads every icon key that is not a glyph name. Defaults to <see cref="ResourcesIconProvider"/>;
		/// setting null restores it. Results are cached until the provider is replaced.
		/// </summary>
		IOmniDebuggerIconProvider Provider { get; set; }

		/// <summary>
		/// Loads an image through <see cref="Provider"/>. A key that cannot be found is reported once.
		/// </summary>
		/// <returns><c>false</c> for a glyph name, a blank key, or a key the provider does not know.</returns>
		bool TryGetImage(string key, out Background background);
	}
}
