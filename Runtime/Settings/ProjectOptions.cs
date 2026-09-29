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

			if (source == null)
			{
				ProjectOptionsAsset asset = Resources.Load<ProjectOptionsAsset>(ProjectOptionsAsset.ResourcesPath);
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
					$"Expected: Resources/{ProjectOptionsAsset.ResourcesPath}.");
			}

			return new OmniDebuggerOptions();
		}

		public static void NotifyEditorChanged() => OnEditorChanged?.Invoke();
	}
}
