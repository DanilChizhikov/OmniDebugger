using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Loads the icons that are not built-in glyphs (see <see cref="OmniGlyphs"/>): the key a command
	/// declared with <see cref="DebugIconAttribute"/>, or a tab with <see cref="IOmniDebuggerTabFactory.Icon"/>.
	/// Set one as <see cref="IIconRegistry.Provider"/> to serve icons from an atlas, Addressables or anywhere
	/// else; the default one loads a sprite or texture from a <c>Resources</c> folder.
	/// </summary>
	public interface IOmniDebuggerIconProvider
	{
		/// <returns><c>false</c> when this provider does not know the key.</returns>
		bool TryGetIcon(string key, out Background background);
	}
}
