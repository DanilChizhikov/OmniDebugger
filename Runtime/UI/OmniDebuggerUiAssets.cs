using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal static class OmniDebuggerUiAssets
	{
		public const string ResourcesFolder = "OmniDebugger";

		private const string PanelStyleSheetPath = ResourcesFolder + "/OmniDebuggerPanel";
		private const string DarkTokensPath = ResourcesFolder + "/OmniDebuggerTokensDark";
		private const string LightTokensPath = ResourcesFolder + "/OmniDebuggerTokensLight";
		private const string PanelSettingsPath = ResourcesFolder + "/OmniDebuggerPanelSettings";
		private const string RuntimeThemePath = ResourcesFolder + "/OmniDebuggerRuntimeTheme";

		public static StyleSheet PanelStyleSheet => Load(ref _panelStyleSheet, PanelStyleSheetPath);

		public static StyleSheet DarkTokens => Load(ref _darkTokens, DarkTokensPath);

		public static StyleSheet LightTokens => Load(ref _lightTokens, LightTokensPath);

		public static PanelSettings PanelSettings => Load(ref _panelSettings, PanelSettingsPath);

		public static ThemeStyleSheet RuntimeTheme => Load(ref _runtimeTheme, RuntimeThemePath);

		private static StyleSheet _panelStyleSheet;
		private static StyleSheet _darkTokens;
		private static StyleSheet _lightTokens;
		private static PanelSettings _panelSettings;
		private static ThemeStyleSheet _runtimeTheme;

		private static T Load<T>(ref T cached, string path) where T : Object
		{
			if (cached != null)
			{
				return cached;
			}

			cached = Resources.Load<T>(path);

			if (cached == null)
			{
				UnityLogSink.Default.Warning(
					$"UI asset is missing. Path: Resources/{path}; Type: {typeof(T).Name}. " +
					"The panel will still open, but it will be unstyled.");
			}

			return cached;
		}
	}
}