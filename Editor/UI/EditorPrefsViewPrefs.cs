#if OMNI_DEBUGGER
using System.Collections.Generic;
using UnityEditor;

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
	}
}
#endif