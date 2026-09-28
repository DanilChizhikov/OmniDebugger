using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal interface IViewPrefs
	{
		bool TryGetThemeId(out string id);
		void SetThemeId(string id);
		IReadOnlyList<string> GetFavorites();
		void SetFavorites(IReadOnlyList<string> keys);
	}
}