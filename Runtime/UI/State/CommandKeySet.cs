using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class CommandKeySet
	{
		public event Action OnChanged;

		private readonly List<string> _keys = new ();
		private readonly HashSet<string> _lookup = new (StringComparer.Ordinal);

		public IReadOnlyList<string> Keys => _keys;

		public int Count => _keys.Count;

		public bool Contains(string key) => key != null && _lookup.Contains(key);

		public bool Toggle(string key)
		{
			if (Contains(key))
			{
				Remove(key);
				return false;
			}

			Add(key);
			return true;
		}

		public bool Add(string key)
		{
			if (string.IsNullOrWhiteSpace(key) || !_lookup.Add(key))
			{
				return false;
			}

			_keys.Add(key);
			NotifyChanged();
			return true;
		}

		public bool Remove(string key)
		{
			if (key == null || !_lookup.Remove(key))
			{
				return false;
			}

			_keys.Remove(key);
			NotifyChanged();
			return true;
		}

		public void Clear()
		{
			if (_keys.Count == 0)
			{
				return;
			}

			_keys.Clear();
			_lookup.Clear();
			NotifyChanged();
		}

		private void NotifyChanged() => OnChanged?.Invoke();
	}
}