#if OMNI_DEBUGGER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.UI.Editor
{
	internal sealed class EditorPrefsViewPrefs : IViewPrefs
	{
		public static readonly EditorPrefsViewPrefs Default = new EditorPrefsViewPrefs();

		public bool TryGetThemeId(out string id)
		{
			if (!EditorPrefs.HasKey(ViewStateKeys.Theme))
			{
				id = null;
				return false;
			}

			id = EditorPrefs.GetString(ViewStateKeys.Theme);
			return true;
		}

		public void SetThemeId(string id) => EditorPrefs.SetString(ViewStateKeys.Theme, id ?? string.Empty);

		public IReadOnlyList<string> GetFavorites() =>
			KeyListFormat.Split(EditorPrefs.GetString(ViewStateKeys.Favorites, string.Empty));

		public void SetFavorites(IReadOnlyList<string> keys) =>
			EditorPrefs.SetString(ViewStateKeys.Favorites, KeyListFormat.Join(keys));

		public bool TryGetWindowScale(out float scale)
		{
			if (!EditorPrefs.HasKey(ViewStateKeys.WindowScale))
			{
				scale = 1.0f;
				return false;
			}

			scale = EditorPrefs.GetFloat(ViewStateKeys.WindowScale);
			return true;
		}

		public void SetWindowScale(float scale) => EditorPrefs.SetFloat(ViewStateKeys.WindowScale, scale);

		public bool TryGetOpenButton(out OpenButtonAnchor anchor, out Vector2 position)
		{
			if (!EditorPrefs.HasKey(ViewStateKeys.OpenButtonAnchor) ||
				!EditorPrefs.HasKey(ViewStateKeys.OpenButtonX) ||
				!EditorPrefs.HasKey(ViewStateKeys.OpenButtonY))
			{
				anchor = default;
				position = default;
				return false;
			}

			anchor = (OpenButtonAnchor)EditorPrefs.GetInt(ViewStateKeys.OpenButtonAnchor);
			position = new Vector2(EditorPrefs.GetFloat(ViewStateKeys.OpenButtonX), EditorPrefs.GetFloat(ViewStateKeys.OpenButtonY));
			return true;
		}

		public void SetOpenButton(OpenButtonAnchor anchor, Vector2 position)
		{
			EditorPrefs.SetInt(ViewStateKeys.OpenButtonAnchor, (int)anchor);
			EditorPrefs.SetFloat(ViewStateKeys.OpenButtonX, position.x);
			EditorPrefs.SetFloat(ViewStateKeys.OpenButtonY, position.y);
		}
	}
}
#endif