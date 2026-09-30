using System;
using UnityEngine;

namespace DTech.OmniDebugger
{
	internal static class ProjectOptions
	{
		public static event Action OnEditorChanged;

		public static Func<OmniDebuggerOptions> EditorSource { get; set; }

		public static OmniDebuggerOptions Load()
		{
			OmniDebuggerOptions source = EditorSource?.Invoke();

			if (source == null && !Application.isEditor)
			{
				ProjectOptionsAsset asset = FindPreloaded();
				source = asset != null ? asset.Options : null;
			}

			if (source != null)
			{
				return source.Clone();
			}

			if (!Application.isEditor)
			{
				UnityLogSink.Default.Warning(
					"Project settings were not found in the build, so the built-in defaults are used. " +
					$"Expected among the preloaded assets: {ProjectOptionsAsset.AssetName}.");
			}

			return new OmniDebuggerOptions();
		}

		public static void NotifyEditorChanged() => OnEditorChanged?.Invoke();

		private static ProjectOptionsAsset FindPreloaded()
		{
			if (ProjectOptionsAsset.Loaded != null)
			{
				return ProjectOptionsAsset.Loaded;
			}

			ProjectOptionsAsset[] loaded = Resources.FindObjectsOfTypeAll<ProjectOptionsAsset>();
			return loaded.Length > 0 ? loaded[0] : null;
		}
	}
}
