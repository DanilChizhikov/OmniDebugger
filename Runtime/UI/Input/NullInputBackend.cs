using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal sealed class NullInputBackend : IInputBackend
	{
		public bool IsTouchSupported => false;

		public bool IsKeyHeld(KeyCode key) => false;
		public bool WasKeyPressed(KeyCode key) => false;
		public bool IsGamepadButtonHeld(OmniDebuggerGamepadButtons button) => false;
		public bool WasGamepadButtonPressed(OmniDebuggerGamepadButtons button) => false;
		public NavigationMoveEvent.Direction PollStrandedDpad() => NavigationMoveEvent.Direction.None;
	}
}