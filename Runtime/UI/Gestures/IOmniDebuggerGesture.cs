using System;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// How the panel is opened on a device, expressed in UI Toolkit pointer events. The built-in
	/// floating button implements it; replace it with <see cref="OmniDebuggerPanel.SetGesture"/>.
	/// Keyboard shortcuts are configured separately in <see cref="OmniDebuggerOpenOptions"/>.
	/// </summary>
	public interface IOmniDebuggerGesture
	{
		/// <summary>
		/// Called once the panel exists. <paramref name="requestOpen"/> opens it.
		/// </summary>
		void Attach(VisualElement root, Action requestOpen);

		/// <summary>
		/// Called whenever the panel opens or closes, so the gesture can hide itself while the panel
		/// is in the way.
		/// </summary>
		void SetPanelOpen(bool open);

		/// <summary>Called before the panel goes away. Must remove everything it added.</summary>
		void Detach();
	}
}