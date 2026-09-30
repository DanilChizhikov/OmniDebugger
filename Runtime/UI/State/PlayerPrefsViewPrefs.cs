using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal sealed class PlayerPrefsViewPrefs : IViewPrefs
	{
		public static readonly PlayerPrefsViewPrefs Default = new PlayerPrefsViewPrefs();

		public bool TryGetThemeId(out string id)
		{
			if (!PlayerPrefs.HasKey(ViewStateKeys.Theme))
			{
				id = null;
				return false;
			}

			id = PlayerPrefs.GetString(ViewStateKeys.Theme);
			return true;
		}

		public void SetThemeId(string id)
		{
			PlayerPrefs.SetString(ViewStateKeys.Theme, id ?? string.Empty);
			PlayerPrefs.Save();
		}

		public IReadOnlyList<string> GetHotbar() =>
			KeyListFormat.Split(PlayerPrefs.GetString(ViewStateKeys.Hotbar, string.Empty));

		public void SetHotbar(IReadOnlyList<string> paths)
		{
			PlayerPrefs.SetString(ViewStateKeys.Hotbar, KeyListFormat.Join(paths));
			PlayerPrefs.Save();
		}

		public bool GetHotbarCollapsed() => PlayerPrefs.GetInt(ViewStateKeys.HotbarCollapsed, 0) != 0;

		public void SetHotbarCollapsed(bool collapsed)
		{
			PlayerPrefs.SetInt(ViewStateKeys.HotbarCollapsed, collapsed ? 1 : 0);
			PlayerPrefs.Save();
		}

		public string GetArguments() => PlayerPrefs.GetString(ViewStateKeys.Arguments, string.Empty);

		public void SetArguments(string serialized)
		{
			PlayerPrefs.SetString(ViewStateKeys.Arguments, serialized ?? string.Empty);
			PlayerPrefs.Save();
		}

		public IReadOnlyList<string> GetFloating() =>
			KeyListFormat.Split(PlayerPrefs.GetString(ViewStateKeys.Floating, string.Empty));

		public void SetFloating(IReadOnlyList<string> keys)
		{
			PlayerPrefs.SetString(ViewStateKeys.Floating, KeyListFormat.Join(keys));
			PlayerPrefs.Save();
		}

		public IReadOnlyDictionary<string, float> GetSectionScales() =>
			ScaleListFormat.Parse(PlayerPrefs.GetString(ViewStateKeys.SectionScales, string.Empty));

		public void SetSectionScales(IReadOnlyDictionary<string, float> scales)
		{
			PlayerPrefs.SetString(ViewStateKeys.SectionScales, ScaleListFormat.Format(scales));
			PlayerPrefs.Save();
		}

		public bool TryGetOpenButton(out OpenButtonAnchor anchor, out ScreenEdge edge, out float along)
		{
			if (!PlayerPrefs.HasKey(ViewStateKeys.OpenButtonAnchor) ||
				!PlayerPrefs.HasKey(ViewStateKeys.OpenButtonEdge) ||
				!PlayerPrefs.HasKey(ViewStateKeys.OpenButtonAlong))
			{
				anchor = default;
				edge = default;
				along = default;
				return false;
			}

			anchor = (OpenButtonAnchor)PlayerPrefs.GetInt(ViewStateKeys.OpenButtonAnchor);
			edge = (ScreenEdge)PlayerPrefs.GetInt(ViewStateKeys.OpenButtonEdge);
			along = PlayerPrefs.GetFloat(ViewStateKeys.OpenButtonAlong);
			return true;
		}

		public void SetOpenButton(OpenButtonAnchor anchor, ScreenEdge edge, float along)
		{
			PlayerPrefs.SetInt(ViewStateKeys.OpenButtonAnchor, (int)anchor);
			PlayerPrefs.SetInt(ViewStateKeys.OpenButtonEdge, (int)edge);
			PlayerPrefs.SetFloat(ViewStateKeys.OpenButtonAlong, along);
			PlayerPrefs.Save();
		}
	}
}
