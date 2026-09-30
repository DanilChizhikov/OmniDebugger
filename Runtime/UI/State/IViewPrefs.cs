using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal interface IViewPrefs
	{
		bool TryGetThemeId(out string id);

		void SetThemeId(string id);

		IReadOnlyList<string> GetHotbar();

		void SetHotbar(IReadOnlyList<string> paths);

		bool GetHotbarCollapsed();

		void SetHotbarCollapsed(bool collapsed);

		string GetArguments();

		void SetArguments(string serialized);

		IReadOnlyList<string> GetFloating();

		void SetFloating(IReadOnlyList<string> keys);

		IReadOnlyDictionary<string, float> GetSectionScales();

		void SetSectionScales(IReadOnlyDictionary<string, float> scales);

		bool TryGetOpenButton(out OpenButtonAnchor anchor, out ScreenEdge edge, out float along);

		void SetOpenButton(OpenButtonAnchor anchor, ScreenEdge edge, float along);
	}
}
