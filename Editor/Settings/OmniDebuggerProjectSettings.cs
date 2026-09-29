#if OMNI_DEBUGGER
using UnityEditor;
using UnityEngine;

namespace DTech.OmniDebugger.Editor
{
	[FilePath(AssetPath, FilePathAttribute.Location.ProjectFolder)]
	internal sealed class OmniDebuggerProjectSettings : ScriptableSingleton<OmniDebuggerProjectSettings>
	{
		public const string OptionsField = nameof(_options);
		
		private const string AssetPath = "ProjectSettings/OmniDebuggerSettings.asset";

		public OmniDebuggerOptions Options => _options ??= new OmniDebuggerOptions();

		[SerializeField] private OmniDebuggerOptions _options = new ();

		public SerializedObject CreateSerializedObject()
		{
			hideFlags &= ~HideFlags.NotEditable;
			return new SerializedObject(this);
		}

		public void Persist()
		{
			Save(true);
			ProjectOptions.NotifyEditorChanged();
		}

		public void ResetToDefaults()
		{
			_options = new OmniDebuggerOptions();
			Persist();
		}

		[InitializeOnLoadMethod]
		private static void ExposeToPlayMode() => ProjectOptions.EditorSource = () => instance.Options;
	}
}
#endif