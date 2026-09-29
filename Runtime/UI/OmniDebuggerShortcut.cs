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
	}
}