using UnityEngine;
using UnityEngine.UIElements;

namespace DTech.OmniDebugger.UI
{
	internal interface IInputBackend
	{
		bool IsTouchSupported { get; }

		bool IsKeyHeld(KeyCode key);
		bool WasKeyPressed(KeyCode key);
		bool IsGamepadButtonHeld(OmniDebuggerGamepadButtons button);
		bool WasGamepadButtonPressed(OmniDebuggerGamepadButtons button);
		NavigationMoveEvent.Direction PollStrandedDpad();
	}
}