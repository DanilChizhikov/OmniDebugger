using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal interface IInputBackend
	{
		bool IsTouchSupported { get; }

		bool IsKeyHeld(KeyCode key);
		bool WasKeyPressed(KeyCode key);
	}
}