using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal interface IViewPrefs
	{
		bool TryGetThemeId(out string id);
		void SetThemeId(string id);
		IReadOnlyList<string> GetFavorites();
		void SetFavorites(IReadOnlyList<string> keys);
		bool TryGetWindowScale(out float scale);
		void SetWindowScale(float scale);
		bool TryGetOpenButton(out OpenButtonAnchor anchor, out Vector2 position);
		void SetOpenButton(OpenButtonAnchor anchor, Vector2 position);
	}
}