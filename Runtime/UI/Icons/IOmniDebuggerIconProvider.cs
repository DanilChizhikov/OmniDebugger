using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Resolves the icon a command declared with <see cref="DebugIconAttribute"/>. Register one with
	/// <see cref="IOmniDebuggerHost.Icons"/> to serve icons from an atlas, Addressables or anywhere else.
	/// </summary>
	public interface IOmniDebuggerIconProvider
	{
		/// <returns><c>false</c> when this provider does not know the icon.</returns>
		bool TryGetIcon(in CommandIcon icon, out Background background);
	}
}