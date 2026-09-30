using System;
using System.Collections.Generic;

namespace DTech.OmniDebugger.UI
{
	internal sealed class TabRegistry : ITabRegistry
	{
		public event Action OnChanged;

		private static readonly Comparison<IOmniDebuggerTabFactory> _order = Compare;

		private readonly List<IOmniDebuggerTabFactory> _registered = new ();
		private readonly List<IOmniDebuggerTabFactory> _sorted = new ();
		private readonly ILogSink _log;

		public IReadOnlyList<IOmniDebuggerTabFactory> All
		{
			get
			{
				Sort();
				return _sorted;
			}
		}

		private bool _dirty = true;

		public TabRegistry(ILogSink log)
		{
			_log = log ?? throw new ArgumentNullException(nameof(log));

			_registered.Add(new CommandsTabFactory());
			_registered.Add(new LogsTabFactory());
			_registered.Add(new InfoTabFactory());
		}

		public bool Register(IOmniDebuggerTabFactory factory)
		{
			MainThreadGuard.Verify(nameof(Register));

			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			if (string.IsNullOrWhiteSpace(factory.Id))
			{
				throw new ArgumentException("A tab factory needs an id.", nameof(factory));
			}

			for (int i = 0; i < _registered.Count; i++)
			{
				IOmniDebuggerTabFactory existing = _registered[i];

				if (ReferenceEquals(existing, factory))
				{
					return false;
				}

				if (string.Equals(existing.Id, factory.Id, StringComparison.Ordinal))
				{
					_log.Warning(
						"A tab with this id is already registered, so the new one is ignored. " +
						$"Id: {factory.Id}; Kept: {existing.DisplayName}; Ignored: {factory.DisplayName}.");
					return false;
				}
			}

			_registered.Add(factory);
			_dirty = true;
			OnChanged?.Invoke();
			return true;
		}

		public bool Unregister(IOmniDebuggerTabFactory factory)
		{
			MainThreadGuard.Verify(nameof(Unregister));

			if (factory == null)
			{
				throw new ArgumentNullException(nameof(factory));
			}

			if (!_registered.Remove(factory))
			{
				return false;
			}

			_dirty = true;
			OnChanged?.Invoke();
			return true;
		}

		internal void Clear()
		{
			_registered.Clear();
			_sorted.Clear();
			_dirty = false;
			OnChanged = null;
		}

		private static int Compare(IOmniDebuggerTabFactory left, IOmniDebuggerTabFactory right)
		{
			int byOrder = left.Order.CompareTo(right.Order);
			return byOrder != 0 ? byOrder : string.CompareOrdinal(left.DisplayName, right.DisplayName);
		}

		private void Sort()
		{
			if (!_dirty)
			{
				return;
			}

			_dirty = false;
			_sorted.Clear();
			_sorted.AddRange(_registered);
			_sorted.Sort(_order);
		}
	}
}
