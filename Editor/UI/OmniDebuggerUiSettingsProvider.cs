#if OMNI_DEBUGGER
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.UI.Editor
{
	internal static class OmniDebuggerUiSettingsProvider
	{
		private const string SettingsPath = "Project/DTech/OmniDebugger/UI";

		private static ThemeCatalog _themes;

		private static ThemeCatalog Themes => _themes ??= new ThemeCatalog(UnityLogSink.Default);

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider(SettingsPath, SettingsScope.Project)
			{
				label = "UI",
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
				"the other. Themes are OmniDebuggerTheme assets in any Resources/OmniDebugger folder.",
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

			EditorPrefsViewPrefs.Default.TryGetThemeId(out string storedId);

			for (int i = 0; i < themes.Count; i++)
			{
				labels[i] = themes[i].DisplayName;

				if (string.Equals(themes[i].Id, storedId, StringComparison.Ordinal))
				{
					selected = i;
				}
			}

			using (new EditorGUI.IndentLevelScope())
			{
				if (themes.Count == 0)
				{
					EditorGUILayout.LabelField("No themes were found.");
					return;
				}

				int picked = EditorGUILayout.Popup("Theme", selected, labels);

				if (picked != selected)
				{
					EditorPrefsViewPrefs.Default.SetThemeId(themes[picked].Id);
				}

				if (GUILayout.Button("Rescan Themes", GUILayout.Width(140f)))
				{
					Themes.Refresh();
				}
			}
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