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

				if (!_input.IsKeyHeld(key))
				{
					return false;
				}

				anyKey = true;
				pressedNow |= _input.WasKeyPressed(key);
			}

			return anyKey && pressedNow;
		}
	}
}