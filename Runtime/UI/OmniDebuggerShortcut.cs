using System;
using System.Collections.Generic;
using UnityEngine;

namespace DTech.OmniDebugger.UI
{
	/// <summary>
	/// One key or a chord of keys. Fires once when every key is held and the last of them has just
	/// gone down — holding the chord does not repeat it. Shift, Control, Alt, Command and Windows
	/// match either side of the keyboard: <see cref="KeyCode.LeftControl"/> also answers to
	/// <see cref="KeyCode.RightControl"/>.
	/// </summary>
	[Serializable]
	public sealed class OmniDebuggerShortcut
	{
		/// <summary>The keys, in no particular order. <see cref="KeyCode.None"/> entries are ignored.</summary>
		public IReadOnlyList<KeyCode> Keys => _keys ?? Array.Empty<KeyCode>();

		[SerializeField] private KeyCode[] _keys = Array.Empty<KeyCode>();

		public OmniDebuggerShortcut()
		{
		}

		public OmniDebuggerShortcut(params KeyCode[] keys)
		{
			_keys = keys ?? Array.Empty<KeyCode>();
		}

		public override string ToString() => string.Join("+", Keys);

		internal OmniDebuggerShortcut Clone() =>
			new OmniDebuggerShortcut(_keys == null ? Array.Empty<KeyCode>() : (KeyCode[])_keys.Clone());

		internal string ToHint()
		{
			string hint = null;

			for (int i = 0; i < Keys.Count; i++)
			{
				KeyCode key = Keys[i];

				if (key == KeyCode.None)
				{
					continue;
				}

				string name = KeyName(key);
				hint = hint == null ? name : hint + "+" + name;
			}

			return hint;
		}

		private static string KeyName(KeyCode key)
		{
			switch (key)
			{
				case KeyCode.LeftControl:
				case KeyCode.RightControl:
					return "Ctrl";
				case KeyCode.LeftShift:
				case KeyCode.RightShift:
					return "Shift";
				case KeyCode.LeftAlt:
				case KeyCode.RightAlt:
					return "Alt";
				case KeyCode.LeftCommand:
				case KeyCode.RightCommand:
					return "Cmd";
				case KeyCode.LeftWindows:
				case KeyCode.RightWindows:
					return "Win";
				case KeyCode.BackQuote:
					return "`";
			}

			return key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9
				? ((int)(key - KeyCode.Alpha0)).ToString()
				: key.ToString();
		}
	}
}