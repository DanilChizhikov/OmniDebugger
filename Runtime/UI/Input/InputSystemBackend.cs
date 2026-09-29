#if ENABLE_INPUT_SYSTEM && OMNI_DEBUGGER_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DTech.OmniDebugger.UI
{
	internal sealed class InputSystemBackend : IInputBackend
	{
		public bool IsTouchSupported => Touchscreen.current != null;

		public bool IsKeyHeld(KeyCode key)
		{
			KeyControl control = Resolve(key);
			return control != null && control.isPressed;
		}

		public bool WasKeyPressed(KeyCode key)
		{
			KeyControl control = Resolve(key);
			return control != null && control.wasPressedThisFrame;
		}

		private static KeyControl Resolve(KeyCode code)
		{
			Keyboard keyboard = Keyboard.current;

			if (keyboard == null || !KeyCodeToKey.TryConvert(code, out Key key))
			{
				return null;
			}

			return keyboard[key];
		}
	}
}
#endif