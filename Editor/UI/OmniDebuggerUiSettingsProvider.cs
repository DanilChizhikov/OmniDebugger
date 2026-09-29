#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using DTech.OmniDebugger.Editor;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.UI.Editor
{
	internal static class OmniDebuggerUiSettingsProvider
	{
		private const string SettingsPath = "Project/DTech/OmniDebugger/UI";

		private static ThemeCatalog _themes;

		private static ThemeCatalog Themes => _themes ??= CreateThemes();

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider(SettingsPath, SettingsScope.Project)
			{
				label = "UI",
				activateHandler = (_, _) => ReleaseThemes(),
				guiHandler = _ => Draw(),
				keywords = new HashSet<string>
				{
					"omni",
					"debugger",
					"panel",
					"theme",
					"ui",
					"uss",
				},
			};
		}

		private static void Draw()
		{
			EditorGUIUtility.labelWidth = 220f;
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				"The theme and favourites are the only choices the panel saves. The editor window keeps them " +
				"in EditorPrefs and the running game keeps its own in PlayerPrefs, so changing one never moves " +
				"the other. Themes are OmniDebuggerTheme assets set on the Panel page.",
				MessageType.Info);

			EditorGUILayout.Space();
			DrawThemePicker();

			EditorGUILayout.Space();
			DrawDiscovered();
		}

		private static void DrawThemePicker()
		{
			EditorGUILayout.LabelField("Editor Window Theme", EditorStyles.boldLabel);

			IReadOnlyList<OmniDebuggerTheme> themes = Themes.All;
			string[] labels = new string[themes.Count];
			int selected = 0;
			string selectedId = themes.Count == 0 ? null : ResolveSelectedId();

			for (int i = 0; i < themes.Count; i++)
			{
				labels[i] = themes[i].DisplayName;

				if (string.Equals(themes[i].Id, selectedId, StringComparison.Ordinal))
				{
					selected = i;
				}
			}

			using (new EditorGUI.IndentLevelScope())
			{
				if (themes.Count == 0)
				{
					EditorGUILayout.LabelField("Theme", "No themes were found.");
					return;
				}

				int picked = EditorGUILayout.Popup("Theme", selected, labels);

				if (picked != selected)
				{
					EditorPrefsViewPrefs.Default.SetThemeId(themes[picked].Id);
				}
			}
		}

		private static ThemeCatalog CreateThemes()
		{
			ThemeCatalog themes = new ThemeCatalog(UnityLogSink.Default);
			OmniDebuggerOptions options = OmniDebuggerProjectSettings.instance.Options;

			for (int i = 0; i < options.Themes.Count; i++)
			{
				OmniDebuggerTheme theme = options.Themes[i];

				if (theme != null)
				{
					themes.Register(theme);
				}
			}

			themes.SetDefault(options.DefaultTheme);
			return themes;
		}

		private static void ReleaseThemes()
		{
			_themes?.Clear();
			_themes = null;
		}

		private static string ResolveSelectedId()
		{
			if (EditorPrefsViewPrefs.Default.TryGetThemeId(out string storedId) && Themes.TryGet(storedId, out _))
			{
				return storedId;
			}

			return Themes.Default.Id;
		}

		private static void DrawDiscovered()
		{
			EditorGUILayout.LabelField("Available Themes", EditorStyles.boldLabel);

			IReadOnlyList<OmniDebuggerTheme> themes = Themes.All;

			using (new EditorGUI.IndentLevelScope())
			{
				for (int i = 0; i < themes.Count; i++)
				{
					OmniDebuggerTheme theme = themes[i];
					EditorGUILayout.LabelField(theme.DisplayName, $"id: {theme.Id}, sheets: {theme.StyleSheets.Count}");
				}
			}
		}
	}
}
#endif