#if OMNI_DEBUGGER
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DTech.OmniDebugger.Editor
{
	internal sealed class ProjectOptionsBuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private const string ParentFolder = "Assets";
		private const string GeneratedFolderName = "OmniDebuggerGenerated";
		private const string GeneratedFolder = ParentFolder + "/" + GeneratedFolderName;
		private const string AssetPath = GeneratedFolder + "/" + ProjectOptionsAsset.AssetName + ".asset";

		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			DeleteGenerated();

			NamedBuildTarget target = OmniDebuggerDefines.ResolveTarget(report);

			if (!OmniDebuggerDefines.IsEnabled(target))
			{
				UnityLogSink.Default.Info(
					$"{OmniDebuggerDefines.Symbol} is off for this build, so the project settings stay out of it. " +
					$"Target: {target.TargetName}.");
				return;
			}

			AssetDatabase.CreateFolder(ParentFolder, GeneratedFolderName);

			ProjectOptionsAsset asset = ProjectOptionsAsset.Create(OmniDebuggerProjectSettings.instance.Options);
			AssetDatabase.CreateAsset(asset, AssetPath);
			AssetDatabase.SaveAssets();

			List<Object> preloaded = new List<Object>(PlayerSettings.GetPreloadedAssets());
			preloaded.Add(asset);
			PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
		}

		public void OnPostprocessBuild(BuildReport report) => DeleteGenerated();

		[InitializeOnLoadMethod]
		private static void DeleteLeftovers() => EditorApplication.delayCall += DeleteGenerated;

		private static void DeleteGenerated()
		{
			RemoveFromPreloaded();

			if (AssetDatabase.IsValidFolder(GeneratedFolder))
			{
				AssetDatabase.DeleteAsset(GeneratedFolder);
			}
		}

		private static void RemoveFromPreloaded()
		{
			Object[] preloaded = PlayerSettings.GetPreloadedAssets();
			List<Object> kept = new List<Object>(preloaded.Length);

			for (int i = 0; i < preloaded.Length; i++)
			{
				Object asset = preloaded[i];

				if (asset != null && !(asset is ProjectOptionsAsset))
				{
					kept.Add(asset);
				}
			}

			if (kept.Count != preloaded.Length)
			{
				PlayerSettings.SetPreloadedAssets(kept.ToArray());
			}
		}
	}
}
#endif
