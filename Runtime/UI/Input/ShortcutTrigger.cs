using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	internal sealed class ShortcutTrigger
	{
		private readonly IInputBackend _input;

		public ShortcutTrigger(IInputBackend input)
		{
			_input = input ?? new NullInputBackend();
		}

		public bool Poll(IReadOnlyList<OmniDebuggerShortcut> shortcuts)
		{
			if (shortcuts == null)
			{
				return false;
			}

			for (int i = 0; i < shortcuts.Count; i++)
			{
				if (IsTriggered(shortcuts[i]))
				{
					return true;
				}
			}

			return false;
		}

		private static KeyCode Twin(KeyCode key)
		{
			switch (key)
			{
				case KeyCode.LeftShift: return KeyCode.RightShift;
				case KeyCode.RightShift: return KeyCode.LeftShift;
				case KeyCode.LeftControl: return KeyCode.RightControl;
				case KeyCode.RightControl: return KeyCode.LeftControl;
				case KeyCode.LeftAlt: return KeyCode.RightAlt;
				case KeyCode.RightAlt: return KeyCode.LeftAlt;
				case KeyCode.LeftCommand: return KeyCode.RightCommand;
				case KeyCode.RightCommand: return KeyCode.LeftCommand;
				case KeyCode.LeftWindows: return KeyCode.RightWindows;
				case KeyCode.RightWindows: return KeyCode.LeftWindows;
				default: return KeyCode.None;
			}
		}

		private bool IsTriggered(OmniDebuggerShortcut shortcut)
		{
			if (shortcut == null)
			{
				return false;
			}

			IReadOnlyList<KeyCode> keys = shortcut.Keys;
			bool anyKey = false;
			bool pressedNow = false;

			for (int i = 0; i < keys.Count; i++)
			{
				KeyCode key = keys[i];

				if (key == KeyCode.None)
				{
					continue;
				}

				if (!IsHeld(key))
				{
					return false;
				}

				anyKey = true;
				pressedNow |= WasPressed(key);
			}

			return anyKey && pressedNow;
		}

		private bool IsHeld(KeyCode key)
		{
			KeyCode twin = Twin(key);
			return _input.IsKeyHeld(key) || (twin != KeyCode.None && _input.IsKeyHeld(twin));
		}

		private bool WasPressed(KeyCode key)
		{
			KeyCode twin = Twin(key);
			return _input.WasKeyPressed(key) || (twin != KeyCode.None && _input.WasKeyPressed(twin));
		}
	}
}