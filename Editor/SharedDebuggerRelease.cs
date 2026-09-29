#if OMNI_DEBUGGER
using UnityEditor;

namespace DTech.OmniDebugger.Editor
{
	internal static class SharedDebuggerRelease
	{
		[InitializeOnLoadMethod]
		private static void Subscribe()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.EnteredEditMode)
			{
				OmniDebuggerHost.ReleaseShared();
			}
		}
	}
}
#endif
