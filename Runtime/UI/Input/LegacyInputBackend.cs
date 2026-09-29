#if ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal sealed class LegacyInputBackend : IInputBackend
	{
		public bool IsTouchSupported => Input.touchSupported;

		public bool IsKeyHeld(KeyCode key) => key != KeyCode.None && Input.GetKey(key);
		public bool WasKeyPressed(KeyCode key) => key != KeyCode.None && Input.GetKeyDown(key);
	}
}
#endif