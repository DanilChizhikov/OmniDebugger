#if OMNI_DEBUGGER
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace DTech.OmniDebugger.Editor
{
	internal sealed class CommandLinkerStep : IUnityLinkerProcessor
	{
		private const string OutputFolder = "Temp/OmniDebugger";
		private const string OutputFileName = "CommandsLink.xml";

		public int callbackOrder => 0;

		public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
		{
			NamedBuildTarget target = OmniDebuggerDefines.ResolveTarget(report);

			if (!OmniDebuggerDefines.IsEnabled(target))
			{
				return null;
			}

			CommandPreservation preservation = CommandPreservation.Collect(CommandAssemblies.CollectTypes());

			if (preservation.IsEmpty)
			{
				UnityLogSink.Default.Info("No command needs keeping from code stripping, so no link.xml is added.");
				return null;
			}

			Directory.CreateDirectory(OutputFolder);
			string path = Path.GetFullPath(Path.Combine(OutputFolder, OutputFileName));
			File.WriteAllText(path, preservation.ToXml());

			UnityLogSink.Default.Info(
				$"Commands keep {preservation.TypeCount} type(s) in {preservation.AssemblyCount} assembly(ies) " +
				$"from code stripping. Target: {target.TargetName}. File: {path}");

			return path;
		}
	}
}
#endif
