using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger
{
	internal sealed class Hotbar : IHotbar
	{
		public event Action OnChanged;

		private readonly List<string> _paths = new ();
		private readonly HashSet<string> _lookup = new (StringComparer.Ordinal);

		public IReadOnlyList<string> Paths => _paths;

		internal bool IsRestored { get; set; }

		public bool Contains(string path) => path != null && _lookup.Contains(CommandPath.Normalize(path));

		public bool Pin(string path)
		{
			MainThreadGuard.Verify(nameof(Pin));

			string normalized = CommandPath.Normalize(path);
			if (normalized.Length == 0 || !_lookup.Add(normalized))
			{
				return false;
			}

			_paths.Add(normalized);
			OnChanged?.Invoke();
			return true;
		}

		public bool Unpin(string path)
		{
			MainThreadGuard.Verify(nameof(Unpin));

			string normalized = CommandPath.Normalize(path);
			if (!_lookup.Remove(normalized))
			{
				return false;
			}

			_paths.Remove(normalized);
			OnChanged?.Invoke();
			return true;
		}

		public bool Toggle(string path)
		{
			if (Contains(path))
			{
				Unpin(path);
				return false;
			}

			return Pin(path);
		}

		public void Move(int fromIndex, int toIndex)
		{
			MainThreadGuard.Verify(nameof(Move));

			if (fromIndex < 0 || fromIndex >= _paths.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(fromIndex));
			}

			if (toIndex < 0 || toIndex >= _paths.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(toIndex));
			}

			if (fromIndex == toIndex)
			{
				return;
			}

			string path = _paths[fromIndex];
			_paths.RemoveAt(fromIndex);
			_paths.Insert(toIndex, path);
			OnChanged?.Invoke();
		}

		public void Clear()
		{
			MainThreadGuard.Verify(nameof(Clear));

			if (_paths.Count == 0)
			{
				return;
			}

			_paths.Clear();
			_lookup.Clear();
			OnChanged?.Invoke();
		}

		internal void Release()
		{
			_paths.Clear();
			_lookup.Clear();
			OnChanged = null;
		}
	}
}
