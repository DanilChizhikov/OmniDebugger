#if ENABLE_INPUT_SYSTEM && OMNI_DEBUGGER_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DTech.OmniDebugger.UI
{
	internal static class KeyCodeToKey
	{
		private static readonly HashSet<KeyCode> _reported = new ();

		public static bool TryConvert(KeyCode code, out Key key)
		{
			if (code >= KeyCode.A && code <= KeyCode.Z)
			{
				key = Key.A + (code - KeyCode.A);
				return true;
			}

			if (code >= KeyCode.Alpha1 && code <= KeyCode.Alpha9)
			{
				key = Key.Digit1 + (code - KeyCode.Alpha1);
				return true;
			}

			if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9)
			{
				key = Key.Numpad0 + (code - KeyCode.Keypad0);
				return true;
			}

			if (code >= KeyCode.F1 && code <= KeyCode.F12)
			{
				key = Key.F1 + (code - KeyCode.F1);
				return true;
			}

			key = Special(code);

			if (key != Key.None)
			{
				return true;
			}

			if (code != KeyCode.None && _reported.Add(code))
			{
				UnityLogSink.Default.Warning(
					$"Key has no Input System equivalent, so the shortcut using it never fires. Key: {code}.");
			}

			return false;
		}

		private static Key Special(KeyCode code)
		{
			switch (code)
			{
				case KeyCode.Alpha0: return Key.Digit0;
				case KeyCode.Space: return Key.Space;
				case KeyCode.Return: return Key.Enter;
				case KeyCode.Tab: return Key.Tab;
				case KeyCode.BackQuote: return Key.Backquote;
				case KeyCode.Quote: return Key.Quote;
				case KeyCode.Semicolon: return Key.Semicolon;
				case KeyCode.Comma: return Key.Comma;
				case KeyCode.Period: return Key.Period;
				case KeyCode.Slash: return Key.Slash;
				case KeyCode.Backslash: return Key.Backslash;
				case KeyCode.LeftBracket: return Key.LeftBracket;
				case KeyCode.RightBracket: return Key.RightBracket;
				case KeyCode.Minus: return Key.Minus;
				case KeyCode.Equals: return Key.Equals;
				case KeyCode.LeftShift: return Key.LeftShift;
				case KeyCode.RightShift: return Key.RightShift;
				case KeyCode.LeftAlt: return Key.LeftAlt;
				case KeyCode.RightAlt: return Key.RightAlt;
				case KeyCode.AltGr: return Key.AltGr;
				case KeyCode.LeftControl: return Key.LeftCtrl;
				case KeyCode.RightControl: return Key.RightCtrl;
				case KeyCode.LeftCommand: return Key.LeftMeta;
				case KeyCode.RightCommand: return Key.RightMeta;
				case KeyCode.LeftWindows: return Key.LeftMeta;
				case KeyCode.RightWindows: return Key.RightMeta;
				case KeyCode.Menu: return Key.ContextMenu;
				case KeyCode.Escape: return Key.Escape;
				case KeyCode.LeftArrow: return Key.LeftArrow;
				case KeyCode.RightArrow: return Key.RightArrow;
				case KeyCode.UpArrow: return Key.UpArrow;
				case KeyCode.DownArrow: return Key.DownArrow;
				case KeyCode.Backspace: return Key.Backspace;
				case KeyCode.PageDown: return Key.PageDown;
				case KeyCode.PageUp: return Key.PageUp;
				case KeyCode.Home: return Key.Home;
				case KeyCode.End: return Key.End;
				case KeyCode.Insert: return Key.Insert;
				case KeyCode.Delete: return Key.Delete;
				case KeyCode.CapsLock: return Key.CapsLock;
				case KeyCode.Numlock: return Key.NumLock;
				case KeyCode.Print: return Key.PrintScreen;
				case KeyCode.ScrollLock: return Key.ScrollLock;
				case KeyCode.Pause: return Key.Pause;
				case KeyCode.KeypadEnter: return Key.NumpadEnter;
				case KeyCode.KeypadDivide: return Key.NumpadDivide;
				case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
				case KeyCode.KeypadPlus: return Key.NumpadPlus;
				case KeyCode.KeypadMinus: return Key.NumpadMinus;
				case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
				case KeyCode.KeypadEquals: return Key.NumpadEquals;
				default: return Key.None;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset() => _reported.Clear();
	}
}
#endif
