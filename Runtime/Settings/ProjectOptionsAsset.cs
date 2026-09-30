using UnityEngine;
using UnityEngine.Scripting;

namespace DTech.OmniDebugger
{
	[Preserve]
	internal sealed class ProjectOptionsAsset : ScriptableObject
	{
		public const string AssetName = "OmniDebuggerProjectOptions";

		public static ProjectOptionsAsset Loaded { get; private set; }

		public OmniDebuggerOptions Options => _options ??= new OmniDebuggerOptions();

		[SerializeField] private OmniDebuggerOptions _options = new ();

		public static ProjectOptionsAsset Create(OmniDebuggerOptions options)
		{
			ProjectOptionsAsset asset = CreateInstance<ProjectOptionsAsset>();
			asset.name = AssetName;
			asset._options = options == null ? new OmniDebuggerOptions() : options.Clone();
			return asset;
		}

		private void OnEnable() => Loaded = this;

		private void OnDisable()
		{
			if (ReferenceEquals(Loaded, this))
			{
				Loaded = null;
			}
		}
	}
}
