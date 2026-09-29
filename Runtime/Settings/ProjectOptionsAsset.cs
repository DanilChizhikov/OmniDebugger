using UnityEngine;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger
{
	[Preserve]
	internal sealed class ProjectOptionsAsset : ScriptableObject
	{
		public const string ResourcesPath = "OmniDebuggerProjectOptions";

		public OmniDebuggerOptions Options => _options ??= new OmniDebuggerOptions();

		[SerializeField] private OmniDebuggerOptions _options = new ();

		public static ProjectOptionsAsset Create(OmniDebuggerOptions options)
		{
			ProjectOptionsAsset asset = CreateInstance<ProjectOptionsAsset>();
			asset.name = ResourcesPath;
			asset._options = options == null ? new OmniDebuggerOptions() : options.Clone();
			return asset;
		}
	}
}
