using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// Where command icons come from. <see cref="DebugIconSource.Resources"/> keys load straight from
	/// a <c>Resources</c> folder and <see cref="DebugIconSource.Catalog"/> keys are looked up in
	/// <see cref="OmniDebuggerIconCatalog"/> assets; registered providers are asked first. Reached
	/// through <see cref="IOmniDebugger.Icons"/>.
	/// </summary>
	public interface IIconRegistry
	{
		/// <summary>Adds a provider, asked before the built-in lookups.</summary>
		void Register(IOmniDebuggerIconProvider provider);

		/// <returns><c>false</c> when it was not registered.</returns>
		bool Unregister(IOmniDebuggerIconProvider provider);

		/// <summary>Adds a catalog that lives outside <c>Resources/OmniDebugger</c>.</summary>
		void AddCatalog(OmniDebuggerIconCatalog catalog);

		/// <summary>Resolves an icon. A key that cannot be found is reported once.</summary>
		bool TryGet(in CommandIcon icon, out Background background);
	}
}