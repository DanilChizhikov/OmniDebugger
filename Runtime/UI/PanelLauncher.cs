using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal static class PanelLauncher
	{
		public static OmniDebuggerPanel Launch(IOmniDebugger debugger, OmniDebuggerPanelOptions options)
		{
			if (!Application.isPlaying)
			{
				return null;
			}

			return OmniDebuggerPanel.Create(debugger, options);
		}

		public static void Release(OmniDebuggerPanel panel)
		{
			if (panel == null)
			{
				return;
			}

			panel.Unbind();

			if (Application.isPlaying)
			{
				Object.Destroy(panel.gameObject);
			}
			else
			{
				Object.DestroyImmediate(panel.gameObject);
			}
		}
	}
}