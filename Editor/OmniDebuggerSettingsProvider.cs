using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace DTech.OmniDebugger.Editor
{
	internal static class OmniDebuggerSettingsProvider
	{
		private const string SettingsPath = "Project/DTech/OmniDebugger";

		[SettingsProvider]
		public static SettingsProvider Create()
		{
			return new SettingsProvider(SettingsPath, SettingsScope.Project)
			{
				label = "OmniDebugger",
				guiHandler = _ => Draw(),
				keywords = new HashSet<string>
				{
					"omni",
					"debugger",
					"debug",
					"cheat",
					"command",
					"define",
					OmniDebuggerDefines.Symbol,
				},
			};
		}

		private static void Draw()
		{
			EditorGUIUtility.labelWidth = 220f;
			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				$"The '{OmniDebuggerDefines.Symbol}' define gates the entire OmniDebugger runtime " +
				"assembly. With it off, no debugger code is compiled and nothing ships — which " +
				"also means your own calls into it belong inside " +
				$"#if {OmniDebuggerDefines.Symbol}.",
				MessageType.Info);

			EditorGUILayout.Space();
			DrawActiveTarget();

			EditorGUILayout.Space();
			DrawAllTargets();
		}

		private static void DrawActiveTarget()
		{
			NamedBuildTarget active = OmniDebuggerDefines.ActiveTarget;

			EditorGUILayout.LabelField("Active Build Target", EditorStyles.boldLabel);

			using (new EditorGUI.IndentLevelScope())
			{
				bool enabled = OmniDebuggerDefines.IsEnabled(active);

				EditorGUI.BeginChangeCheck();
				bool toggled = EditorGUILayout.Toggle(
					new GUIContent(
						$"Enabled for {Describe(active)}",
						$"Adds or removes {OmniDebuggerDefines.Symbol} for this build target."),
					enabled);

				if (EditorGUI.EndChangeCheck())
				{
					OmniDebuggerDefines.SetEnabled(active, toggled);
				}
			}
		}

		private static void DrawAllTargets()
		{
			EditorGUILayout.LabelField("All Build Targets", EditorStyles.boldLabel);

			using (new EditorGUI.IndentLevelScope())
			{
				IReadOnlyList<NamedBuildTarget> targets = OmniDebuggerDefines.GetKnownTargets();

				for (int i = 0; i < targets.Count; i++)
				{
					NamedBuildTarget target = targets[i];
					bool enabled = OmniDebuggerDefines.IsEnabled(target);

					EditorGUI.BeginChangeCheck();
					bool toggled = EditorGUILayout.Toggle(Describe(target), enabled);

					if (EditorGUI.EndChangeCheck())
					{
						OmniDebuggerDefines.SetEnabled(target, toggled);
					}
				}
			}
		}

		private static string Describe(NamedBuildTarget target) =>
			string.IsNullOrEmpty(target.TargetName) ? "Unknown" : target.TargetName;
	}
}