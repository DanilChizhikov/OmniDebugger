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

		public IReadOnlyList<string> GetFavorites() =>
			KeyListFormat.Split(PlayerPrefs.GetString(ViewStateKeys.Favorites, string.Empty));

		public void SetFavorites(IReadOnlyList<string> keys)
		{
			PlayerPrefs.SetString(ViewStateKeys.Favorites, KeyListFormat.Join(keys));
			PlayerPrefs.Save();
		}

		public bool TryGetWindowScale(out float scale)
		{
			if (!PlayerPrefs.HasKey(ViewStateKeys.WindowScale))
			{
				scale = 1.0f;
				return false;
			}

			scale = PlayerPrefs.GetFloat(ViewStateKeys.WindowScale);
			return true;
		}

		public void SetWindowScale(float scale)
		{
			PlayerPrefs.SetFloat(ViewStateKeys.WindowScale, scale);
			PlayerPrefs.Save();
		}
	}
}