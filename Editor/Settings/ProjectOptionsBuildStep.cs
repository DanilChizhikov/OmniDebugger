#if OMNI_DEBUGGER
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DTech.OmniDebugger.Editor
{
	internal sealed class ProjectOptionsBuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		private const string ParentFolder = "Assets";
		private const string GeneratedFolderName = "OmniDebuggerGenerated";
		private const string GeneratedFolder = ParentFolder + "/" + GeneratedFolderName;
		private const string ResourcesFolderName = "Resources";
		private const string ResourcesFolder = GeneratedFolder + "/" + ResourcesFolderName;
		private const string AssetPath = ResourcesFolder + "/" + ProjectOptionsAsset.ResourcesPath + ".asset";

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
			AssetDatabase.CreateFolder(GeneratedFolder, ResourcesFolderName);
			AssetDatabase.CreateAsset(ProjectOptionsAsset.Create(OmniDebuggerProjectSettings.instance.Options), AssetPath);
			AssetDatabase.SaveAssets();
		}

		public void OnPostprocessBuild(BuildReport report) => DeleteGenerated();

		[InitializeOnLoadMethod]
		private static void DeleteLeftovers() => EditorApplication.delayCall += DeleteGenerated;

		private static void DeleteGenerated()
		{
			if (AssetDatabase.IsValidFolder(GeneratedFolder))
			{
				AssetDatabase.DeleteAsset(GeneratedFolder);
			}
		}
	}
}
#endif