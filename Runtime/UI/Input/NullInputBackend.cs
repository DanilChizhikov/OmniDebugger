using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NullInputBackend : IInputBackend
	{
		public bool IsTouchSupported => false;

		public bool IsKeyHeld(KeyCode key) => false;
		public bool WasKeyPressed(KeyCode key) => false;
	}
}